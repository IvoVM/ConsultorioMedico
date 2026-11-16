namespace ClinicaSaaS.Services.Utilities;

// Reads the size of a JPEG from its frame header, which is all the PDF needs to embed it untouched.
public static class Jpeg
{
    public sealed record Info(int Width, int Height, int Components);

    public static Info? Read(byte[] data)
    {
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
            return null;
        var i = 2;
        while (i + 4 <= data.Length)
        {
            if (data[i] != 0xFF)
                return null;
            var marker = data[i + 1];
            if (marker == 0xFF)
            {
                i++;
                continue;
            }
            if (marker is 0xD8 or 0x01 or >= 0xD0 and <= 0xD7)
            {
                i += 2;
                continue;
            }
            var length = (data[i + 2] << 8) | data[i + 3];
            if (length < 2 || i + 2 + length > data.Length)
                return null;
            // Start-of-frame markers, except DHT (C4), JPG (C8) and DAC (CC).
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                if (length < 8)
                    return null;
                var height = (data[i + 5] << 8) | data[i + 6];
                var width = (data[i + 7] << 8) | data[i + 8];
                var components = data[i + 9];
                return width > 0 && height > 0 ? new Info(width, height, components) : null;
            }
            if (marker == 0xDA)
                return null;
            i += 2 + length;
        }
        return null;
    }
}
