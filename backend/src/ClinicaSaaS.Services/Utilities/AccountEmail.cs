namespace ClinicaSaaS.Services.Utilities;

public static class AccountEmail
{
    public static string ForPatient(string document) => Address(document, "pacientes.local");

    public static string ForStaff(string document) => Address(document, "personal.local");

    private static string Address(string document, string domain)
    {
        var local = new string(document.Where(char.IsLetterOrDigit).ToArray());
        return $"{local}@{domain}";
    }
}
