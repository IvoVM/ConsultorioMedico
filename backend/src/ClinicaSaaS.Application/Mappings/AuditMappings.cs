using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Application.Mappings;

public static class AuditMappings
{
    public static IQueryable<AuditEntryDto> ToAuditDtos(this IQueryable<AuditEntry> query) =>
        query.Select(a => new AuditEntryDto(a.Id, a.UserId, a.Action, a.Entity, a.EntityId, a.Detail, a.Timestamp));
}
