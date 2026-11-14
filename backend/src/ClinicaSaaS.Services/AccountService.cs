using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// A patient's running account. Each payment is a receipt entered in the register of a site and applied
// to one or more invoices; what it does not cover stays as credit, which pays a later invoice or is given back.
public class AccountService(
    IBillingStore store,
    IClientStore clients,
    IAppointmentStore appointments,
    CashService cash,
    ICurrentUser currentUser,
    IAuditStore audit,
    ITimeZoneProvider timeZone,
    TimeProvider clock)
{
    public async Task<AccountDto> GetAsync(Guid clientId, CancellationToken ct)
    {
        var client = await clients.Clients.Where(c => c.Id == clientId)
            .Select(c => new { c.Id, c.FirstName, c.LastName, c.DocumentNumber })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Paciente no encontrado.");
        var invoices = await InvoiceDtosAsync(store.Invoices.Where(i => i.ClientId == clientId).OrderByDescending(i => i.CreatedAt), ct);
        var receipts = await store.Receipts.Where(r => r.ClientId == clientId)
            .Select(r => new { r.PaidAt, r.Amount, r.Method, r.CardBrand, r.Note })
            .ToListAsync(ct);

        var lines = new List<(DateTimeOffset At, AccountMovementKind Kind, decimal Debit, decimal Credit, InvoiceDto? Invoice, PaymentMethod? Method, CardBrand? Brand, string? Note)>();
        foreach (var invoice in invoices)
        {
            if (invoice.PatientAmount > 0)
                lines.Add((invoice.CreatedAt, AccountMovementKind.Invoice, invoice.PatientAmount, 0, invoice, null, null, null));
            if (invoice.VoidedAt is DateTimeOffset voided && invoice.PatientAmount > 0)
                lines.Add((voided, AccountMovementKind.Voided, 0, invoice.PatientAmount, invoice, null, null, invoice.VoidReason));
        }
        foreach (var receipt in receipts)
        {
            lines.Add(receipt.Amount >= 0
                ? (receipt.PaidAt, AccountMovementKind.Payment, 0, receipt.Amount, null, receipt.Method, receipt.CardBrand, receipt.Note)
                : (receipt.PaidAt, AccountMovementKind.Refund, -receipt.Amount, 0, null, receipt.Method, null, receipt.Note));
        }

        var balance = 0m;
        var movements = lines.OrderBy(l => l.At).Select(l =>
        {
            balance += l.Debit - l.Credit;
            return new AccountMovementDto(l.At, l.Kind, l.Debit, l.Credit, balance, l.Invoice?.Id, l.Invoice?.VoucherType, l.Invoice?.PointOfSale, l.Invoice?.Number, l.Method, l.Brand, l.Note);
        }).Reverse().ToList();

        var live = invoices.Where(i => i.Status != InvoiceStatus.Voided).ToList();
        var overdue = live.Where(i => i.Plan is not null)
            .SelectMany(i => i.Plan!.Installments)
            .Where(i => i.Status == InstallmentStatus.Overdue)
            .Sum(i => i.Amount - i.Paid);
        return new AccountDto(
            client.Id,
            $"{client.FirstName} {client.LastName}".Trim(),
            client.DocumentNumber,
            live.Sum(i => i.Balance),
            (await CreditsAsync([clientId], ct)).GetValueOrDefault(clientId),
            overdue,
            movements,
            invoices);
    }

    public async Task<AccountDto> MineAsync(CancellationToken ct) => await GetAsync(await OwnClientAsync(ct), ct);

    public async Task<AccountDto> PayAsync(Guid clientId, AccountPaymentCommand command, CancellationToken ct)
    {
        _ = await clients.Clients.AnyAsync(c => c.Id == clientId, ct) ? true : throw new NotFoundException("Paciente no encontrado.");
        var amount = BillingRules.NormalizePrice(command.Amount);
        if (amount <= 0)
            throw new BusinessRuleException("El monto tiene que ser mayor a 0.");

        // The listed invoices, or every one still owing, oldest first.
        var openIds = await store.Invoices
            .Where(i => i.ClientId == clientId && (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.PartiallyPaid))
            .Where(i => command.InvoiceIds == null || command.InvoiceIds.Count == 0 || command.InvoiceIds.Contains(i.Id))
            .OrderBy(i => i.CreatedAt)
            .Select(i => i.Id)
            .ToListAsync(ct);
        var invoices = (await store.GetInvoicesAsync(openIds, ct)).OrderBy(i => i.CreatedAt).ToList();
        var left = amount;
        var allocations = new List<(Invoice, decimal)>();
        foreach (var invoice in invoices)
        {
            if (left <= 0)
                break;
            var owed = BillingRules.Balance(invoice.Total, invoice.InsurerAmount, invoice.Payments.Sum(p => p.Amount));
            var part = Math.Min(owed, left);
            if (part <= 0)
                continue;
            allocations.Add((invoice, part));
            left -= part;
        }
        await PostAsync(clientId, command.LocationId, amount, command.Method, Card(command.Method, command.CardBrand, command.CardInstallments, command.CardBatch, command.CardCoupon), command.Note, allocations, ct);
        return await GetAsync(clientId, ct);
    }

    public async Task<AccountDto> RefundAsync(Guid clientId, RefundCommand command, CancellationToken ct)
    {
        var amount = BillingRules.NormalizePrice(command.Amount);
        var reason = (command.Reason ?? "").Trim();
        if (amount <= 0)
            throw new BusinessRuleException("El monto tiene que ser mayor a 0.");
        if (reason.Length == 0)
            throw new BusinessRuleException("Contá por qué se devuelve.");
        var credit = (await CreditsAsync([clientId], ct)).GetValueOrDefault(clientId);
        if (amount > credit)
            throw new BusinessRuleException("No se puede devolver más que el saldo a favor.");
        var session = await cash.RequireOpenAsync(command.LocationId, ct);
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Amount = -amount,
            Method = command.Method,
            PaidAt = clock.GetUtcNow(),
            LocationId = command.LocationId,
            CashSessionId = session.Id,
            Note = reason.Length > 200 ? reason[..200] : reason
        };
        store.AddReceipt(receipt);
        cash.Record(session, CashMovementKind.Refund, command.Method, amount, $"Devolución a {await PatientNameAsync(clientId, ct)}", receipt.Id);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "devolucion", "Cuenta", clientId.ToString(), $"{command.Method} {amount:0.00} · {reason}", ct);
        return await GetAsync(clientId, ct);
    }

    // Pays an invoice with the patient's credit, taking it from their oldest receipts that still have some left.
    public async Task<InvoiceDto> ApplyCreditAsync(Guid invoiceId, ApplyCreditCommand command, CancellationToken ct)
    {
        var invoice = (await store.GetInvoicesAsync([invoiceId], ct)).FirstOrDefault() ?? throw new NotFoundException("Comprobante no encontrado.");
        if (!BillingRules.CanPay(invoice.Status))
            throw new BusinessRuleException("Ese comprobante ya no se puede cobrar.");
        var credit = (await CreditsAsync([invoice.ClientId], ct)).GetValueOrDefault(invoice.ClientId);
        var balance = BillingRules.Balance(invoice.Total, invoice.InsurerAmount, invoice.Payments.Sum(p => p.Amount));
        var amount = BillingRules.NormalizePrice(command.Amount ?? Math.Min(credit, balance));
        if (BillingRules.PaymentError(amount, balance) is string error)
            throw new BusinessRuleException(error);
        if (amount > credit)
            throw new BusinessRuleException("El paciente no tiene tanto saldo a favor.");

        var receipts = await store.Receipts
            .Where(r => r.ClientId == invoice.ClientId && r.Amount > 0)
            .OrderBy(r => r.PaidAt)
            .Select(r => new
            {
                r.Id,
                r.Method,
                Left = r.Amount - r.Allocations.Where(a => a.Invoice!.Status != InvoiceStatus.Voided).Sum(a => a.Amount)
            })
            .Where(r => r.Left > 0)
            .ToListAsync(ct);
        var now = clock.GetUtcNow();
        var left = amount;
        foreach (var receipt in receipts)
        {
            if (left <= 0)
                break;
            var part = Math.Min(receipt.Left, left);
            Allocate(invoice, new InvoicePayment { Id = Guid.NewGuid(), InvoiceId = invoice.Id, ReceiptId = receipt.Id, Amount = part, Method = receipt.Method, PaidAt = now });
            left -= part;
        }
        Settle(invoice, now);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "aplicacion", "Comprobante", invoice.Id.ToString(), $"saldo a favor {amount:0.00}", ct);
        return await ReadInvoiceAsync(invoice.Id, ct);
    }

    // Enters money in the open register of the site and applies it to the given invoices; the rest is credit.
    public async Task<Receipt> PostAsync(
        Guid clientId,
        Guid locationId,
        decimal amount,
        PaymentMethod method,
        (CardBrand Brand, int Installments, string Batch, string Coupon)? card,
        string? note,
        IReadOnlyList<(Invoice Invoice, decimal Amount)> allocations,
        CancellationToken ct)
    {
        var session = await cash.RequireOpenAsync(locationId, ct);
        var now = clock.GetUtcNow();
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Amount = amount,
            Method = method,
            PaidAt = now,
            LocationId = locationId,
            CashSessionId = session.Id,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 200)],
            CardBrand = card?.Brand,
            CardInstallments = card?.Installments,
            CardBatch = card?.Batch,
            CardCoupon = card?.Coupon
        };
        store.AddReceipt(receipt);
        foreach (var (invoice, part) in allocations)
        {
            Allocate(invoice, new InvoicePayment { Id = Guid.NewGuid(), InvoiceId = invoice.Id, ReceiptId = receipt.Id, Amount = part, Method = method, PaidAt = now });
            Settle(invoice, now);
        }
        cash.Record(session, CashMovementKind.Payment, method, amount, $"Cobro a {await PatientNameAsync(clientId, ct)}", receipt.Id);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "pago", "Cuenta", clientId.ToString(),
            $"{method} {amount:0.00}{(card is { } c ? $" · {c.Brand} {c.Installments} cuotas lote {c.Batch} cupón {c.Coupon}" : "")}", ct);
        return receipt;
    }

    public static (CardBrand, int, string, string)? Card(PaymentMethod method, CardBrand? brand, int? installments, string? batch, string? coupon)
    {
        if (method != PaymentMethod.Card)
            return null;
        if (CardRules.Error(brand, installments, batch, coupon) is string error)
            throw new BusinessRuleException(error);
        return (brand!.Value, installments!.Value, batch!.Trim(), coupon!.Trim());
    }

    // Credit: money received (net of refunds) that no live invoice has used.
    public async Task<Dictionary<Guid, decimal>> CreditsAsync(IReadOnlyCollection<Guid> clientIds, CancellationToken ct)
    {
        var received = await store.Receipts.Where(r => clientIds.Contains(r.ClientId))
            .GroupBy(r => r.ClientId).Select(g => new { g.Key, Sum = g.Sum(r => r.Amount) }).ToListAsync(ct);
        var applied = await store.Invoices.Where(i => clientIds.Contains(i.ClientId) && i.Status != InvoiceStatus.Voided)
            .SelectMany(i => i.Payments.Where(p => p.ReceiptId != null).Select(p => new { i.ClientId, p.Amount }))
            .GroupBy(p => p.ClientId).Select(g => new { g.Key, Sum = g.Sum(p => p.Amount) }).ToListAsync(ct);
        return clientIds.ToDictionary(
            id => id,
            id => Math.Max(0, (received.FirstOrDefault(r => r.Key == id)?.Sum ?? 0) - (applied.FirstOrDefault(a => a.Key == id)?.Sum ?? 0)));
    }

    public async Task<List<InvoiceDto>> InvoiceDtosAsync(IQueryable<Invoice> source, CancellationToken ct)
    {
        var invoices = await source.Select(InvoiceMappingExtensions.MapForInvoiceDto()).ToListAsync(ct);
        var credits = await CreditsAsync(invoices.Select(i => i.ClientId).Distinct().ToList(), ct);
        var today = Today();
        return invoices.Select(i => i.MapToInvoiceDto(today, credits.GetValueOrDefault(i.ClientId))).ToList();
    }

    public async Task<InvoiceDto> ReadInvoiceAsync(Guid id, CancellationToken ct) =>
        (await InvoiceDtosAsync(store.Invoices.Where(i => i.Id == id), ct)).FirstOrDefault()
            ?? throw new NotFoundException("Comprobante no encontrado.");

    public async Task<Guid> LocationOfAsync(Invoice invoice, CancellationToken ct) =>
        await appointments.Appointments.Where(a => a.Id == invoice.AppointmentId).Select(a => a.LocationId).FirstAsync(ct);

    public DateOnly Today() => SchedulingRules.LocalDate(clock.GetUtcNow(), timeZone.Zone);

    public async Task<Guid> OwnClientAsync(CancellationToken ct)
    {
        var userId = currentUser.Id ?? Guid.Empty;
        var own = await clients.Clients.Where(c => c.UserId == userId).Select(c => c.Id).FirstOrDefaultAsync(ct);
        return own == Guid.Empty ? throw new NotFoundException("No hay un paciente asociado a esta cuenta.") : own;
    }

    // Added through the store first: a new row with its key already set, reached only through a loaded
    // invoice's collection, would otherwise be taken for an existing one.
    private void Allocate(Invoice invoice, InvoicePayment payment)
    {
        store.AddPayment(payment);
        invoice.Payments.Add(payment);
    }

    private static void Settle(Invoice invoice, DateTimeOffset now)
    {
        var paid = invoice.Payments.Sum(p => p.Amount);
        invoice.Status = BillingRules.StatusFor(invoice.Total, invoice.InsurerAmount, paid);
        invoice.PaidAt = invoice.Status == InvoiceStatus.Paid ? invoice.PaidAt ?? now : null;
    }

    private async Task<string> PatientNameAsync(Guid clientId, CancellationToken ct) =>
        await clients.Clients.Where(c => c.Id == clientId).Select(c => c.FirstName + " " + c.LastName).FirstOrDefaultAsync(ct) ?? "paciente";
}
