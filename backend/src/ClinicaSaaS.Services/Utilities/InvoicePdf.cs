using System.Globalization;
using System.Text;
using ClinicaSaaS.Domain;

namespace ClinicaSaaS.Services.Utilities;

// Writes an invoice as a one-page A4 PDF.
public static class InvoicePdf
{
    private const float Width = PdfDocument.Width;
    private const float Height = PdfDocument.Height;
    private const float Left = 48;
    private const float Right = Width - 48;
    private static readonly CultureInfo Money = CultureInfo.GetCultureInfo("es-AR");

    public static byte[] Render(InvoiceDto invoice, ClinicSettings clinic, TimeZoneInfo zone)
    {
        var page = new PdfDocument();
        var y = Height - 56;

        page.Text(Left, y, 16, true, clinic.Name);
        page.Text(Right, y, 16, true, Title(invoice), alignRight: true);
        y -= 18;
        if (invoice.Number is int number)
        {
            page.Text(Left, y, 9, false, $"CUIT {FormatTaxId(clinic.TaxId)} · {TaxConditionLabel(clinic.TaxCondition)}");
            page.Text(Right, y, 10, true, $"N° {invoice.PointOfSale:0000}-{number:00000000}", alignRight: true);
        }
        else
        {
            page.Text(Right, y, 9, false, "Documento no válido como factura", alignRight: true);
        }
        y -= 14;
        // A fiscal voucher is dated the day the tax authority authorized it.
        page.Text(Right, y, 9, false, $"Fecha {Local(invoice.AuthorizedAt ?? invoice.CreatedAt, zone):dd/MM/yyyy}", alignRight: true);
        y -= 14;
        page.Line(Left, y, Right, y);

        y -= 22;
        page.Text(Left, y, 10, true, "Paciente");
        page.Text(300, y, 10, true, "Atención");
        y -= 14;
        page.Text(Left, y, 10, false, invoice.Patient);
        page.Text(300, y, 10, false, $"{Local(invoice.AppointmentStart, zone):dd/MM/yyyy HH:mm} · {invoice.Professional}");
        y -= 14;
        page.Text(Left, y, 10, false, $"DNI {invoice.PatientDocument} · Consumidor final");
        if (invoice.Insurer is string insurer)
            page.Text(300, y, 10, false, $"Obra social: {insurer}");

        y -= 28;
        page.Text(Left, y, 9, true, "Descripción");
        page.Text(360, y, 9, true, "Cant.", alignRight: true);
        page.Text(450, y, 9, true, "Precio unit.", alignRight: true);
        page.Text(Right, y, 9, true, "Importe", alignRight: true);
        y -= 6;
        page.Line(Left, y, Right, y);
        foreach (var item in invoice.Items)
        {
            y -= 16;
            page.Text(Left, y, 10, false, Clip(item.Description, 52));
            page.Text(360, y, 10, false, item.Quantity.ToString(Money), alignRight: true);
            page.Text(450, y, 10, false, Format(item.UnitPrice), alignRight: true);
            page.Text(Right, y, 10, false, Format(item.Amount), alignRight: true);
        }
        y -= 8;
        page.Line(Left, y, Right, y);

        void Row(string label, decimal amount, bool bold = false)
        {
            y -= 16;
            page.Text(450, y, 10, bold, label, alignRight: true);
            page.Text(Right, y, 10, bold, Format(amount), alignRight: true);
        }

        Row("Subtotal", invoice.Subtotal);
        if (invoice.Discount > 0)
            Row("Descuento", -invoice.Discount);
        Row("Total", invoice.Total, bold: true);
        if (invoice.InsurerAmount > 0)
        {
            Row("Cubre la obra social", invoice.InsurerAmount);
            Row("A cargo del paciente", invoice.PatientAmount, bold: true);
        }
        Row("Pagado", invoice.Paid);
        if (invoice.Status != InvoiceStatus.Voided)
            Row("Saldo", invoice.Balance, bold: true);

        if (invoice.Payments.Count > 0)
        {
            y -= 26;
            page.Text(Left, y, 10, true, "Pagos");
            foreach (var payment in invoice.Payments)
            {
                y -= 14;
                page.Text(Left, y, 10, false, $"{Local(payment.PaidAt, zone):dd/MM/yyyy HH:mm} · {MethodLabel(payment.Method)}");
                page.Text(Right, y, 10, false, Format(payment.Amount), alignRight: true);
            }
        }

        if (invoice.Status == InvoiceStatus.Voided)
        {
            y -= 30;
            page.Text(Left, y, 12, true, "ANULADO");
            y -= 14;
            page.Text(Left, y, 10, false, Clip($"Motivo: {invoice.VoidReason}", 90));
            if (invoice.CreditNoteNumber is int note)
            {
                y -= 14;
                page.Text(Left, y, 10, false, $"{VoucherLabel(invoice.CreditNoteType)} N° {invoice.PointOfSale:0000}-{note:00000000} · CAE {invoice.CreditNoteCae}");
            }
        }

        if (invoice.Cae is string cae)
        {
            page.Line(Left, 92, Right, 92);
            page.Text(Left, 74, 10, true, $"CAE {cae}");
            page.Text(Right, 74, 10, false, $"Vencimiento del CAE {invoice.CaeDueDate:dd/MM/yyyy}", alignRight: true);
            if (invoice.FiscalSimulated)
                page.Text(Left, 58, 9, false, "CAE de prueba emitido por el simulador: no tiene validez ante ARCA.");
        }

        return page.Build();
    }

    private static string Title(InvoiceDto invoice) =>
        invoice.VoucherType is FiscalVoucherType type ? VoucherLabel(type).ToUpperInvariant() : "COMPROBANTE";

    private static string VoucherLabel(FiscalVoucherType? type) => type switch
    {
        FiscalVoucherType.FacturaB => "Factura B",
        FiscalVoucherType.FacturaC => "Factura C",
        FiscalVoucherType.NotaCreditoB => "Nota de crédito B",
        FiscalVoucherType.NotaCreditoC => "Nota de crédito C",
        _ => "Comprobante"
    };

    private static string TaxConditionLabel(TaxCondition? condition) => condition switch
    {
        TaxCondition.ResponsableInscripto => "IVA Responsable Inscripto",
        TaxCondition.Monotributista => "Responsable Monotributo",
        TaxCondition.Exento => "IVA Exento",
        _ => ""
    };

    private static string MethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Efectivo",
        PaymentMethod.Transfer => "Transferencia",
        PaymentMethod.Card => "Tarjeta",
        _ => ""
    };

    private static string FormatTaxId(string? taxId)
    {
        var digits = new string((taxId ?? "").Where(char.IsDigit).ToArray());
        return digits.Length == 11 ? $"{digits[..2]}-{digits[2..10]}-{digits[10]}" : digits;
    }

    private static string Format(decimal amount) => (amount < 0 ? "- $ " : "$ ") + Math.Abs(amount).ToString("N2", Money);

    private static DateTime Local(DateTimeOffset moment, TimeZoneInfo zone) => TimeZoneInfo.ConvertTime(moment, zone).DateTime;

    private static string Clip(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
