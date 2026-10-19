using ClinicaSaaS.Services;

namespace ClinicaSaaS.Services.Utilities;

public static class PhoneRules
{
    private static readonly HashSet<string> Area3 =
    [
        "220", "221", "223", "230", "236", "237", "249",
        "260", "261", "263", "264", "266", "280", "291", "294", "297", "298", "299",
        "336", "341", "342", "343", "345", "348",
        "351", "353", "358",
        "362", "364", "370", "376", "379", "380", "381", "383", "385", "387", "388"
    ];

    public static string Require(string? value)
    {
        if (!TryFormat(value, out var formatted))
            throw new BusinessRuleException("El teléfono no es válido.");
        return formatted;
    }

    public static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Require(value);

    public static bool TryFormat(string? value, out string formatted)
    {
        formatted = "";
        var national = National(value);
        if (national is null)
            return false;
        var area = national.StartsWith("11", StringComparison.Ordinal) ? 2
            : Area3.Contains(national[..3]) ? 3
            : 4;
        formatted = $"+54 {national[..area]} {national[area..]}";
        return true;
    }

    private static string? National(string? value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
            digits = digits[2..];
        if (digits.StartsWith("54", StringComparison.Ordinal))
            digits = digits[2..];
        if (digits.StartsWith('0'))
            digits = digits[1..];
        if (digits.StartsWith('9') && digits.Length > 10)
            digits = digits[1..];
        if (digits.StartsWith("15", StringComparison.Ordinal) && digits.Length == 12)
            digits = digits[2..];
        if (digits.Length == 12)
            digits = WithoutMobileMarker(digits);
        return digits.Length == 10 ? digits : null;
    }

    private static string WithoutMobileMarker(string digits)
    {
        if (digits.StartsWith("11", StringComparison.Ordinal) && digits[2..4] == "15")
            return "11" + digits[4..];
        if (Area3.Contains(digits[..3]) && digits[3..5] == "15")
            return digits[..3] + digits[5..];
        if (digits[4..6] == "15")
            return digits[..4] + digits[6..];
        return digits;
    }
}
