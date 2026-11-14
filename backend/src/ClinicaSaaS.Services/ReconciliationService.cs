using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// Card reconciliation: the processor's settlement for a brand and some days is brought in, and each of its
// lines is matched to the card payment taken at the desk by batch, coupon and amount. What is left on
// either side is what the admin has to look into.
public class ReconciliationService(
    IBillingStore store,
    ICardProcessor processor,
    ICurrentUser currentUser,
    IAuditStore audit,
    ITimeZoneProvider timeZone,
    TimeProvider clock)
{
    public async Task<ReconciliationDto> GetAsync(CancellationToken ct)
    {
        var settlements = await store.Settlements
            .OrderByDescending(s => s.ImportedAt)
            .Take(30)
            .Select(s => new
            {
                s.Id,
                s.Brand,
                s.From,
                s.To,
                s.ImportedAt,
                s.Simulated,
                Lines = s.Lines.OrderBy(l => l.ChargedOn).ThenBy(l => l.Batch).ThenBy(l => l.Coupon).Select(l => new
                {
                    l.Id,
                    l.Batch,
                    l.Coupon,
                    l.Installments,
                    l.ChargedOn,
                    l.PaidOn,
                    l.Gross,
                    l.Fee,
                    l.Net,
                    l.ReceiptId
                }).ToList()
            })
            .ToListAsync(ct);
        var receiptIds = settlements.SelectMany(s => s.Lines.Select(l => l.ReceiptId)).OfType<Guid>().ToList();
        var patients = await store.Receipts.Where(r => receiptIds.Contains(r.Id))
            .Select(r => new { r.Id, Name = r.Client.FirstName + " " + r.Client.LastName })
            .ToDictionaryAsync(r => r.Id, r => r.Name, ct);
        var pending = await store.Receipts
            .Where(r => r.CardBrand != null && r.SettlementLineId == null && r.Amount > 0)
            .OrderBy(r => r.PaidAt)
            .Select(r => new CardReceiptDto(
                r.Id,
                r.Client.FirstName + " " + r.Client.LastName,
                r.CardBrand!.Value,
                r.CardInstallments ?? 1,
                r.CardBatch ?? "",
                r.CardCoupon ?? "",
                r.Amount,
                r.PaidAt,
                false))
            .ToListAsync(ct);
        return new ReconciliationDto(
            settlements.Select(s => new CardSettlementDto(
                s.Id, s.Brand, s.From, s.To, s.ImportedAt, s.Simulated,
                s.Lines.Sum(l => l.Gross), s.Lines.Sum(l => l.Fee), s.Lines.Sum(l => l.Net),
                s.Lines.Select(l => new SettlementLineDto(
                    l.Id, l.Batch, l.Coupon, l.Installments, l.ChargedOn, l.PaidOn, l.Gross, l.Fee, l.Net, l.ReceiptId,
                    l.ReceiptId is Guid id && patients.TryGetValue(id, out var name) ? name : null)).ToList())).ToList(),
            pending,
            processor.Simulated);
    }

    public async Task<ReconciliationDto> ImportAsync(ImportSettlementCommand command, CancellationToken ct)
    {
        var today = SchedulingRules.LocalDate(clock.GetUtcNow(), timeZone.Zone);
        if (command.From > command.To)
            throw new BusinessRuleException("La fecha desde tiene que ser anterior a la fecha hasta.");
        if (command.To > today)
            throw new BusinessRuleException("No se puede pedir la liquidación de días que todavía no pasaron.");
        if (command.To.DayNumber - command.From.DayNumber > 62)
            throw new BusinessRuleException("Pedí la liquidación de dos meses como mucho.");

        var lines = await processor.SettlementAsync(command.Brand, command.From, command.To, ct);
        // A line the clinic already has, from an earlier import of overlapping days, is not brought in twice.
        var known = await store.SettlementLines
            .Where(l => l.Settlement!.Brand == command.Brand)
            .Select(l => l.Batch + "|" + l.Coupon)
            .ToListAsync(ct);
        var fresh = lines.Where(l => !known.Contains(l.Batch + "|" + l.Coupon)).ToList();
        var settlement = new CardSettlement
        {
            Id = Guid.NewGuid(),
            Brand = command.Brand,
            From = command.From,
            To = command.To,
            ImportedAt = clock.GetUtcNow(),
            Simulated = processor.Simulated
        };
        var open = await store.Receipts
            .Where(r => r.CardBrand == command.Brand && r.SettlementLineId == null && r.Amount > 0)
            .Select(r => r.Id)
            .ToListAsync(ct);
        var receipts = new List<Receipt>();
        foreach (var id in open)
            receipts.Add((await store.GetReceiptAsync(id, ct))!);
        var matched = 0;
        foreach (var data in fresh)
        {
            var line = new CardSettlementLine
            {
                Id = Guid.NewGuid(),
                SettlementId = settlement.Id,
                Batch = data.Batch,
                Coupon = data.Coupon,
                Installments = data.Installments,
                ChargedOn = data.ChargedOn,
                PaidOn = data.PaidOn,
                Gross = data.Gross,
                Fee = data.Fee,
                Net = data.Gross - data.Fee
            };
            var receipt = receipts.FirstOrDefault(r => r.SettlementLineId == null && r.CardBatch == data.Batch && r.CardCoupon == data.Coupon && r.Amount == data.Gross);
            if (receipt is not null)
            {
                receipt.SettlementLineId = line.Id;
                line.ReceiptId = receipt.Id;
                matched++;
            }
            settlement.Lines.Add(line);
        }
        store.AddSettlement(settlement);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "liquidacion", "Tarjeta", settlement.Id.ToString(),
            $"{command.Brand} {command.From:dd/MM}–{command.To:dd/MM} · {fresh.Count} líneas · {matched} conciliadas", ct);
        return await GetAsync(ct);
    }

    // For a line the automatic match missed, say a coupon typed wrong at the desk.
    public async Task<ReconciliationDto> MatchAsync(Guid lineId, MatchLineCommand command, CancellationToken ct)
    {
        var line = await store.GetSettlementLineAsync(lineId, ct) ?? throw new NotFoundException("Línea de liquidación no encontrada.");
        if (line.ReceiptId is not null)
            throw new ConflictException("Esa línea ya está conciliada.");
        var receipt = await store.GetReceiptAsync(command.ReceiptId, ct) ?? throw new NotFoundException("Cobro no encontrado.");
        if (receipt.CardBrand != line.Settlement!.Brand || receipt.SettlementLineId is not null)
            throw new BusinessRuleException("Ese cobro no es de esta tarjeta o ya está conciliado.");
        if (receipt.Amount != line.Gross)
            throw new BusinessRuleException("El importe del cobro no coincide con el de la liquidación.");
        receipt.SettlementLineId = line.Id;
        line.ReceiptId = receipt.Id;
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "conciliacion", "Tarjeta", line.Id.ToString(), $"lote {line.Batch} cupón {line.Coupon}", ct);
        return await GetAsync(ct);
    }
}
