using ClinicaSaaS.Domain;
using ClinicaSaaS.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class UsersService(
    ITenantUserStore users,
    IEmployeeStore employees,
    IClientStore clients,
    IRefreshSessionStore sessions,
    IAuditStore audit,
    ICurrentUser currentUser)
{
    public async Task<PageDto<AccountUserDto>> ListAsync(ListQuery query, CancellationToken ct)
    {
        var source = users.Users.AsQueryable();
        if (query.Term is string term)
            source = source.Where(user =>
                user.FirstName.ToLower().Contains(term) ||
                user.LastName.ToLower().Contains(term) ||
                (user.Email ?? "").ToLower().Contains(term) ||
                (user.Employee != null && (user.Employee.DocumentNumber ?? "").ToLower().Contains(term)) ||
                (user.Client != null && user.Client.DocumentNumber.ToLower().Contains(term)) ||
                (user.Role == TenantRole.Doctor ? "médico" : user.Role == TenantRole.Secretary ? "secretario" : user.Role == TenantRole.TenantAdmin ? "administrador" : "paciente").Contains(term));

        var ordered = (query.Sort, query.Desc) switch
        {
            ("email", false) => source.OrderBy(user => user.Email).ThenBy(user => user.LastName),
            ("email", true) => source.OrderByDescending(user => user.Email).ThenBy(user => user.LastName),
            ("role", false) => source.OrderBy(user => user.Role == TenantRole.TenantAdmin ? "Administrador" : user.Role == TenantRole.Doctor ? "Médico" : user.Role == TenantRole.Secretary ? "Secretario" : "Paciente").ThenBy(user => user.LastName),
            ("role", true) => source.OrderByDescending(user => user.Role == TenantRole.TenantAdmin ? "Administrador" : user.Role == TenantRole.Doctor ? "Médico" : user.Role == TenantRole.Secretary ? "Secretario" : "Paciente").ThenBy(user => user.LastName),
            ("document", false) => source.OrderBy(user => user.Employee != null ? user.Employee.DocumentNumber ?? "" : user.Client != null ? user.Client.DocumentNumber : "").ThenBy(user => user.LastName),
            ("document", true) => source.OrderByDescending(user => user.Employee != null ? user.Employee.DocumentNumber ?? "" : user.Client != null ? user.Client.DocumentNumber : "").ThenBy(user => user.LastName),
            ("name", true) => source.OrderByDescending(user => user.LastName).ThenByDescending(user => user.FirstName),
            _ => source.OrderBy(user => user.LastName).ThenBy(user => user.FirstName)
        };

        var rows = ordered.Select(user => new AccountUserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? "",
            user.Role,
            user.Employee != null ? user.Employee.DocumentNumber : user.Client != null ? user.Client.DocumentNumber : null));
        return await Pages.Materialize(rows, query, ct);
    }

    public async Task ResetPasswordAsync(Guid id, CancellationToken ct)
    {
        _ = await users.FindByIdAsync(id, ct) ?? throw new NotFoundException("Usuario no encontrado.");
        var document = await DocumentOfAsync(id, ct);
        if (string.IsNullOrWhiteSpace(document))
            throw new BusinessRuleException("Este usuario no tiene documento. Cargalo en la ficha antes de reiniciar la contraseña.");
        await users.SetDocumentPasswordAsync(id, document, ct);
        await sessions.RevokeUserAsync(id, ct);
        await audit.RecordAsync(currentUser.Id, "reinicio", "Usuario", id.ToString(), document, ct);
    }

    private async Task<string?> DocumentOfAsync(Guid id, CancellationToken ct)
    {
        var employee = await employees.Employees.Where(item => item.UserId == id).Select(item => item.DocumentNumber).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(employee))
            return employee;
        return await clients.Clients.Where(client => client.UserId == id).Select(client => client.DocumentNumber).FirstOrDefaultAsync(ct);
    }
}
