namespace ClinicaSaaS.Domain;

public class Client
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public string Phone { get; set; } = "";
}
