using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application.Mappings;

public static class OrganizationMappings
{
    public static IQueryable<LocationDto> ToLocationDtos(this IQueryable<Location> query) =>
        query.Select(l => new LocationDto(l.Id, l.Name, l.Address, l.IsActive));

    public static IQueryable<MedicalServiceDto> ToMedicalServiceDtos(this IQueryable<MedicalService> query) =>
        query.Select(s => new MedicalServiceDto(s.Id, s.LocationId, s.Name));

    public static IQueryable<SpecialtyDto> ToSpecialtyDtos(this IQueryable<Specialty> query) =>
        query.Select(s => new SpecialtyDto(s.Id, s.Name));

    public static IQueryable<AppointmentTypeDto> ToAppointmentTypeDtos(this IQueryable<AppointmentType> query) =>
        query.Select(t => new AppointmentTypeDto(t.Id, t.Name, t.DurationMinutes, t.SpecialtyId));

    public static IQueryable<ProfessionalDto> ToProfessionalDtos(this IQueryable<Employee> query) =>
        query.Select(e => new ProfessionalDto(
            e.UserId,
            e.User.FirstName,
            e.User.LastName,
            e.User.Email ?? "",
            e.LicenseNumber,
            e.SpecialtyId));
}
