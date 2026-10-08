using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application;

public class EmployeesService(ITenantUserStore users, IEmployeeStore employees, IOrganizationStore organization, IAuditStore audit, ICurrentUser currentUser)
{
    private static readonly Dictionary<string, TenantRole> CsvRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Medico"] = TenantRole.Doctor,
        ["Médico"] = TenantRole.Doctor,
        ["Secretario"] = TenantRole.Secretary,
        ["AdminTenant"] = TenantRole.TenantAdmin
    };

    public async Task<EmployeeImportDto> ImportAsync(ImportEmployeesCommand command, CancellationToken ct)
    {
        var lines = command.Csv.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
            throw new BusinessRuleException("El CSV debe incluir encabezado y al menos una fila.");

        var created = new List<CreatedEmployeeDto>();
        var rejected = new List<RejectedRowDto>();
        for (var i = 1; i < lines.Length; i++)
        {
            var row = i + 1;
            var cols = lines[i].Split(',');
            if (cols.Length < 4)
            {
                rejected.Add(new RejectedRowDto(row, "Faltan columnas."));
                continue;
            }

            var firstName = cols[0].Trim();
            var lastName = cols[1].Trim();
            var email = cols[2].Trim();
            var roleText = cols[3].Trim();
            var licenseNumber = cols.Length > 4 ? cols[4].Trim() : null;
            var specialtyName = cols.Length > 5 ? cols[5].Trim() : null;
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            {
                rejected.Add(new RejectedRowDto(row, "Nombre, apellido y email son obligatorios."));
                continue;
            }

            if (!CsvRoles.TryGetValue(roleText, out var role))
            {
                rejected.Add(new RejectedRowDto(row, "El rol debe ser Medico, Secretario o AdminTenant."));
                continue;
            }

            if (await users.FindByEmailAsync(email, ct) is not null)
            {
                rejected.Add(new RejectedRowDto(row, "El email ya existe."));
                continue;
            }

            Guid? specialtyId = null;
            if (role == TenantRole.Doctor)
            {
                if (string.IsNullOrWhiteSpace(specialtyName))
                {
                    rejected.Add(new RejectedRowDto(row, "El médico necesita una especialidad."));
                    continue;
                }

                var specialty = await organization.GetSpecialtyByNameAsync(specialtyName, ct);
                if (specialty is null)
                {
                    rejected.Add(new RejectedRowDto(row, "Especialidad inexistente."));
                    continue;
                }

                specialtyId = specialty.Id;
            }

            var password = TemporaryPassword.Generate();
            var account = await users.CreateAsync(email, password, firstName, lastName, role, true, ct);
            await employees.AddAsync(new Employee
            {
                Id = Guid.NewGuid(),
                UserId = account.Id,
                LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber,
                SpecialtyId = specialtyId
            }, ct);
            created.Add(new CreatedEmployeeDto(row, account.Email, password, role));
        }

        await audit.RecordAsync(currentUser.Id, "importacion", "Empleado", null, $"{created.Count} creados, {rejected.Count} rechazados", ct);
        return new EmployeeImportDto(created, rejected);
    }
}

public static class TemporaryPassword
{
    public static string Generate()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return $"Tmp{new string(chars)}1a";
    }
}
