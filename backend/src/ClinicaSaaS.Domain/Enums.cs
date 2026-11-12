namespace ClinicaSaaS.Domain;

public enum TenantRole
{
    TenantAdmin,
    Doctor,
    Secretary,
    Patient
}

public enum AppointmentStatus
{
    Booked,
    CheckedIn,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
    Rescheduled
}

public enum WaitlistStatus
{
    Pending,
    Offered,
    Accepted,
    Cancelled
}

public enum BookingRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Rescheduled
}

public enum PaymentMethod
{
    Cash,
    Transfer,
    Card
}

public enum InvoiceStatus
{
    Pending,
    Paid,
    Voided
}

public enum AppointmentKind
{
    Consulta,
    Estudio
}

// Which account to enter with when one document holds both a staff and a patient account.
public enum LoginAccount
{
    Staff,
    Patient
}

// How far back the admin dashboard looks: the last 7, 30 or 90 days.
public enum MetricsRange
{
    Week,
    Month,
    Quarter
}
