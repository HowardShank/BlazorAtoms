namespace BlazorAtoms.Barcodes.Encoders;

/// <summary>
/// EAN-13 encoder. Accepts 12 digits (computes the check digit) or 13 digits (validates it),
/// and emits a module pattern (true = bar) with start/center/end guards. The first digit is
/// carried by the L/G parity mix of the six left-hand digits.
/// </summary>
internal static class Ean13Encoder
{
    // 7-module patterns per digit ('1' = bar). R = inverse of L; G = reverse of R.
    private static readonly string[] L =
    { "0001101","0011001","0010011","0111101","0100011","0110001","0101111","0111011","0110111","0001011" };
    private static readonly string[] G =
    { "0100111","0110011","0011011","0100001","0011101","0111001","0000101","0010001","0001001","0010111" };
    private static readonly string[] R =
    { "1110010","1100110","1101100","1000010","1011100","1001110","1010000","1000100","1001000","1110100" };

    // Per first-digit: parity of the six left digits ('0' = L/odd, '1' = G/even).
    private static readonly string[] Parity =
    { "000000","001011","001101","001110","010011","011001","011100","010101","010110","011010" };

    public static bool[] Encode(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (!IsAllDigits(value))
            throw new FormatException("EAN-13 accepts digits only.");

        string thirteen;
        if (value.Length == 12)
        {
            thirteen = value + (char)('0' + CheckDigit(value));
        }
        else if (value.Length == 13)
        {
            var expected = CheckDigit(value.Substring(0, 12));
            if (value[12] - '0' != expected)
                throw new FormatException($"EAN-13 check digit is {value[12]}, expected {expected}.");
            thirteen = value;
        }
        else
        {
            throw new FormatException("EAN-13 requires 12 digits (check computed) or 13 digits (check validated).");
        }

        return Build(thirteen);
    }

    /// <summary>EAN-13 check digit for the leading 12 digits (even positions weighted ×3).</summary>
    internal static int CheckDigit(string twelve)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var d = twelve[i] - '0';
            sum += ((i + 1) % 2 == 0) ? d * 3 : d;
        }
        return (10 - sum % 10) % 10;
    }

    internal static bool IsAllDigits(string s)
    {
        foreach (var c in s) if (c < '0' || c > '9') return false;
        return s.Length > 0;
    }

    private static bool[] Build(string d)
    {
        var parity = Parity[d[0] - '0'];
        var bits = new System.Text.StringBuilder(95);

        bits.Append("101"); // start guard
        for (var i = 0; i < 6; i++)
        {
            var digit = d[1 + i] - '0';
            bits.Append(parity[i] == '0' ? L[digit] : G[digit]);
        }
        bits.Append("01010"); // center guard
        for (var i = 0; i < 6; i++)
            bits.Append(R[d[7 + i] - '0']);
        bits.Append("101"); // end guard

        var s = bits.ToString();
        var modules = new bool[s.Length];
        for (var i = 0; i < s.Length; i++) modules[i] = s[i] == '1';
        return modules;
    }
}
