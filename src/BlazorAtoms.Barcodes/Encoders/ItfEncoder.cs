namespace BlazorAtoms.Barcodes.Encoders;

/// <summary>
/// Interleaved 2 of 5 (ITF) encoder. Numeric only, even digit count (a leading zero is added when
/// odd). Digit pairs interleave: the first digit's five elements are the bars, the second's are the
/// spaces. Narrow = 1 unit, wide = 3 (a 3:1 ratio reads more reliably than 2:1). Emits a module
/// pattern (true = bar).
/// </summary>
internal static class ItfEncoder
{
    private const int Wide = 3;

    // Per digit: five elements, '1' = wide.
    private static readonly string[] Patterns =
    { "00110","10001","01001","11000","00101","10100","01100","00011","10010","01010" };

    public static bool[] Encode(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (!Ean13Encoder.IsAllDigits(value))
            throw new FormatException("ITF (Interleaved 2 of 5) accepts digits only.");

        var s = value.Length % 2 != 0 ? "0" + value : value;
        var modules = new List<bool>(s.Length * 4 * Wide + 16);

        // Start guard: narrow bar, space, bar, space.
        modules.Add(true); modules.Add(false); modules.Add(true); modules.Add(false);

        for (var i = 0; i < s.Length; i += 2)
        {
            var bars = Patterns[s[i] - '0'];
            var spaces = Patterns[s[i + 1] - '0'];
            for (var k = 0; k < 5; k++)
            {
                for (var w = 0; w < (bars[k] == '1' ? Wide : 1); w++) modules.Add(true);
                for (var w = 0; w < (spaces[k] == '1' ? Wide : 1); w++) modules.Add(false);
            }
        }

        // Stop guard: wide bar, narrow space, narrow bar.
        for (var w = 0; w < Wide; w++) modules.Add(true);
        modules.Add(false);
        modules.Add(true);
        return modules.ToArray();
    }
}
