using ClinicaSaaS.Domain;
using ClinicaSaaS.Domain.QueryViews;

namespace ClinicaSaaS.Application.Services.MappingExtensions;

public static class ScheduleMappings
{
    public static IQueryable<ScheduleBlockDto> ToScheduleBlockDtos(this IQueryable<ScheduleBlock> query) =>
        query.Select(b => new ScheduleBlockDto(b.Id, b.Day, b.StartTime, b.EndTime, b.LocationId, b.AppointmentTypeId));

    public static IQueryable<BlockoutDto> ToBlockoutDtos(this IQueryable<ScheduleBlockout> query) =>
        query.Select(b => new BlockoutDto(b.Id, b.ProfessionalId, b.Start, b.End, b.Reason));

    public static IQueryable<ScheduleOpening> ToOpenings(this IQueryable<ScheduleBlock> query) =>
        query.Select(b => new ScheduleOpening
        {
            ProfessionalId = b.ProfessionalId,
            ProfessionalFirstName = b.Professional.FirstName,
            ProfessionalLastName = b.Professional.LastName,
            LocationId = b.LocationId,
            AppointmentTypeId = b.AppointmentTypeId,
            AppointmentType = b.AppointmentType.Name,
            DurationMinutes = b.AppointmentType.DurationMinutes,
            StartTime = b.StartTime,
            EndTime = b.EndTime
        });

    public static IQueryable<BusyInterval> ToBusyIntervals(this IQueryable<Appointment> query) =>
        query.Select(a => new BusyInterval
        {
            ProfessionalId = a.ProfessionalId,
            Start = a.Start,
            End = a.End,
            Status = a.Status
        });

    public static IQueryable<AppointmentDto> ToAppointmentDtos(this IQueryable<Appointment> query) =>
        query.Select(a => new AppointmentDto(
            a.Id,
            a.ClientId,
            (a.Client.User.FirstName + " " + a.Client.User.LastName).Trim(),
            a.ProfessionalId,
            (a.Professional.FirstName + " " + a.Professional.LastName).Trim(),
            a.LocationId,
            a.Location.Name,
            a.AppointmentTypeId,
            a.AppointmentType.Name,
            a.Start,
            a.End,
            a.Status,
            a.VisitReason));

    public static IQueryable<WaitlistEntryDto> ToWaitlistDtos(this IQueryable<WaitlistEntry> query) =>
        query.Select(w => new WaitlistEntryDto(
            w.Id,
            w.ClientId,
            (w.Client.User.FirstName + " " + w.Client.User.LastName).Trim(),
            w.ProfessionalId,
            w.LocationId,
            w.SpecialtyId,
            w.Status,
            w.CreatedAt,
            w.Notes));
}
