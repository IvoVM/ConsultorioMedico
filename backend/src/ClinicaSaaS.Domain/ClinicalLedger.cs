using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClinicaSaaS.Domain;

// How a page of the clinical record is sealed: its fields, in a fixed order, plus the previous page's hash.
// Recomputing it later tells whether anything changed since it was written.
public static class ClinicalLedger
{
    public const int MaxAttachmentBytes = 5 * 1024 * 1024;

    public static readonly string[] AttachmentTypes = ["application/pdf", "image/jpeg", "image/png"];

    // Postgres keeps microseconds; a time with more precision would not hash the same after a round trip.
    public static DateTimeOffset Truncate(DateTimeOffset moment) =>
        new(moment.UtcTicks - moment.UtcTicks % TimeSpan.TicksPerMillisecond, TimeSpan.Zero);

    // A page signed with a handwritten signature seals it too (v2). Pages without one keep the v1 layout, so the
    // pages sealed before signatures existed still verify.
    public static string ComputeHash(ClinicalEntry entry)
    {
        var withSignature = entry.SignatureSha256 is not null;
        object?[] fields =
        [
            withSignature ? "v2" : "v1",
            entry.PreviousHash ?? "",
            entry.Folio,
            entry.Kind.ToString(),
            entry.ClientId,
            entry.EncounterId,
            entry.AmendsId,
            entry.SpecialtyId,
            entry.SpecialtyName,
            entry.AuthorId,
            entry.AuthorName,
            entry.AuthorLicense,
            Truncate(entry.RecordedAt).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            entry.Signed,
            entry.Text,
            entry.Plan,
            entry.BloodPressure,
            entry.HeartRate,
            Number(entry.Temperature),
            Number(entry.WeightKg),
            entry.Diagnoses,
            entry.BackgroundKind?.ToString(),
            entry.ConsentDecision?.ToString(),
            entry.AttachmentId,
            entry.AttachmentSha256
        ];
        if (withSignature)
            fields = [.. fields, entry.SignatureId, entry.SignatureSha256];
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fields)));
        return Convert.ToHexStringLower(bytes);
    }

    public static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    // Seals a page as the next one after `previous` (null for the first page of the record).
    public static void Seal(ClinicalEntry entry, ClinicalEntry? previous)
    {
        entry.RecordedAt = Truncate(entry.RecordedAt);
        entry.Folio = (previous?.Folio ?? 0) + 1;
        entry.PreviousHash = previous?.Hash;
        entry.Hash = ComputeHash(entry);
    }

    // The first folio whose seal no longer matches its contents or its place in the chain, or null if all hold.
    public static int? FirstBroken(IEnumerable<ClinicalEntry> byFolio)
    {
        string? previous = null;
        var expected = 1;
        foreach (var entry in byFolio)
        {
            if (entry.Folio != expected || entry.PreviousHash != previous || entry.Hash != ComputeHash(entry))
                return entry.Folio ?? expected;
            previous = entry.Hash;
            expected++;
        }
        return null;
    }

    private static string? Number(decimal? value) => value?.ToString("0.###", CultureInfo.InvariantCulture);
}
