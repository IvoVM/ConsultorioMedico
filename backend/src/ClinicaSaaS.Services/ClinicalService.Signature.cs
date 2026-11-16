using ClinicaSaaS.Domain;
using ClinicaSaaS.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

// The professional's stamp and handwritten signature. The stamp is the data a paper stamp carries (name,
// specialty, license); the signature is a picture the professional draws or scans. Both are shown on each page
// they sign, but neither is what makes it signed: that is the electronic signature, the password typed again
// (Ley 25.506, art. 5). A picture of a signature is not a digital signature and does not replace one.
public partial class ClinicalService
{
    public async Task<SignatureStampDto> MySignatureAsync(CancellationToken ct)
    {
        var userId = DoctorId();
        var employee = await employees.Employees
            .Where(e => e.UserId == userId)
            .Select(e => new
            {
                Name = (e.User.FirstName + " " + e.User.LastName).Trim(),
                Specialty = e.Specialty != null ? e.Specialty.Name : null,
                e.LicenseNumber,
                e.SignatureId
            })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("No tenés ficha de profesional.");
        var signature = employee.SignatureId is Guid id ? await clinical.GetSignatureAsync(id, ct) : null;
        return new SignatureStampDto(
            employee.Name,
            employee.Specialty ?? NoSpecialty,
            Clean(employee.LicenseNumber),
            signature is not null,
            signature?.UploadedAt);
    }

    public async Task<FileDto> MySignatureImageAsync(CancellationToken ct)
    {
        var userId = DoctorId();
        var signatureId = await employees.Employees.Where(e => e.UserId == userId).Select(e => e.SignatureId).FirstOrDefaultAsync(ct);
        var signature = signatureId is Guid id ? await clinical.GetSignatureAsync(id, ct) : null;
        return signature is null
            ? throw new NotFoundException("Todavía no cargaste tu firma.")
            : new FileDto(signature.Content, "image/jpeg", "firma.jpg");
    }

    public async Task<SignatureStampDto> SaveSignatureAsync(SaveSignatureCommand command, CancellationToken ct)
    {
        var userId = DoctorId();
        if (string.IsNullOrEmpty(command.Password) || !await users.CheckPasswordAsync(userId, command.Password, ct))
            throw new BusinessRuleException("La contraseña no es correcta. La firma no se guardó.");
        byte[] content;
        try
        {
            content = Convert.FromBase64String(command.ContentBase64 ?? "");
        }
        catch (FormatException)
        {
            throw new BusinessRuleException("No se pudo leer la imagen de la firma.");
        }
        if (content.Length == 0 || content.Length > ProfessionalSignature.MaxBytes)
            throw new BusinessRuleException("La imagen de la firma tiene que pesar hasta 300 KB.");
        var info = Jpeg.Read(content);
        if (info is null || info.Components is not (1 or 3))
            throw new BusinessRuleException("La firma tiene que ser una imagen JPG.");
        if (info.Width < 80 || info.Height < 30 || info.Width > 2400 || info.Height > 1200)
            throw new BusinessRuleException("La imagen de la firma tiene que medir entre 80 × 30 y 2400 × 1200 píxeles.");

        var signature = new ProfessionalSignature
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = content,
            Sha256 = ClinicalLedger.Sha256(content),
            Width = info.Width,
            Height = info.Height,
            Components = info.Components,
            UploadedAt = clock.GetUtcNow()
        };
        await clinical.SetSignatureAsync(userId, signature, ct);
        await audit.RecordAsync(currentUser.Id, "firma ológrafa", "Profesional", userId.ToString(), signature.Sha256, ct);
        return await MySignatureAsync(ct);
    }

    // Pages already signed keep the picture they were signed with; only the next ones go without it.
    public async Task<SignatureStampDto> RemoveSignatureAsync(CancellationToken ct)
    {
        var userId = DoctorId();
        await clinical.SetSignatureAsync(userId, null, ct);
        await audit.RecordAsync(currentUser.Id, "quitar firma ológrafa", "Profesional", userId.ToString(), null, ct);
        return await MySignatureAsync(ct);
    }

    // The signature picture of one page, for whoever can see that page.
    public async Task<FileDto> EntrySignatureAsync(Guid entryId, CancellationToken ct)
    {
        var entry = await clinical.Entries.FirstOrDefaultAsync(e => e.Id == entryId && e.SignatureId != null, ct)
            ?? throw new NotFoundException("Firma no encontrada.");
        await EnsureCanSeeAsync(entry, ct);
        var signature = await clinical.GetSignatureAsync(entry.SignatureId!.Value, ct) ?? throw new NotFoundException("Firma no encontrada.");
        if (signature.Sha256 != entry.SignatureSha256 || ClinicalLedger.Sha256(signature.Content) != signature.Sha256)
            throw new BusinessRuleException("La firma no coincide con la del folio. Avisá al administrador.");
        return new FileDto(signature.Content, "image/jpeg", $"firma-folio-{entry.Folio}.jpg");
    }

    // The pictures to print on a copy, checked against the seal of each page that cites them.
    private async Task<Dictionary<Guid, ProfessionalSignature>> SignaturesForAsync(IEnumerable<ClinicalEntry> entries, CancellationToken ct)
    {
        var byId = new Dictionary<Guid, ProfessionalSignature?>();
        var result = new Dictionary<Guid, ProfessionalSignature>();
        foreach (var entry in entries)
        {
            if (entry.SignatureId is not Guid id)
                continue;
            if (!byId.TryGetValue(id, out var signature))
                byId[id] = signature = await clinical.GetSignatureAsync(id, ct);
            if (signature is not null && signature.Sha256 == entry.SignatureSha256 && ClinicalLedger.Sha256(signature.Content) == signature.Sha256)
                result[entry.Id] = signature;
        }
        return result;
    }

    private Guid DoctorId() =>
        IsDoctor && currentUser.Id is Guid id ? id : throw new BusinessRuleException("Solo un profesional tiene firma y sello.");
}
