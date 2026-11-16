using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Infrastructure.Persistence;

// Brings what was written before the signed ledger into it, at startup: visits closed before signing existed
// and the background the migration moved out of the old patient file. Each gets the next folio and its seal,
// marked as unsigned, since nobody signed it then. The demo clinic's sample pages count as signed by the
// professional who appears as their author.
public static class ClinicalLedgerSealer
{
    public static async Task SealAsync(TenantDbContext db, bool demo, CancellationToken ct)
    {
        var licenses = await db.Employees
            .Where(e => e.LicenseNumber != null)
            .ToDictionaryAsync(e => e.UserId, e => e.LicenseNumber!, ct);
        var closed = await db.Encounters
            .Where(e => e.IsClosed && !db.ClinicalEntries.Any(x => x.EncounterId == e.Id))
            .Select(e => new
            {
                e.Id,
                e.ClientId,
                e.SpecialtyId,
                Specialty = e.Specialty != null ? e.Specialty.Name : "",
                e.ProfessionalId,
                Author = (e.Professional.FirstName + " " + e.Professional.LastName).Trim(),
                e.Note,
                e.Plan,
                e.BloodPressure,
                e.HeartRate,
                e.Temperature,
                e.WeightKg,
                e.CreatedAt,
                Diagnoses = e.Diagnoses.OrderBy(d => d.Diagnosis!.Code).Select(d => d.Diagnosis!.Code + " " + d.Diagnosis.Name).ToList()
            })
            .ToListAsync(ct);
        foreach (var visit in closed)
        {
            db.ClinicalEntries.Add(new ClinicalEntry
            {
                Id = Guid.NewGuid(),
                ClientId = visit.ClientId,
                Kind = ClinicalEntryKind.Visit,
                EncounterId = visit.Id,
                SpecialtyId = visit.SpecialtyId,
                SpecialtyName = visit.Specialty,
                AuthorId = visit.ProfessionalId,
                AuthorName = visit.Author,
                AuthorLicense = licenses.GetValueOrDefault(visit.ProfessionalId),
                RecordedAt = visit.CreatedAt,
                Text = visit.Note ?? "",
                Plan = visit.Plan,
                BloodPressure = visit.BloodPressure,
                HeartRate = visit.HeartRate,
                Temperature = visit.Temperature,
                WeightKg = visit.WeightKg,
                Diagnoses = visit.Diagnoses.Count > 0 ? string.Join("; ", visit.Diagnoses) : null
            });
        }
        await db.SaveChangesAsync(ct);

        var pending = await db.ClinicalEntries.Where(e => e.Hash == null).ToListAsync(ct);
        foreach (var patient in pending.GroupBy(e => e.ClientId))
        {
            var previous = await db.ClinicalEntries.AsNoTracking()
                .Where(e => e.ClientId == patient.Key && e.Hash != null)
                .OrderByDescending(e => e.Folio)
                .FirstOrDefaultAsync(ct);
            foreach (var entry in patient.OrderBy(e => e.RecordedAt).ThenBy(e => e.Kind))
            {
                entry.Signed = demo && entry.AuthorLicense is not null;
                ClinicalLedger.Seal(entry, previous);
                previous = entry;
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
