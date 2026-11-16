namespace ClinicaSaaS.Domain;

// One numbered page (folio) of a patient's single clinical record. Once written it is never edited or deleted:
// the database refuses it, and a mistake is fixed with an amendment that points here. Each page carries the hash
// of the one before, so any change to an old page breaks the chain and shows.
public class ClinicalEntry
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    // Null only while a page brought over from before the ledger waits to be sealed at startup.
    public int? Folio { get; set; }
    public ClinicalEntryKind Kind { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid? AmendsId { get; set; }
    public Guid? SpecialtyId { get; set; }
    public string SpecialtyName { get; set; } = "";
    public Guid? AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string? AuthorLicense { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    // False for pages written before electronic signature existed: they stay, marked as unsigned.
    public bool Signed { get; set; }
    public string Text { get; set; } = "";
    public string? Plan { get; set; }
    public string? BloodPressure { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Diagnoses { get; set; }
    public BackgroundKind? BackgroundKind { get; set; }
    public ConsentDecision? ConsentDecision { get; set; }
    public Guid? AttachmentId { get; set; }
    public string? AttachmentSha256 { get; set; }
    public string? PreviousHash { get; set; }
    public string? Hash { get; set; }


    public virtual Client Client { get; set; } = null!;
    public virtual ClinicalEntry? Amends { get; set; }
    public virtual ClinicalAttachment? Attachment { get; set; }
}
