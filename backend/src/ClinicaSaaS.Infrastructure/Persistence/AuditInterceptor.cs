using ClinicaSaaS.Domain;
using ClinicaSaaS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClinicaSaaS.Infrastructure.Persistence;

// Stamps every Auditable row and logs each status or slot change of an appointment, whatever service made it.
public class AuditInterceptor(ICurrentUser currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;
        var now = clock.GetUtcNow();
        var user = currentUser.Id;

        foreach (var entry in context.ChangeTracker.Entries<Appointment>().ToList())
        {
            var changed = entry.State == EntityState.Added
                || (entry.State == EntityState.Modified
                    && (entry.Property(a => a.Status).IsModified || entry.Property(a => a.Start).IsModified || entry.Property(a => a.ProfessionalId).IsModified));
            if (!changed) continue;
            var appointment = entry.Entity;
            context.Add(new AppointmentChange
            {
                Id = Guid.NewGuid(),
                Appointment = appointment,
                Status = appointment.Status,
                Start = appointment.Start,
                End = appointment.End,
                ProfessionalId = appointment.ProfessionalId,
            });
        }

        foreach (var entry in context.ChangeTracker.Entries<Auditable>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy ??= user;
                entry.Entity.UpdatedAt = entry.Entity.CreatedAt;
                entry.Entity.UpdatedBy = entry.Entity.CreatedBy;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = user;
            }
        }
    }
}
