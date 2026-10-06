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
    NoShow
}

public enum WaitlistStatus
{
    Pending,
    Offered,
    Accepted,
    Cancelled
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
