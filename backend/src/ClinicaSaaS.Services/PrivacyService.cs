using ClinicaSaaS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The privacy notice every form shows before taking personal data (Ley 25.326, art. 6) and the record that the
// person agreed to it. Health data is sensitive (art. 2 and 7), so nobody's data is kept without that record.
public class PrivacyService(
    IClinicSettingsStore settings,
    IOrganizationStore organization,
    IConsentStore consents,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    // Change it whenever the text below changes what a person agrees to: older agreements keep the version they read.
    public const string NoticeVersion = "2026-11-17";

    public async Task<PrivacyNoticeDto> NoticeAsync(CancellationToken ct)
    {
        var clinic = await settings.GetAsync(ct);
        var address = await organization.Locations
            .Where(l => l.IsActive && l.Address != "")
            .OrderBy(l => l.Name)
            .Select(l => l.Address)
            .FirstOrDefaultAsync(ct);
        var name = clinic.Name;
        var where = address is null ? name : $"{name}, {address}";
        var contact = string.IsNullOrWhiteSpace(clinic.PrivacyContact)
            ? "en la recepción del consultorio"
            : $"escribiendo o llamando a {clinic.PrivacyContact}";
        return new PrivacyNoticeDto(NoticeVersion, name,
        [
            new("Quién usa tus datos",
                $"{where} es responsable de la base de datos de pacientes. El sistema donde se guardan lo opera un proveedor que trata los datos solo por cuenta del consultorio y no los usa para nada más."),
            new("Qué datos y para qué",
                "Tu nombre, documento, teléfono y cobertura, y los datos de tus turnos, para darte turnos, avisarte si cambian, atenderte y cobrar la consulta. No los usamos para publicidad ni se los damos a terceros para eso."),
            new("Qué es obligatorio",
                "Nombre, documento y teléfono son necesarios para reservar: sin ellos no podemos darte el turno. La obra social es opcional; si no la cargás, el turno es particular."),
            new("Datos de salud",
                "La especialidad que pedís y lo que se escribe en tu historia clínica son datos sensibles. Nadie está obligado a darlos, pero sin ellos no podemos atenderte. Solo los leen vos y los profesionales que te atienden, bajo secreto profesional."),
            new("Quién más los recibe",
                "Tu obra social, si la usás, recibe lo necesario para autorizar y facturar la prestación. Una autoridad los recibe solo cuando una ley lo exige."),
            new("Tus derechos",
                $"Podés pedir ver, corregir, actualizar o borrar tus datos {contact}. La historia clínica no se borra: la ley obliga a guardarla al menos diez años (Ley 26.529, art. 18)."),
            new("Ley 25.326",
                "El titular de los datos personales tiene la facultad de ejercer el derecho de acceso a los mismos en forma gratuita a intervalos no inferiores a seis meses, salvo que se acredite un interés legítimo al efecto conforme lo establecido en el artículo 14, inciso 3 de la Ley Nº 25.326. La Agencia de Acceso a la Información Pública, en su carácter de Órgano de Control de la Ley Nº 25.326, tiene la atribución de atender las denuncias y reclamos que interpongan quienes resulten afectados en sus derechos por incumplimiento de las normas vigentes en materia de protección de datos personales.")
        ]);
    }

    // The person read this version of the notice and agreed. Throws when they did not, or read an older one.
    public static void RequireAccepted(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new BusinessRuleException("Leé el aviso de privacidad y marcá que lo aceptás para seguir.");
        if (version != NoticeVersion)
            throw new BusinessRuleException("El aviso de privacidad cambió. Volvé a leerlo y aceptalo para seguir.");
    }

    public static void RequireInformed(bool informed)
    {
        if (!informed)
            throw new BusinessRuleException("Contale al paciente el aviso de privacidad y marcá que lo hiciste antes de cargar sus datos.");
    }

    public Task RecordAsync(string documentNumber, Guid? clientId, ConsentChannel channel, CancellationToken ct) =>
        consents.AddAsync(new PrivacyConsent
        {
            Id = Guid.NewGuid(),
            DocumentNumber = documentNumber,
            ClientId = clientId,
            Kind = ConsentKind.PrivacyNotice,
            Channel = channel,
            NoticeVersion = NoticeVersion,
            RecordedBy = currentUser.Id,
            At = clock.GetUtcNow()
        }, ct);
}
