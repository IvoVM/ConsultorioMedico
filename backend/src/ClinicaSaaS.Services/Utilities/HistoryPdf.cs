using System.Globalization;
using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.Utilities;

public record HistoryCopy(
    string Clinic,
    PatientSummaryDto Patient,
    IReadOnlyList<ClinicalEntryDto> Entries,
    int? BrokenFolio,
    string Scope,
    string IssuedBy,
    DateTimeOffset IssuedAt);

// The copy of the clinical record the patient is entitled to (Ley 26.529, art. 14): every page in folio order,
// with who signed it, their license and the page's seal, so the copy can be checked against the system.
public static class HistoryPdf
{
    private const float Left = 48;
    private const float Right = PdfDocument.Width - 48;
    private const float Bottom = 64;
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-AR");

    public static byte[] Render(HistoryCopy copy, TimeZoneInfo zone)
    {
        var pdf = new PdfDocument();
        var y = PdfDocument.Height - 56;
        var page = 1;

        void Footer()
        {
            pdf.Line(Left, 48, Right, 48);
            pdf.Text(Left, 36, 8, false, $"Copia de la historia clínica N.º {copy.Patient.HistoryNumber} · {copy.Patient.Name} · emitida el {Local(copy.IssuedAt, zone):dd/MM/yyyy HH:mm} por {copy.IssuedBy}");
            pdf.Text(Right, 36, 8, false, $"Página {page}", alignRight: true);
        }

        void Need(float height)
        {
            if (y - height >= Bottom)
                return;
            Footer();
            pdf.NewPage();
            page++;
            y = PdfDocument.Height - 56;
        }

        void Paragraph(string text, float size = 10, bool bold = false, float indent = 0)
        {
            foreach (var line in PdfDocument.Wrap(text, size, bold, Right - Left - indent))
            {
                Need(size + 4);
                pdf.Text(Left + indent, y, size, bold, line);
                y -= size + 4;
            }
        }

        pdf.Text(Left, y, 16, true, copy.Clinic);
        pdf.Text(Right, y, 14, true, "HISTORIA CLÍNICA", alignRight: true);
        y -= 18;
        pdf.Text(Right, y, 10, true, $"N.º {copy.Patient.HistoryNumber}", alignRight: true);
        pdf.Text(Left, y, 10, false, copy.Scope);
        y -= 12;
        pdf.Line(Left, y, Right, y);
        y -= 20;
        pdf.Text(Left, y, 11, true, copy.Patient.Name);
        y -= 14;
        pdf.Text(Left, y, 10, false, $"DNI {copy.Patient.DocumentNumber} · Nacimiento {copy.Patient.BirthDate:dd/MM/yyyy} · {copy.Patient.HealthInsurance ?? "Particular"}{(copy.Patient.MemberNumber is string member ? $" · afiliado {member}" : "")}");
        y -= 18;
        Paragraph(copy.BrokenFolio is int broken
            ? $"ATENCIÓN: el folio {broken} no coincide con su sello. La historia pudo haber sido alterada fuera del sistema."
            : $"Integridad verificada: los {copy.Entries.Count} folios coinciden con sus sellos.", 9, copy.BrokenFolio is not null);
        y -= 6;

        foreach (var entry in copy.Entries)
        {
            Need(60);
            pdf.Line(Left, y + 4, Right, y + 4);
            y -= 12;
            pdf.Text(Left, y, 10, true, $"Folio {entry.Folio} · {Title(entry)}");
            pdf.Text(Right, y, 10, false, $"{Local(entry.RecordedAt, zone):dd/MM/yyyy HH:mm}", alignRight: true);
            y -= 14;
            Paragraph(entry.Signed
                ? $"Firmó {entry.AuthorName}{(entry.AuthorLicense is string license ? $", matrícula {license}" : "")} · {entry.SpecialtyName}"
                : $"Cargado por {(entry.AuthorName.Length > 0 ? entry.AuthorName : "el consultorio")} antes de la firma electrónica: sin firma · {entry.SpecialtyName}", 9);
            if (entry.AmendsFolio is int amends)
                Paragraph($"Corrige el folio {amends}.", 9, true);
            y -= 2;
            Paragraph(entry.Text);
            if (entry.Plan is string plan)
                Paragraph($"Indicaciones: {plan}");
            var vitals = Vitals(entry);
            if (vitals.Length > 0)
                Paragraph(vitals, 9);
            if (entry.Diagnoses is string diagnoses)
                Paragraph($"Diagnósticos (CIE-10): {diagnoses}", 9);
            if (entry.HasAttachment)
                Paragraph("Consentimiento firmado por el paciente adjunto en el sistema.", 9);
            Paragraph($"Sello {entry.Hash}", 7);
            y -= 6;
        }

        if (copy.Entries.Count == 0)
            Paragraph("La historia todavía no tiene folios.");
        Footer();
        return pdf.Build();
    }

    public static string Title(ClinicalEntryDto entry) => entry.Kind switch
    {
        ClinicalEntryKind.Visit => "Consulta",
        ClinicalEntryKind.Amendment => "Corrección",
        ClinicalEntryKind.Background => $"Antecedente: {BackgroundLabel(entry.BackgroundKind)}",
        ClinicalEntryKind.Consent => entry.ConsentDecision == ConsentDecision.Revoked ? "Revocación de consentimiento" : "Consentimiento informado",
        _ => ""
    };

    public static string BackgroundLabel(BackgroundKind? kind) => kind switch
    {
        BackgroundKind.BloodType => "grupo sanguíneo",
        BackgroundKind.Allergies => "alergias",
        BackgroundKind.Medication => "medicación habitual",
        BackgroundKind.PersonalHistory => "antecedentes personales",
        BackgroundKind.FamilyHistory => "antecedentes familiares",
        BackgroundKind.Habits => "hábitos",
        BackgroundKind.Notes => "observaciones",
        _ => ""
    };

    private static string Vitals(ClinicalEntryDto entry)
    {
        var parts = new List<string>();
        if (entry.BloodPressure is string pressure)
            parts.Add($"TA {pressure}");
        if (entry.HeartRate is int rate)
            parts.Add($"FC {rate}");
        if (entry.Temperature is decimal temperature)
            parts.Add($"T {temperature.ToString("0.#", Spanish)} °C");
        if (entry.WeightKg is decimal weight)
            parts.Add($"Peso {weight.ToString("0.##", Spanish)} kg");
        return string.Join(" · ", parts);
    }

    private static DateTime Local(DateTimeOffset moment, TimeZoneInfo zone) => TimeZoneInfo.ConvertTime(moment, zone).DateTime;
}
