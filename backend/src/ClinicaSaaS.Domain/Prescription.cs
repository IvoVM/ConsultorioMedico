namespace ClinicaSaaS.Domain;

public class Prescription
{
    public Guid Id { get; set; }
    public Guid EncounterId { get; set; }
    public Guid ClientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public string? Instructions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PrescriptionItem> Items { get; set; } = [];
}
