using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The supplies the practices use up: what is on the shelf, what came in, what each visit used and the corrections after counting.
public class SupplyService(
    ISupplyStore store,
    IOrganizationStore organization,
    ICurrentUser currentUser,
    IAuditStore audit,
    TimeProvider clock)
{
    // low keeps only the active supplies at or below their minimum: what front desk opens to know what to order.
    public async Task<object> ListAsync(ListQuery query, bool low, CancellationToken ct)
    {
        var source = store.Supplies;
        // A supply taken out of use only matters to the admin, who can put it back.
        if (currentUser.Role != TenantRole.TenantAdmin.ToString())
            source = source.Where(s => s.IsActive);
        if (query.Term is string term)
            source = source.Where(s => s.Name.ToLower().Contains(term) || s.Unit.ToLower().Contains(term));
        if (low)
            source = source.Where(s => s.IsActive && s.MinStock > 0 && s.Stock <= s.MinStock);
        var ordered = (query.Sort, query.Desc) switch
        {
            ("stock", false) => source.OrderBy(s => s.Stock).ThenBy(s => s.Name),
            ("stock", true) => source.OrderByDescending(s => s.Stock).ThenBy(s => s.Name),
            ("name", true) => source.OrderByDescending(s => s.Name),
            _ => source.OrderBy(s => s.Name)
        };
        var rows = ordered.Select(s => new SupplyDto(s.Id, s.Name, s.Unit, s.Stock, s.MinStock, s.IsActive, s.MinStock > 0 && s.Stock <= s.MinStock));
        if (!query.Paged)
            return await rows.ToListAsync(ct);
        return await Pages.Materialize(rows, query, ct);
    }

    public async Task<SupplyDto> CreateAsync(SaveSupplyCommand command, CancellationToken ct)
    {
        var (name, unit) = await CheckAsync(command, null, ct);
        var supply = new Supply { Id = Guid.NewGuid(), Name = name, Unit = unit, MinStock = command.MinStock, IsActive = true };
        store.Add(supply);
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "alta", "Insumo", supply.Id.ToString(), supply.Name, ct);
        return ToDto(supply);
    }

    public async Task<SupplyDto> UpdateAsync(Guid id, SaveSupplyCommand command, CancellationToken ct)
    {
        var supply = await store.GetAsync(id, ct) ?? throw new NotFoundException("Insumo no encontrado.");
        (supply.Name, supply.Unit) = await CheckAsync(command, id, ct);
        supply.MinStock = command.MinStock;
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "edicion", "Insumo", supply.Id.ToString(), supply.Name, ct);
        return ToDto(supply);
    }

    public async Task<SupplyDto> SetActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var supply = await store.GetAsync(id, ct) ?? throw new NotFoundException("Insumo no encontrado.");
        if (!active)
        {
            var practice = await organization.TypeSupplies.Where(s => s.SupplyId == id).Select(s => s.AppointmentType.Name).FirstOrDefaultAsync(ct);
            if (practice is not null)
                throw new BusinessRuleException($"Lo usa la prestación {practice}. Sacalo de esa práctica antes de darlo de baja.");
        }
        supply.IsActive = active;
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, active ? "alta" : "baja", "Insumo", supply.Id.ToString(), supply.Name, ct);
        return ToDto(supply);
    }

    // A delivery: the stock goes up by what came in.
    public async Task<SupplyDto> EntryAsync(Guid id, SupplyEntryCommand command, CancellationToken ct)
    {
        if (command.Quantity <= 0 || command.Quantity > 100000)
            throw new BusinessRuleException("La cantidad que ingresa va de más de 0 a 100000.");
        var note = command.Note?.Trim();
        if (note is { Length: > 200 })
            throw new BusinessRuleException("La nota tiene hasta 200 caracteres.");
        var supply = await store.GetAsync(id, ct) ?? throw new NotFoundException("Insumo no encontrado.");
        if (!supply.IsActive)
            throw new BusinessRuleException("Ese insumo está dado de baja.");
        var quantity = Math.Round(command.Quantity, 2);
        supply.Stock += quantity;
        store.AddMovement(Movement(supply.Id, SupplyMovementKind.Entry, quantity, null, string.IsNullOrEmpty(note) ? null : note));
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "ingreso", "Insumo", supply.Id.ToString(), $"{supply.Name} +{quantity} {supply.Unit}", ct);
        return ToDto(supply);
    }

    // A count: the stock becomes what is on the shelf and the difference stays as an adjustment with its reason.
    public async Task<SupplyDto> CountAsync(Guid id, SupplyCountCommand command, CancellationToken ct)
    {
        if (command.Stock < 0 || command.Stock > 1000000)
            throw new BusinessRuleException("El stock contado va de 0 a 1000000.");
        var reason = command.Reason?.Trim() ?? "";
        if (reason.Length is 0 or > 200)
            throw new BusinessRuleException("Escribí por qué se corrige, en hasta 200 caracteres.");
        var supply = await store.GetAsync(id, ct) ?? throw new NotFoundException("Insumo no encontrado.");
        var counted = Math.Round(command.Stock, 2);
        var difference = counted - supply.Stock;
        if (difference == 0)
            throw new BusinessRuleException("El stock contado es igual al que figura; no hay nada que corregir.");
        supply.Stock = counted;
        store.AddMovement(Movement(supply.Id, SupplyMovementKind.Adjustment, difference, null, reason));
        await store.SaveAsync(ct);
        await audit.RecordAsync(currentUser.Id, "ajuste", "Insumo", supply.Id.ToString(), $"{supply.Name} {difference:+0.##;-0.##} {supply.Unit}: {reason}", ct);
        return ToDto(supply);
    }

    public async Task<PageDto<SupplyMovementDto>> MovementsAsync(Guid id, ListQuery query, CancellationToken ct)
    {
        if (!await store.Supplies.AnyAsync(s => s.Id == id, ct))
            throw new NotFoundException("Insumo no encontrado.");
        var rows = store.Movements
            .Where(m => m.SupplyId == id)
            .OrderByDescending(m => m.At)
            .Select(m => new SupplyMovementDto(
                m.Id,
                m.Kind,
                m.Quantity,
                m.At,
                m.Note,
                m.User != null ? (m.User.FirstName + " " + m.User.LastName).Trim() : null,
                m.AppointmentId,
                m.Appointment != null ? (m.Appointment.Client.FirstName + " " + m.Appointment.Client.LastName).Trim() : null));
        return await Pages.Materialize(rows, query, ct);
    }

    // Closing a visit uses up what its practice needs. A visit is consumed once, even if closing is retried.
    // The stock may go below zero: the visit happened, and the count afterwards tells front desk to correct it.
    public async Task ConsumeForAsync(Guid appointmentId, Guid appointmentTypeId, CancellationToken ct)
    {
        if (await store.Movements.AnyAsync(m => m.AppointmentId == appointmentId && m.Kind == SupplyMovementKind.Consumption, ct))
            return;
        var lines = await organization.TypeSupplies
            .Where(s => s.AppointmentTypeId == appointmentTypeId)
            .Select(s => new { s.SupplyId, s.Quantity, Practice = s.AppointmentType.Name })
            .ToListAsync(ct);
        if (lines.Count == 0)
            return;
        var supplies = await store.GetManyAsync(lines.Select(l => l.SupplyId).ToList(), ct);
        foreach (var line in lines)
        {
            var supply = supplies.First(s => s.Id == line.SupplyId);
            supply.Stock -= line.Quantity;
            store.AddMovement(Movement(supply.Id, SupplyMovementKind.Consumption, -line.Quantity, appointmentId, line.Practice));
        }
        await store.SaveAsync(ct);
    }

    private SupplyMovement Movement(Guid supplyId, SupplyMovementKind kind, decimal quantity, Guid? appointmentId, string? note) => new()
    {
        Id = Guid.NewGuid(),
        SupplyId = supplyId,
        Kind = kind,
        Quantity = quantity,
        AppointmentId = appointmentId,
        Note = note,
        At = clock.GetUtcNow(),
        UserId = currentUser.Id
    };

    private async Task<(string Name, string Unit)> CheckAsync(SaveSupplyCommand command, Guid? self, CancellationToken ct)
    {
        var name = command.Name?.Trim() ?? "";
        var unit = command.Unit?.Trim() ?? "";
        if (name.Length is 0 or > 120)
            throw new BusinessRuleException("El insumo necesita un nombre de hasta 120 caracteres.");
        if (unit.Length is 0 or > 20)
            throw new BusinessRuleException("Escribí la unidad en que se cuenta (unidad, caja, ml), en hasta 20 caracteres.");
        if (command.MinStock < 0 || command.MinStock > 1000000)
            throw new BusinessRuleException("El stock mínimo va de 0 a 1000000.");
        var lower = name.ToLower();
        if (await store.Supplies.AnyAsync(s => s.Id != self && s.Name.ToLower() == lower, ct))
            throw new BusinessRuleException("Ya hay un insumo con ese nombre.");
        return (name, unit);
    }

    private static SupplyDto ToDto(Supply s) =>
        new(s.Id, s.Name, s.Unit, s.Stock, s.MinStock, s.IsActive, s.MinStock > 0 && s.Stock <= s.MinStock);
}
