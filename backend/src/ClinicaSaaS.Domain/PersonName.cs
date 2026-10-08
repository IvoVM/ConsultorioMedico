namespace ClinicaSaaS.Domain;

public static class PersonName
{
    public static string Of(string? firstName, string? lastName) => $"{firstName} {lastName}".Trim();
}
