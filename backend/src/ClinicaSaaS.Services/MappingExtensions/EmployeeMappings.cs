using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.MappingExtensions;

public static class EmployeeMappings
{
    public static IQueryable<EmployeeDto> ToEmployeeDtos(this IQueryable<Employee> query) =>
        query.Select(e => new EmployeeDto(
            e.UserId,
            e.User.FirstName,
            e.User.LastName,
            e.User.Email ?? "",
            e.DocumentNumber,
            e.User.Role,
            e.LicenseNumber,
            e.Specialty != null ? e.Specialty.Name : null,
            e.SpecialtyId,
            e.IsActive,
            e.Insurers.Select(i => i.Id).ToArray()));
}
