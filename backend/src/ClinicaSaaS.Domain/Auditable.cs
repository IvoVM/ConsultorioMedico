using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

// Who created a row and who touched it last. The tenant context fills these on save, so services never set them.
public abstract class Auditable
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }


    public virtual TenantUser? CreatedByUser { get; set; }
    public virtual TenantUser? UpdatedByUser { get; set; }
}
