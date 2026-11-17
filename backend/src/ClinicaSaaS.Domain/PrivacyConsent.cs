namespace ClinicaSaaS.Domain;

// Proof that a person was told how their data is used (Ley 25.326, art. 5 and 6): what they agreed to, the version
// of the text they read, where and when. Front desk records it for a patient they told in person or by phone.
public class PrivacyConsent
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = "";
    public Guid? ClientId { get; set; }
    public ConsentKind Kind { get; set; }
    public ConsentChannel Channel { get; set; }
    public string NoticeVersion { get; set; } = "";
    public Guid? RecordedBy { get; set; }
    public DateTimeOffset At { get; set; }
}
