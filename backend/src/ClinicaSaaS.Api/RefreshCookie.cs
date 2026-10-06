namespace ClinicaSaaS.Api;

public static class RefreshCookie
{
    public const string Name = "clinica.refresh";
    public const string Path = "/api/acceso";

    public static void Set(HttpResponse response, string token, DateTimeOffset expires) =>
        response.Cookies.Append(Name, token, Options(expires));

    public static void Clear(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(null));

    private static CookieOptions Options(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = Path,
        Expires = expires,
        IsEssential = true
    };
}
