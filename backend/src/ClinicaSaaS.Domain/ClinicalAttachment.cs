namespace ClinicaSaaS.Domain;

// A scanned document that belongs to the record, such as the informed consent the patient signed on paper.
// Like the pages, it is never changed: its SHA-256 is part of the page that cites it.
public class ClinicalAttachment
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] Content { get; set; } = [];
    public string Sha256 { get; set; } = "";
    public DateTimeOffset UploadedAt { get; set; }
}
