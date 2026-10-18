namespace ClinicaSaaS.Services.Utilities;

public static class DocumentRules
{
    public static string Normalize(string? value)
    {
        var trimmed = value?.Trim() ?? "";
        var compact = trimmed.Replace(".", "").Replace("-", "").Replace(" ", "");
        if (compact.Length > 0 && compact.All(char.IsDigit))
            return compact;
        return trimmed;
    }

    public static bool IsValid(string normalized) => normalized.Length is >= 6 and <= 20;
}
