using System.Globalization;
using System.Text;

namespace ClinicaSaaS.Services.Utilities;

// A PDF written by hand on A4 pages with the built-in Helvetica fonts. WinAnsi encoding covers Spanish text,
// so no PDF library is needed.
public sealed class PdfDocument
{
    public const float Width = 595;
    public const float Height = 842;

    private readonly List<StringBuilder> pages = [new()];

    private StringBuilder Current => pages[^1];

    public int PageCount => pages.Count;

    public void NewPage() => pages.Add(new StringBuilder());

    public void Text(float x, float y, float size, bool bold, string text, bool alignRight = false)
    {
        if (alignRight)
            x -= Measure(text, size, bold);
        Current.Append(CultureInfo.InvariantCulture, $"BT /{(bold ? "F2" : "F1")} {size} Tf {x:0.##} {y:0.##} Td (")
            .Append(Escape(text))
            .Append(") Tj ET\n");
    }

    public void Line(float x1, float y1, float x2, float y2) =>
        Current.Append(CultureInfo.InvariantCulture, $"0.6 w {x1:0.##} {y1:0.##} m {x2:0.##} {y2:0.##} l S\n");

    // Splits text into lines that fit `width` points, keeping the paragraphs it already had.
    public static IEnumerable<string> Wrap(string text, float size, bool bold, float width)
    {
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = new StringBuilder();
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : $"{line} {word}";
                if (line.Length > 0 && Measure(candidate, size, bold) > width)
                {
                    yield return line.ToString();
                    line.Clear().Append(word);
                }
                else
                {
                    line.Clear().Append(candidate);
                }
            }
            yield return line.ToString();
        }
    }

    public byte[] Build()
    {
        List<byte[]> objects =
        [
            Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
            [],
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")
        ];
        var kids = new List<string>();
        foreach (var content in pages)
        {
            var stream = Encoding.Latin1.GetBytes(content.ToString());
            byte[] body = [.. Ascii($"<< /Length {stream.Length} >>\nstream\n"), .. stream, .. Ascii("\nendstream")];
            objects.Add(body);
            var contentId = objects.Count;
            objects.Add(Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Width} {Height}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>"));
            kids.Add($"{objects.Count} 0 R");
        }
        objects[1] = Ascii($"<< /Type /Pages /Kids [{string.Join(' ', kids)}] /Count {pages.Count} >>");

        using var output = new MemoryStream();
        output.Write(Ascii("%PDF-1.4\n%âãÏÓ\n"));
        var offsets = new List<long>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            output.Write(Ascii($"{i + 1} 0 obj\n"));
            output.Write(objects[i]);
            output.Write(Ascii("\nendobj\n"));
        }
        var xref = output.Position;
        var table = new StringBuilder($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            table.Append(CultureInfo.InvariantCulture, $"{offset:0000000000} 00000 n \n");
        table.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        output.Write(Ascii(table.ToString()));
        return output.ToArray();
    }

    // Average Helvetica widths: close enough to right-align figures and to wrap text.
    public static float Measure(string text, float size, bool bold)
    {
        float units = 0;
        foreach (var c in text)
            units += c switch
            {
                ' ' or '.' or ',' or ':' or 'i' or 'l' or 'I' or 'j' or 't' or 'f' or 'r' => 280,
                >= '0' and <= '9' or '$' => 556,
                'm' or 'w' or 'M' or 'W' => 833,
                >= 'A' and <= 'Z' => 667,
                _ => 520
            };
        return units * size / 1000f * (bold ? 1.05f : 1f);
    }

    private static byte[] Ascii(string text) => Encoding.Latin1.GetBytes(text);

    private static string Escape(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is '(' or ')' or '\\')
                builder.Append('\\').Append(c);
            else if (c == '…')
                builder.Append('\u0085');
            else if (c > 255 || char.IsControl(c))
                builder.Append('?');
            else
                builder.Append(c);
        }
        return builder.ToString();
    }
}
