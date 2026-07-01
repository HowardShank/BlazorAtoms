namespace BlazorAtoms.Barcodes.Encoders;

/// <summary>
/// Code 39 encoder. Produces a module pattern (one bool per narrow unit; true = bar).
/// Uses the standard element table (narrow = 1 unit, wide = 2 units), matching the widely
/// implemented convention, framed by the <c>*</c> start/stop character.
/// </summary>
internal static class Code39Encoder
{
    // Data alphabet (index → character). '*' is the start/stop guard and is not valid data.
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    // 9-element wide/narrow pattern per character (bit (8-i) set ⇒ element i is wide).
    // Element order alternates bar, space, bar, … starting with a bar (even indices = bars).
    private static readonly int[] Encodings =
    {
        0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064, // 0-9
        0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C, // A-J
        0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016, // K-T
        0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0,                             // U-Z
        0x085, 0x184, 0x0C4, 0x0A8, 0x0A2, 0x08A, 0x02A,                      // - . space $ / + %
    };

    private const int StarEncoding = 0x094; // '*' start/stop

    /// <summary>Encodes <paramref name="value"/> (letters are upper-cased) into a bar/space module pattern.</summary>
    public static bool[] Encode(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var upper = value.ToUpperInvariant();

        var modules = new List<bool>(upper.Length * 16 + 32);

        AppendCharacter(modules, StarEncoding);
        foreach (var ch in upper)
        {
            var index = Alphabet.IndexOf(ch);
            if (index < 0)
                throw new FormatException($"Code 39 cannot encode '{ch}'. Valid: 0-9 A-Z and - . space $ / + %.");
            modules.Add(false); // narrow inter-character gap
            AppendCharacter(modules, Encodings[index]);
        }
        modules.Add(false);
        AppendCharacter(modules, StarEncoding);

        return modules.ToArray();
    }

    private static void AppendCharacter(List<bool> modules, int pattern)
    {
        for (var i = 0; i < 9; i++)
        {
            var wide = (pattern & (1 << (8 - i))) != 0;
            var width = wide ? 2 : 1;
            var isBar = (i & 1) == 0; // even element index = bar
            for (var w = 0; w < width; w++) modules.Add(isBar);
        }
    }
}
