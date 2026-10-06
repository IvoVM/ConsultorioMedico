using System.Text;
using Microsoft.Extensions.Configuration;

namespace ClinicaSaaS.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string Key { get; set; } = "";
    public int AccessMinutes { get; set; } = 15;
    public int RefreshDays { get; set; } = 14;

    public static JwtOptions Bind(IConfiguration configuration)
    {
        var options = new JwtOptions();
        configuration.GetSection(SectionName).Bind(options);
        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
            throw new InvalidOperationException("Faltan Jwt:Issuer o Jwt:Audience.");
        if (string.IsNullOrWhiteSpace(options.Key) || Encoding.UTF8.GetByteCount(options.Key) < 32)
            throw new InvalidOperationException("Jwt:Key debe tener al menos 32 bytes.");
        if (options.AccessMinutes <= 0)
            throw new InvalidOperationException("Jwt:AccessMinutes debe ser mayor a cero.");
        if (options.RefreshDays <= 0)
            throw new InvalidOperationException("Jwt:RefreshDays debe ser mayor a cero.");
        return options;
    }
}
