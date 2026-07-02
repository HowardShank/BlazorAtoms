namespace BlazorAtoms.Barcodes.Encoders;

/// <summary>
/// Codabar encoder. Encodes digits and <c>- $ : / . +</c>, framed by A/B/C/D start/stop guards
/// (an 'A' guard is added automatically when the value isn't already guarded). Narrow = 1 unit,
/// wide = 2. Emits a module pattern (true = bar).
/// </summary>
internal static class CodabarEncoder
{
    private const string Alphabet = "0123456789-$:/.+ABCD";

    // 7-element wide/narrow pattern per character (bit (6-i) set ⇒ element i wide; even i = bar).
    private static readonly int[] Encodings =
    {
        0x003,0x006,0x009,0x060,0x012,0x042,0x021,0x024,0x030,0x048, // 0-9
        0x00C,0x018,0x045,0x051,0x054,0x015,0x01A,0x029,0x00B,0x00E, // - $ : / . + A B C D
    };

    public static bool[] Encode(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var s = value.ToUpperInvariant();
        if (s.Length == 0) throw new FormatException("Codabar value is empty.");

        static bool IsGuard(char c) => c is >= 'A' and <= 'D';
        if (!IsGuard(s[0])) s = "A" + s;
        if (!IsGuard(s[s.Length - 1])) s += "A";

        var modules = new List<bool>(s.Length * 10);
        for (var idx = 0; idx < s.Length; idx++)
        {
            var pos = Alphabet.IndexOf(s[idx]);
            if (pos < 0)
                throw new FormatException($"Codabar cannot encode '{s[idx]}'. Valid: 0-9 - $ : / . + and A-D guards.");

            var code = Encodings[pos];
            for (var i = 0; i < 7; i++)
            {
                var wide = ((code >> (6 - i)) & 1) != 0;
                var isBar = (i & 1) == 0;
                for (var w = 0; w < (wide ? 2 : 1); w++) modules.Add(isBar);
            }
            if (idx < s.Length - 1) modules.Add(false); // narrow inter-character gap
        }
        return modules.ToArray();
    }
}
