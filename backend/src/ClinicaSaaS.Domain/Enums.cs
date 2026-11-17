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

// Pending until the patient pays something, partially paid while their share is not covered, paid once it is.
public enum InvoiceStatus
{
    Pending,
    PartiallyPaid,
    Paid,
    Voided
}

// The fiscal vouchers a clinic issues to patients, who are final consumers: a registered taxpayer
// issues B, a monotributista or exempt clinic issues C, and each has its own credit note to cancel it.
public enum FiscalVoucherType
{
    FacturaB,
    FacturaC,
    NotaCreditoB,
    NotaCreditoC
}

// The clinic's standing with the tax authority, which decides the voucher letter.
public enum TaxCondition
{
    ResponsableInscripto,
    Monotributista,
    Exento
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

// How far back the admin dashboard looks: today only, or the last 7 or 30 days.
public enum MetricsRange
{
    Today,
    Week,
    Month
}

// The card networks a clinic usually takes; each one settles on its own.
public enum CardBrand
{
    Visa,
    Mastercard,
    Amex,
    Cabal,
    Naranja
}

public enum CashSessionStatus
{
    Open,
    Closed
}

// What moved money in or out of a register: a patient's payment or refund, or an entry made by hand.
public enum CashMovementKind
{
    Payment,
    Refund,
    Income,
    Expense
}

public enum InstallmentStatus
{
    Pending,
    Overdue,
    Paid
}

// A line of a patient's account: an invoice owed, its voiding, a payment received or money given back.
public enum AccountMovementKind
{
    Invoice,
    Voided,
    Payment,
    Refund
}

// What changed a supply's stock: a delivery, a visit that used it, or a correction after counting.
public enum SupplyMovementKind
{
    Entry,
    Consumption,
    Adjustment
}

// What a signed page of the clinical record holds. A visit is a closed consultation; an amendment corrects an
// earlier page without touching it; background is a fact that outlives visits (allergies, history); a consent
// records the written informed consent the patient signed for a procedure (Ley 26.529, art. 7).
public enum ClinicalEntryKind
{
    Visit,
    Amendment,
    Background,
    Consent
}

// The background facts every professional sees, whatever their specialty.
public enum BackgroundKind
{
    BloodType,
    Allergies,
    Medication,
    PersonalHistory,
    FamilyHistory,
    Habits,
    Notes
}

public enum ConsentDecision
{
    Accepted,
    Revoked
}

// Each time someone opens, copies or writes a patient's record (Ley 27.706, art. 6: trazabilidad).
public enum ClinicalAccessAction
{
    View,
    Copy,
    Sign,
    Amend,
    Attachment,
    // A professional who does not treat the patient opened the record in an emergency, giving the reason.
    Emergency
}

// What a person agreed to: the clinic's privacy notice, or (as told by the patient) being their emergency contact.
public enum ConsentKind
{
    PrivacyNotice,
    EmergencyContact
}

// Where the agreement was given: the online request, signing up, or at the front desk after being told.
public enum ConsentChannel
{
    OnlineRequest,
    SignUp,
    FrontDesk
}
