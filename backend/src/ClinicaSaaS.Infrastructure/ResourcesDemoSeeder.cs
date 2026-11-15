using ClinicaSaaS.Domain;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure;

// Demo rooms, equipment and supplies on top of the demo clinic: two rooms in the main site (Martín in the
// morning and Sofía in the afternoon share the first one), one in the annex, an ECG machine the
// electrocardiogram needs, and the supplies each practice uses with some stock (ECG paper already low).
// Existing blocks and appointments take their professional's room. It runs once, when the clinic has no
// room yet, and does nothing if the demo doctors are missing. Codes are the national nomenclator's.
public class ResourcesDemoSeeder(TenantDbContext db)
{
    public async Task SeedAsync(CancellationToken ct)
    {
        if (await db.Rooms.AnyAsync(ct))
            return;
        var emails = new[] { "medico@demo.local", "elena.vazquez@demo.local", "julian.costa@demo.local", "sofia.herrera@demo.local" };
        var doctors = await db.Users.Where(u => emails.Contains(u.Email!)).ToDictionaryAsync(u => u.Email!, u => u.Id, ct);
        var main = await db.Locations.Where(l => l.Name != "Anexo Thames").OrderBy(l => l.Name).FirstOrDefaultAsync(ct);
        var annex = await db.Locations.FirstOrDefaultAsync(l => l.Name == "Anexo Thames", ct);
        if (doctors.Count < emails.Length || main is null || annex is null)
            return;

        var one = Room(main.Id, "Consultorio 1");
        var two = Room(main.Id, "Consultorio 2");
        Room(main.Id, "Consultorio 3");
        var annexRoom = Room(annex.Id, "Consultorio A");
        var roomOf = new Dictionary<Guid, Guid>
        {
            [doctors["medico@demo.local"]] = one.Id,
            [doctors["sofia.herrera@demo.local"]] = one.Id,
            [doctors["elena.vazquez@demo.local"]] = two.Id,
            [doctors["julian.costa@demo.local"]] = annexRoom.Id,
        };
        var ecgMachine = new Equipment { Id = Guid.NewGuid(), LocationId = main.Id, Name = "Electrocardiógrafo", IsActive = true };
        db.Equipment.Add(ecgMachine);
        db.Equipment.Add(new Equipment { Id = Guid.NewGuid(), LocationId = annex.Id, Name = "Ecógrafo", IsActive = true });
        await db.SaveChangesAsync(ct);

        // Straight to the database, so the demo appointments keep who last changed them and when.
        foreach (var (professionalId, roomId) in roomOf)
        {
            await db.ScheduleBlocks.Where(b => b.ProfessionalId == professionalId).ExecuteUpdateAsync(s => s.SetProperty(b => b.RoomId, roomId), ct);
            await db.Appointments.Where(a => a.ProfessionalId == professionalId).ExecuteUpdateAsync(s => s.SetProperty(a => a.RoomId, roomId), ct);
        }

        var types = await db.AppointmentTypes.ToDictionaryAsync(t => t.Name, ct);
        var gloves = Supply("Guantes de látex", "par", 120m, 40m);
        var depressor = Supply("Bajalenguas", "unidad", 300m, 100m);
        var electrodes = Supply("Electrodos descartables", "unidad", 250m, 100m);
        var paper = Supply("Papel para electrocardiograma", "hoja", 18m, 30m);
        var gel = Supply("Gel conductor", "ml", 900m, 250m);
        var bandage = Supply("Venda elástica", "unidad", 25m, 10m);
        await db.SaveChangesAsync(ct);

        Practice(types, "Consulta clínica", "42.01.01", null, (gloves, 1m), (depressor, 1m));
        Practice(types, "Control", "42.01.01", null, (gloves, 1m));
        Practice(types, "Consulta pediátrica", "42.01.01", null, (gloves, 1m), (depressor, 1m));
        Practice(types, "Consulta traumatológica", "42.01.01", null, (gloves, 1m), (bandage, 1m));
        Practice(types, "Electrocardiograma", "17.01.01", ecgMachine.Id, (electrodes, 10m), (paper, 1m), (gel, 5m), (gloves, 1m));
        if (types.TryGetValue("Electrocardiograma", out var ecg))
            await db.Appointments.Where(a => a.AppointmentTypeId == ecg.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.EquipmentId, ecgMachine.Id), ct);

        // Two overbooks per professional and day, so the control shows on the agenda.
        var settings = await db.ClinicSettings.FirstOrDefaultAsync(ct);
        if (settings is not null && settings.MaxOverbooksPerDay is null)
            settings.MaxOverbooksPerDay = 2;
        await db.SaveChangesAsync(ct);
    }

    private Room Room(Guid locationId, string name)
    {
        var room = new Room { Id = Guid.NewGuid(), LocationId = locationId, Name = name, IsActive = true };
        db.Rooms.Add(room);
        return room;
    }

    private Supply Supply(string name, string unit, decimal stock, decimal min)
    {
        var supply = new Supply { Id = Guid.NewGuid(), Name = name, Unit = unit, Stock = stock, MinStock = min, IsActive = true };
        db.Supplies.Add(supply);
        db.SupplyMovements.Add(new SupplyMovement
        {
            Id = Guid.NewGuid(),
            SupplyId = supply.Id,
            Kind = SupplyMovementKind.Entry,
            Quantity = stock,
            Note = "Stock inicial",
            At = DateTimeOffset.UtcNow.AddDays(-30)
        });
        return supply;
    }

    private void Practice(Dictionary<string, AppointmentType> types, string name, string code, Guid? equipmentId, params (Supply Supply, decimal Quantity)[] supplies)
    {
        if (!types.TryGetValue(name, out var type))
            return;
        type.Code = code;
        type.EquipmentId = equipmentId;
        foreach (var (supply, quantity) in supplies)
            db.AppointmentTypeSupplies.Add(new AppointmentTypeSupply { AppointmentTypeId = type.Id, SupplyId = supply.Id, Quantity = quantity });
    }
}
