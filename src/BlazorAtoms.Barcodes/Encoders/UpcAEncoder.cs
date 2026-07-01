namespace BlazorAtoms.Barcodes.Encoders;

/// <summary>
/// UPC-A encoder. Accepts 11 digits (computes the check digit) or 12 digits (validates it).
/// UPC-A is EAN-13 with a leading zero, so it delegates to <see cref="Ean13Encoder"/>.
/// </summary>
internal static class UpcAEncoder
{
    public static bool[] Encode(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (!Ean13Encoder.IsAllDigits(value))
            throw new FormatException("UPC-A accepts digits only.");

        string twelve;
        if (value.Length == 11)
        {
            twelve = value + (char)('0' + CheckDigit(value));
        }
        else if (value.Length == 12)
        {
            var expected = CheckDigit(value.Substring(0, 11));
            if (value[11] - '0' != expected)
                throw new FormatException($"UPC-A check digit is {value[11]}, expected {expected}.");
            twelve = value;
        }
        else
        {
            throw new FormatException("UPC-A requires 11 digits (check computed) or 12 digits (check validated).");
        }

        // A 0-prefixed UPC-A is a valid EAN-13 (the check digit is identical).
        return Ean13Encoder.Encode("0" + twelve);
    }

    /// <summary>UPC-A check digit for the leading 11 digits (odd positions weighted ×3).</summary>
    private static int CheckDigit(string eleven)
    {
        var sum = 0;
        for (var i = 0; i < 11; i++)
        {
            var d = eleven[i] - '0';
            sum += ((i + 1) % 2 == 1) ? d * 3 : d;
        }
        return (10 - sum % 10) % 10;
    }
}
