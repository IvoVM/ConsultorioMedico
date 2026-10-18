using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Services.Utilities;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class EmployeesService(ITenantUserStore users, IEmployeeStore employees, IOrganizationStore organization, IAuditStore audit, IRefreshSessionStore sessions, ICurrentUser currentUser)
{
    private static readonly Dictionary<string, TenantRole> CsvRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Medico"] = TenantRole.Doctor,
        ["Médico"] = TenantRole.Doctor,
        ["Secretario"] = TenantRole.Secretary,
        ["AdminTenant"] = TenantRole.TenantAdmin
    };

    public async Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken ct) =>
        await employees.Employees
            .OrderBy(e => e.User.LastName)
            .ThenBy(e => e.User.FirstName)
            .ToEmployeeDtos()
            .ToListAsync(ct);

    public async Task<object> ListResultAsync(ListQuery query, CancellationToken ct)
    {
        if (!query.Paged) return await ListAsync(ct);
        var source = employees.Employees.AsQueryable();
        if (query.Term is string term)
            source = source.Where(e =>
                e.User.FirstName.ToLower().Contains(term) ||
                e.User.LastName.ToLower().Contains(term) ||
                (e.User.Email ?? "").ToLower().Contains(term) ||
                (e.LicenseNumber ?? "").ToLower().Contains(term) ||
                (e.Specialty != null && e.Specialty.Name.ToLower().Contains(term)) ||
                (e.User.Role == TenantRole.Doctor ? "médico" : e.User.Role == TenantRole.Secretary ? "secretario" : "administrador").Contains(term) ||
                (e.IsActive ? "activo" : "de baja").Contains(term));
        var ordered = (query.Sort, query.Desc) switch
        {
            ("email", false) => source.OrderBy(e => e.User.Email).ThenBy(e => e.User.LastName),
            ("email", true) => source.OrderByDescending(e => e.User.Email).ThenBy(e => e.User.LastName),
            ("role", false) => source.OrderBy(e => e.User.Role == TenantRole.TenantAdmin ? "Administrador" : e.User.Role == TenantRole.Doctor ? "Médico" : "Secretario").ThenBy(e => e.User.LastName),
            ("role", true) => source.OrderByDescending(e => e.User.Role == TenantRole.TenantAdmin ? "Administrador" : e.User.Role == TenantRole.Doctor ? "Médico" : "Secretario").ThenBy(e => e.User.LastName),
            ("license", false) => source.OrderBy(e => e.LicenseNumber ?? "").ThenBy(e => e.User.LastName),
            ("license", true) => source.OrderByDescending(e => e.LicenseNumber ?? "").ThenBy(e => e.User.LastName),
            ("specialty", false) => source.OrderBy(e => e.Specialty != null ? e.Specialty.Name : "").ThenBy(e => e.User.LastName),
            ("specialty", true) => source.OrderByDescending(e => e.Specialty != null ? e.Specialty.Name : "").ThenBy(e => e.User.LastName),
            ("status", false) => source.OrderBy(e => e.IsActive ? "activo" : "de baja").ThenBy(e => e.User.LastName),
            ("status", true) => source.OrderByDescending(e => e.IsActive ? "activo" : "de baja").ThenBy(e => e.User.LastName),
            ("name", true) => source.OrderByDescending(e => e.User.LastName).ThenByDescending(e => e.User.FirstName),
            _ => source.OrderBy(e => e.User.LastName).ThenBy(e => e.User.FirstName)
        };
        return await Pages.Materialize(ordered.ToEmployeeDtos(), query, ct);
    }

    public async Task<CreatedEmployeeAccountDto> CreateAsync(CreateEmployeeCommand command, CancellationToken ct)
    {
        var created = await CreateAccountAsync(
            command.FirstName,
            command.LastName,
            command.Email,
            RoleText(command.Role),
            command.LicenseNumber,
            command.Role == TenantRole.Doctor ? await SpecialtyNameAsync(command.SpecialtyId, ct) : null,
            ct);
        if (command.Role == TenantRole.Doctor && command.InsurerIds is { Count: > 0 })
            await employees.SetInsurersAsync(created.UserId, command.InsurerIds, ct);
        await audit.RecordAsync(currentUser.Id, "alta", "Empleado", created.UserId.ToString(), created.Email, ct);
        var employee = await employees.Employees.Where(e => e.UserId == created.UserId).ToEmployeeDtos().FirstAsync(ct);
        return new CreatedEmployeeAccountDto(employee, created.Password);
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeCommand command, CancellationToken ct)
    {
        var current = await employees.Employees.Where(e => e.UserId == id)
            .Select(e => new { e.User.Role, e.IsActive })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Empleado no encontrado.");

        var firstName = command.FirstName?.Trim() ?? "";
        var lastName = command.LastName?.Trim() ?? "";
        var email = command.Email?.Trim() ?? "";
        var license = string.IsNullOrWhiteSpace(command.LicenseNumber) ? null : command.LicenseNumber.Trim();
        if (firstName.Length == 0 || lastName.Length == 0 || email.Length == 0)
            throw new BusinessRuleException("Nombre, apellido y email son obligatorios.");
        if (firstName.Length > 80 || lastName.Length > 80)
            throw new BusinessRuleException("El nombre y el apellido no pueden pasar de 80 caracteres.");
        if (!email.Contains('@') || email.StartsWith('@') || email.EndsWith('@') || email.Length > 120)
            throw new BusinessRuleException("El email no es válido.");
        if (command.Role is not (TenantRole.Doctor or TenantRole.Secretary or TenantRole.TenantAdmin))
            throw new BusinessRuleException("El rol tiene que ser médico, secretario o administrador.");
        if (id == currentUser.Id && command.Role != current.Role)
            throw new BusinessRuleException("No podés cambiar tu propio rol.");

        var taken = await users.FindByEmailAsync(email, ct);
        if (taken is not null && taken.Id != id)
            throw new BusinessRuleException("Ese email ya está cargado.");

        Guid? specialtyId = null;
        if (command.Role == TenantRole.Doctor)
        {
            if (command.SpecialtyId is not Guid specialty)
                throw new BusinessRuleException("Un médico necesita una especialidad.");
            _ = await organization.GetSpecialtyAsync(specialty, ct)
                ?? throw new BusinessRuleException("La especialidad elegida no está cargada.");
            specialtyId = specialty;
            if (license is not null && license.Length > 40)
                throw new BusinessRuleException("La matrícula no puede pasar de 40 caracteres.");
        }
        else
        {
            license = null;
        }

        if (current.Role == TenantRole.TenantAdmin && current.IsActive && command.Role != TenantRole.TenantAdmin)
            await EnsureAnotherAdminAsync(id, ct);

        await users.UpdateProfileAsync(id, firstName, lastName, email, command.Role, ct);
        await employees.UpdateAsync(id, license, specialtyId, ct);
        await employees.SetInsurersAsync(id, command.Role == TenantRole.Doctor ? command.InsurerIds ?? [] : [], ct);
        await audit.RecordAsync(currentUser.Id, "edicion", "Empleado", id.ToString(), email, ct);
        return await employees.Employees.Where(e => e.UserId == id).ToEmployeeDtos().FirstAsync(ct);
    }

    public async Task<EmployeeDto> SetActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var current = await employees.Employees.Where(e => e.UserId == id)
            .Select(e => new { e.User.Role, e.User.Email, e.IsActive })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Empleado no encontrado.");
        if (!active && id == currentUser.Id)
            throw new BusinessRuleException("No podés darte de baja.");
        if (!active && current.Role == TenantRole.TenantAdmin && current.IsActive)
            await EnsureAnotherAdminAsync(id, ct);
        if (current.IsActive != active)
        {
            await employees.SetActiveAsync(id, active, ct);
            if (!active)
                await sessions.RevokeUserAsync(id, ct);
            await audit.RecordAsync(currentUser.Id, active ? "alta" : "baja", "Empleado", id.ToString(), current.Email, ct);
        }
        return await employees.Employees.Where(e => e.UserId == id).ToEmployeeDtos().FirstAsync(ct);
    }

    private async Task EnsureAnotherAdminAsync(Guid exceptId, CancellationToken ct)
    {
        var others = await employees.Employees.CountAsync(
            e => e.IsActive && e.User.Role == TenantRole.TenantAdmin && e.UserId != exceptId, ct);
        if (others == 0)
            throw new BusinessRuleException("Tiene que quedar al menos un administrador activo.");
    }

    public async Task<EmployeeImportDto> ImportAsync(ImportEmployeesCommand command, CancellationToken ct)
    {
        var lines = command.Csv.TrimStart('\uFEFF').Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
            throw new BusinessRuleException("El archivo necesita el encabezado y al menos una fila.");

        var created = new List<CreatedEmployeeDto>();
        var rejected = new List<RejectedRowDto>();
        for (var i = 1; i < lines.Length; i++)
        {
            var row = i + 1;
            var cols = lines[i].Split(',');
            if (cols.Length < 4)
            {
                rejected.Add(Skip(row, "No tiene el formato correcto: faltan columnas. Cada fila es nombre, apellido, email, rol, matrícula y especialidad."));
                continue;
            }

            try
            {
                var account = await CreateAccountAsync(
                    cols[0],
                    cols[1],
                    cols[2],
                    cols[3],
                    cols.Length > 4 ? cols[4] : null,
                    cols.Length > 5 ? cols[5] : null,
                    ct);
                created.Add(new CreatedEmployeeDto(row, account.Email, account.Password, account.Role));
            }
            catch (BusinessRuleException ex)
            {
                rejected.Add(Skip(row, ex.Message));
            }
        }

        await audit.RecordAsync(currentUser.Id, "importacion", "Empleado", null, $"{created.Count} creados, {rejected.Count} rechazados", ct);
        return new EmployeeImportDto(created, rejected);
    }

    private async Task<NewAccount> CreateAccountAsync(
        string? firstName,
        string? lastName,
        string? email,
        string? roleText,
        string? licenseNumber,
        string? specialtyName,
        CancellationToken ct)
    {
        firstName = firstName?.Trim() ?? "";
        lastName = lastName?.Trim() ?? "";
        email = email?.Trim() ?? "";
        roleText = roleText?.Trim() ?? "";
        licenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber.Trim();
        specialtyName = string.IsNullOrWhiteSpace(specialtyName) ? null : specialtyName.Trim();

        if (firstName.Length == 0 || lastName.Length == 0 || email.Length == 0)
            throw new BusinessRuleException("No tiene el formato correcto: nombre, apellido y email son obligatorios.");
        if (!email.Contains('@') || email.StartsWith('@') || email.EndsWith('@'))
            throw new BusinessRuleException("No tiene el formato correcto: el email no es válido.");
        if (!CsvRoles.TryGetValue(roleText, out var role))
            throw new BusinessRuleException("No tiene el formato correcto: el rol tiene que ser Medico, Secretario o AdminTenant.");

        if (await users.FindByEmailAsync(email, ct) is not null)
            throw new BusinessRuleException("Ese email ya está cargado, así que esta persona no se agrega.");

        Guid? specialtyId = null;
        if (role == TenantRole.Doctor)
        {
            if (specialtyName is null)
                throw new BusinessRuleException("No tiene el formato correcto: un médico necesita una especialidad que ya esté cargada.");
            var specialty = await organization.GetSpecialtyByNameAsync(specialtyName, ct);
            if (specialty is null)
                throw new BusinessRuleException($"No tiene el formato correcto: la especialidad «{specialtyName}» no está cargada. Creála en Especialidades y volvé a subir esta fila.");
            specialtyId = specialty.Id;
        }

        var password = TemporaryPassword.Generate();
        var account = await users.CreateAsync(email, password, firstName, lastName, role, true, ct);
        await employees.AddAsync(new Employee
        {
            Id = Guid.NewGuid(),
            UserId = account.Id,
            LicenseNumber = licenseNumber,
            SpecialtyId = specialtyId,
            IsActive = true
        }, ct);
        return new NewAccount(account.Id, account.Email, password, role);
    }

    private async Task<string?> SpecialtyNameAsync(Guid? specialtyId, CancellationToken ct)
    {
        if (specialtyId is not Guid id)
            return null;
        var specialty = await organization.GetSpecialtyAsync(id, ct)
            ?? throw new BusinessRuleException("No tiene el formato correcto: la especialidad elegida no está cargada.");
        return specialty.Name;
    }

    private static string RoleText(TenantRole role) => role switch
    {
        TenantRole.Doctor => "Medico",
        TenantRole.Secretary => "Secretario",
        TenantRole.TenantAdmin => "AdminTenant",
        _ => ""
    };

    private static RejectedRowDto Skip(int row, string reason) =>
        new(row, $"La fila {row} no se carga. {reason}");

    private sealed record NewAccount(Guid UserId, string Email, string Password, TenantRole Role);
}
