using ClinicaSaaS.Services.MappingExtensions;
using ClinicaSaaS.Services.Utilities;
using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public class EmployeesService(ITenantUserStore users, IEmployeeStore employees, IOrganizationStore organization, IAuditStore audit, ICurrentUser currentUser)
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
        await audit.RecordAsync(currentUser.Id, "alta", "Empleado", created.UserId.ToString(), created.Email, ct);
        var employee = await employees.Employees.Where(e => e.UserId == created.UserId).ToEmployeeDtos().FirstAsync(ct);
        return new CreatedEmployeeAccountDto(employee, created.Password);
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
                throw new BusinessRuleException($"No tiene el formato correcto: la especialidad «{specialtyName}» no está cargada. Creala en Especialidades y volvé a subir esta fila.");
            specialtyId = specialty.Id;
        }

        var password = TemporaryPassword.Generate();
        var account = await users.CreateAsync(email, password, firstName, lastName, role, true, ct);
        await employees.AddAsync(new Employee
        {
            Id = Guid.NewGuid(),
            UserId = account.Id,
            LicenseNumber = licenseNumber,
            SpecialtyId = specialtyId
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
