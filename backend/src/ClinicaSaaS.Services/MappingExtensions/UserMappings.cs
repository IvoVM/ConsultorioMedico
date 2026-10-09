using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class UserMappings
{
    public static IQueryable<TenantAccount> ToAccounts(this IQueryable<TenantUser> query) =>
        query.Select(u => new TenantAccount(
            u.Id,
            u.Email ?? "",
            u.FirstName,
            u.LastName,
            u.Role,
            u.MustChangePassword));
}
