using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Domain;

// Something a practice uses up (gloves, a syringe, gel). Stock goes down when the visit is closed.
public class Supply
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Stock { get; set; }
    // Below this the stock shows as low, so front desk knows to order more.
    public decimal MinStock { get; set; }
    public bool IsActive { get; set; } = true;


    public virtual ICollection<SupplyMovement> Movements { get; set; } = [];
}

// How much of a supply one appointment of a practice uses.
public class AppointmentTypeSupply
{
    public Guid AppointmentTypeId { get; set; }
    public Guid SupplyId { get; set; }
    public decimal Quantity { get; set; }


    public virtual AppointmentType AppointmentType { get; set; } = null!;
    public virtual Supply Supply { get; set; } = null!;
}

// Every change to a supply's stock, signed: what came in, what a visit used, or a correction after counting.
public class SupplyMovement
{
    public Guid Id { get; set; }
    public Guid SupplyId { get; set; }
    public SupplyMovementKind Kind { get; set; }
    public decimal Quantity { get; set; }
    public Guid? AppointmentId { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset At { get; set; }
    public Guid? UserId { get; set; }


    public virtual Supply Supply { get; set; } = null!;
    public virtual Appointment? Appointment { get; set; }
    public virtual TenantUser? User { get; set; }
}
