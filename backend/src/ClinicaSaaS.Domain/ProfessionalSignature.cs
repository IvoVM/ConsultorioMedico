namespace ClinicaSaaS.Domain;

// The handwritten signature a professional draws or scans, shown with their stamp (name, specialty, license) on
// the pages they sign. It is only the picture: what binds them to a page is the electronic signature, their
// password typed again (Ley 25.506, art. 5). Never changed once stored, so a page keeps the one it was signed
// with even after the professional replaces it.
public class ProfessionalSignature
{
    public const int MaxBytes = 300 * 1024;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    // JPEG on a white background, so the PDF copy can embed it as it is.
    public byte[] Content { get; set; } = [];
    public string Sha256 { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int Components { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
