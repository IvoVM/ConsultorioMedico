using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The register of each site: front desk opens it with the cash in the drawer, every payment and refund
// lands in it, expenses and other income are entered by hand, and closing it compares the cash counted
// with what should be there.
public class CashService(
    IBillingStore store,
    IOrganizationStore organization,
    ITenantUserStore users,
    ICurrentUser currentUser,
    IAuditStore audit,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<CashSessionDto>> OpenSessionsAsync(CancellationToken ct) =>
        await DtosAsync(store.CashSessions.Where(s => s.Status == CashSessionStatus.Open), withMovements: false, ct);

    public async Task<IReadOnlyList<CashSessionDto>> ListAsync(Guid? locationId, CancellationToken ct)
    {
        var source = store.CashSessions.AsQueryable();
        if (locationId is Guid id)
            source = source.Where(s => s.LocationId == id);
        var recent = source.OrderByDescending(s => s.OpenedAt).Take(60);
        return await DtosAsync(recent, withMovements: false, ct);
    }

    public async Task<CashSessionDto> GetAsync(Guid id, CancellationToken ct) =>
        (await DtosAsync(store.CashSessions.Where(s => s.Id == id), withMovements: true, ct)).FirstOrDefault()
            ?? throw new NotFoundException("Caja no encontrada.");

    public async Task<CashSessionDto> OpenAsync(OpenCashCommand command, CancellationToken ct)
    {
        var location = await organization.GetLocationAsync(command.LocationId, ct) ?? throw new NotFoundException("Sede no encontrada.");
        if (command.OpeningCash < 0)
            throw new BusinessRuleException("El efectivo inicial no puede ser negativo.");
        if (await store.CashSessions.AnyAsync(s => s.LocationId == location.Id && s.Status == CashSessionStatus.Open, ct))
            throw new ConflictException($"La caja de {location.Name} ya está abierta.");
        var session = new CashSession
        {
            Id = Guid.NewGuid(),
            LocationId = location.Id,
            OpeningCash = BillingRules.NormalizePrice(command.OpeningCash),
            OpenedAt = clock.GetUtcNow(),
            OpenedById = currentUser.Id
        };
        store.AddCashSession(session);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "apertura", "Caja", session.Id.ToString(), $"{location.Name} · {session.OpeningCash:0.00}", ct);
        return await GetAsync(session.Id, ct);
    }

    public async Task<CashSessionDto> AddMovementAsync(Guid id, CashMovementCommand command, CancellationToken ct)
    {
        var session = await OpenSessionAsync(id, ct);
        if (CashRules.MovementError(command.Kind, command.Amount, command.Description) is string error)
            throw new BusinessRuleException(error);
        var description = command.Description.Trim();
        if (description.Length > 200)
            throw new BusinessRuleException("La descripción no puede pasar los 200 caracteres.");
        Record(session, command.Kind, command.Method, BillingRules.NormalizePrice(command.Amount), description, null);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, command.Kind == CashMovementKind.Income ? "ingreso" : "egreso", "Caja", session.Id.ToString(), $"{command.Method} {command.Amount:0.00} · {description}", ct);
        return await GetAsync(session.Id, ct);
    }

    public async Task<CashSessionDto> CloseAsync(Guid id, CloseCashCommand command, CancellationToken ct)
    {
        var session = await OpenSessionAsync(id, ct);
        if (command.CountedCash < 0)
            throw new BusinessRuleException("El efectivo contado no puede ser negativo.");
        var movements = await store.CashSessions.Where(s => s.Id == id).SelectMany(s => s.Movements)
            .Select(m => new { m.Kind, m.Method, m.Amount }).ToListAsync(ct);
        session.ExpectedCash = CashRules.ExpectedCash(session.OpeningCash, movements.Select(m => (m.Kind, m.Method, m.Amount)));
        session.CountedCash = BillingRules.NormalizePrice(command.CountedCash);
        session.Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim()[..Math.Min(command.Notes.Trim().Length, 500)];
        session.Status = CashSessionStatus.Closed;
        session.ClosedAt = clock.GetUtcNow();
        session.ClosedById = currentUser.Id;
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "cierre", "Caja", session.Id.ToString(),
            $"esperado {session.ExpectedCash:0.00} · contado {session.CountedCash:0.00} · diferencia {session.CountedCash - session.ExpectedCash:0.00}", ct);
        return await GetAsync(session.Id, ct);
    }

    // A payment or refund needs the register of that site open, so the money is counted when it closes.
    public async Task<CashSession> RequireOpenAsync(Guid locationId, CancellationToken ct)
    {
        var id = await store.CashSessions.Where(s => s.LocationId == locationId && s.Status == CashSessionStatus.Open).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
        if (id is Guid open)
            return (await store.GetCashSessionAsync(open, ct))!;
        var name = await organization.Locations.Where(l => l.Id == locationId).Select(l => l.Name).FirstOrDefaultAsync(ct) ?? "la sede";
        throw new BusinessRuleException($"La caja de {name} está cerrada. Abrila para cobrar.");
    }

    public void Record(CashSession session, CashMovementKind kind, PaymentMethod method, decimal amount, string description, Guid? receiptId) =>
        store.AddCashMovement(new CashMovement
        {
            Id = Guid.NewGuid(),
            CashSessionId = session.Id,
            Kind = kind,
            Method = method,
            Amount = amount,
            Description = description,
            At = clock.GetUtcNow(),
            ReceiptId = receiptId,
            UserId = currentUser.Id
        });

    private async Task<CashSession> OpenSessionAsync(Guid id, CancellationToken ct)
    {
        var session = await store.GetCashSessionAsync(id, ct) ?? throw new NotFoundException("Caja no encontrada.");
        return session.Status == CashSessionStatus.Open ? session : throw new BusinessRuleException("Esa caja ya está cerrada.");
    }

    private async Task<IReadOnlyList<CashSessionDto>> DtosAsync(IQueryable<CashSession> source, bool withMovements, CancellationToken ct)
    {
        var sessions = await source
            .Select(s => new
            {
                s.Id,
                s.LocationId,
                Location = s.Location.Name,
                s.Status,
                s.OpeningCash,
                s.OpenedAt,
                s.OpenedById,
                s.ClosedAt,
                s.ClosedById,
                s.ExpectedCash,
                s.CountedCash,
                s.Notes,
                Movements = s.Movements.OrderBy(m => m.At).Select(m => new { m.Id, m.Kind, m.Method, m.Amount, m.Description, m.At, m.UserId }).ToList()
            })
            .ToListAsync(ct);
        var ids = sessions.SelectMany(s => s.Movements.Select(m => m.UserId).Append(s.OpenedById).Append(s.ClosedById)).OfType<Guid>().Distinct().ToList();
        var names = await users.Users.Where(u => ids.Contains(u.Id)).Select(u => new { u.Id, u.FirstName, u.LastName }).ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), ct);
        string? Name(Guid? id) => id is Guid key && names.TryGetValue(key, out var name) ? name : null;

        return sessions.Select(s =>
        {
            var expected = s.ExpectedCash ?? CashRules.ExpectedCash(s.OpeningCash, s.Movements.Select(m => (m.Kind, m.Method, m.Amount)));
            var totals = Enum.GetValues<PaymentMethod>().Select(method =>
            {
                var of = s.Movements.Where(m => m.Method == method).ToList();
                var inflow = of.Where(m => CashRules.IsIn(m.Kind)).Sum(m => m.Amount);
                var outflow = of.Where(m => !CashRules.IsIn(m.Kind)).Sum(m => m.Amount);
                return new CashTotalDto(method, inflow, outflow, inflow - outflow);
            }).ToList();
            return new CashSessionDto(
                s.Id, s.LocationId, s.Location, s.Status, s.OpeningCash, s.OpenedAt, Name(s.OpenedById), s.ClosedAt, Name(s.ClosedById),
                expected, s.CountedCash, s.CountedCash is decimal counted ? counted - expected : null, s.Notes, totals,
                withMovements ? s.Movements.Select(m => new CashMovementDto(m.Id, m.Kind, m.Method, m.Amount, m.Description, m.At, Name(m.UserId))).ToList() : []);
        }).ToList();
    }
}
