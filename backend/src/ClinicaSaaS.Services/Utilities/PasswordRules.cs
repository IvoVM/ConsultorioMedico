namespace ClinicaSaaS.Services.Utilities;

public static class PasswordRules
{
    public static string? Issue(string? password)
    {
        var value = password ?? "";
        if (value.Length < 8)
            return "La contraseña tiene que tener al menos 8 caracteres.";
        if (!value.Any(char.IsUpper))
            return "La contraseña tiene que incluir una mayúscula.";
        if (!value.Any(char.IsLower))
            return "La contraseña tiene que incluir una minúscula.";
        if (!value.Any(char.IsDigit))
            return "La contraseña tiene que incluir un número.";
        return null;
    }
}
