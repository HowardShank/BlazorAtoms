using System.Collections.Generic;
using System.Text;

namespace BlazorAtoms.Barcodes.Encoders;

/// <summary>
/// QR Code generator (byte/8-bit mode, versions 1–40, all four EC levels). Implements the
/// ISO/IEC 18004 pipeline: bitstream → Reed–Solomon error correction over GF(256) → module
/// matrix with finder/alignment/timing patterns → data placement → best-of-8 data masking.
/// Returns a <c>[row, col]</c> module grid (true = dark). Pure C#, no dependencies.
/// </summary>
internal sealed class QrEncoder
{
    // Error-correction codewords per block, indexed [ecLevel][version] (version 0 unused).
    private static readonly int[][] EccPerBlock =
    {
        new[]{-1,7,10,15,20,26,18,20,24,30,18,20,24,26,30,22,24,28,30,28,28,28,28,30,30,26,28,30,30,30,30,30,30,30,30,30,30,30,30,30,30}, // L
        new[]{-1,10,16,26,18,24,16,18,22,22,26,30,22,22,24,24,28,28,26,26,26,26,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28}, // M
        new[]{-1,13,22,18,26,18,24,18,22,20,24,28,26,24,20,30,24,28,28,26,30,28,30,30,30,30,28,30,30,30,30,30,30,30,30,30,30,30,30,30,30}, // Q
        new[]{-1,17,28,22,16,22,28,26,26,24,28,24,28,22,24,24,30,28,28,26,28,30,24,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30}, // H
    };

    // Number of error-correction blocks, indexed [ecLevel][version].
    private static readonly int[][] NumBlocks =
    {
        new[]{-1,1,1,1,1,1,2,2,2,2,4,4,4,4,4,6,6,6,6,7,8,8,9,9,10,12,12,12,13,14,15,16,17,18,19,19,20,21,22,24,25}, // L
        new[]{-1,1,1,1,2,2,4,4,4,5,5,5,8,9,9,10,10,11,13,14,16,17,17,18,20,21,23,25,26,28,29,31,33,35,37,38,40,43,45,47,49}, // M
        new[]{-1,1,1,2,2,4,4,6,6,8,8,8,10,12,16,12,17,16,18,21,20,23,23,25,27,29,34,34,35,38,40,43,45,48,51,53,56,59,62,65,68}, // Q
        new[]{-1,1,1,2,4,4,4,5,6,8,8,11,11,16,16,18,16,19,21,25,25,25,34,30,32,35,37,40,42,45,48,51,54,57,60,63,66,70,74,77,81}, // H
    };

    private static readonly int[] FormatEccBits = { 1, 0, 3, 2 }; // L, M, Q, H

    private readonly int _version;
    private readonly int _ecl;
    private readonly int _size;
    private readonly bool[,] _modules;    // [row, col]
    private readonly bool[,] _isFunction; // [row, col]

    private QrEncoder(int version, int ecl)
    {
        _version = version;
        _ecl = ecl;
        _size = version * 4 + 17;
        _modules = new bool[_size, _size];
        _isFunction = new bool[_size, _size];
    }

    /// <summary>Encodes <paramref name="text"/> (UTF-8, byte mode) at the given EC level.</summary>
    public static bool[,] Encode(string text, QrErrorCorrection ecc)
    {
        var data = Encoding.UTF8.GetBytes(text ?? string.Empty);
        var ecl = (int)ecc;

        var version = -1;
        for (var v = 1; v <= 40; v++)
        {
            var capacity = NumDataCodewords(v, ecl) * 8;
            var ccBits = v < 10 ? 8 : 16;
            if (4 + ccBits + 8 * data.Length <= capacity) { version = v; break; }
        }
        if (version < 0)
            throw new FormatException($"Data too long for a QR code ({data.Length} bytes at EC level {ecc}).");

        var ccLen = version < 10 ? 8 : 16;
        var bits = new List<bool>((data.Length + 3) * 8);
        AppendBits(bits, 0b0100, 4);          // byte-mode indicator
        AppendBits(bits, data.Length, ccLen); // character count
        foreach (var b in data) AppendBits(bits, b, 8);

        var dataCw = NumDataCodewords(version, ecl);
        var capacityBits = dataCw * 8;
        AppendBits(bits, 0, Math.Min(4, capacityBits - bits.Count)); // terminator
        while (bits.Count % 8 != 0) bits.Add(false);                 // byte align
        for (var pad = 0xEC; bits.Count < capacityBits; pad ^= 0xEC ^ 0x11) AppendBits(bits, pad, 8);

        var dataCodewords = new byte[dataCw];
        for (var i = 0; i < bits.Count; i++)
            if (bits[i]) dataCodewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));

        var qr = new QrEncoder(version, ecl);
        qr.DrawFunctionPatterns();
        qr.DrawCodewords(qr.AddEccAndInterleave(dataCodewords));
        qr.SelectAndApplyMask();
        return qr._modules;
    }

    // ---- capacity math -------------------------------------------------------------------

    private static int NumRawDataModules(int v)
    {
        var result = (16 * v + 128) * v + 64;
        if (v >= 2)
        {
            var n = v / 7 + 2;
            result -= (25 * n - 10) * n - 55;
            if (v >= 7) result -= 36;
        }
        return result;
    }

    private static int NumDataCodewords(int v, int ecl) =>
        NumRawDataModules(v) / 8 - EccPerBlock[ecl][v] * NumBlocks[ecl][v];

    private int[] AlignmentPatternPositions()
    {
        if (_version == 1) return Array.Empty<int>();
        var n = _version / 7 + 2;
        var step = _version == 32 ? 26 : (_version * 4 + n * 2 + 1) / (n * 2 - 2) * 2;
        var result = new int[n];
        result[0] = 6;
        for (int i = n - 1, pos = _size - 7; i >= 1; i--, pos -= step) result[i] = pos;
        return result;
    }

    // ---- function patterns ---------------------------------------------------------------

    private void Set(int x, int y, bool dark) { _modules[y, x] = dark; _isFunction[y, x] = true; }

    private void DrawFunctionPatterns()
    {
        for (var i = 0; i < _size; i++) { Set(6, i, i % 2 == 0); Set(i, 6, i % 2 == 0); } // timing
        DrawFinder(3, 3);
        DrawFinder(_size - 4, 3);
        DrawFinder(3, _size - 4);

        var pos = AlignmentPatternPositions();
        var n = pos.Length;
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
            {
                if ((i == 0 && j == 0) || (i == 0 && j == n - 1) || (i == n - 1 && j == 0)) continue;
                DrawAlignment(pos[i], pos[j]);
            }

        DrawFormatBits(0);
        DrawVersion();
    }

    private void DrawFinder(int cx, int cy)
    {
        for (var dy = -4; dy <= 4; dy++)
            for (var dx = -4; dx <= 4; dx++)
            {
                var dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                int x = cx + dx, y = cy + dy;
                if (x >= 0 && x < _size && y >= 0 && y < _size) Set(x, y, dist != 2 && dist != 4);
            }
    }

    private void DrawAlignment(int cx, int cy)
    {
        for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
                Set(cx + dx, cy + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
    }

    private void DrawFormatBits(int mask)
    {
        var data = FormatEccBits[_ecl] << 3 | mask;
        var rem = data;
        for (var i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        var bits = ((data << 10) | rem) ^ 0x5412;

        for (var i = 0; i <= 5; i++) Set(8, i, GetBit(bits, i));
        Set(8, 7, GetBit(bits, 6));
        Set(8, 8, GetBit(bits, 7));
        Set(7, 8, GetBit(bits, 8));
        for (var i = 9; i < 15; i++) Set(14 - i, 8, GetBit(bits, i));

        for (var i = 0; i < 8; i++) Set(_size - 1 - i, 8, GetBit(bits, i));
        for (var i = 8; i < 15; i++) Set(8, _size - 15 + i, GetBit(bits, i));
        Set(8, _size - 8, true); // always-dark module
    }

    private void DrawVersion()
    {
        if (_version < 7) return;
        var rem = _version;
        for (var i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        var bits = (_version << 12) | rem;
        for (var i = 0; i < 18; i++)
        {
            var bit = GetBit(bits, i);
            int a = _size - 11 + i % 3, b = i / 3;
            Set(a, b, bit);
            Set(b, a, bit);
        }
    }

    // ---- Reed–Solomon --------------------------------------------------------------------

    private byte[] AddEccAndInterleave(byte[] data)
    {
        var numBlocks = NumBlocks[_ecl][_version];
        var blockEccLen = EccPerBlock[_ecl][_version];
        var rawCodewords = NumRawDataModules(_version) / 8;
        var numShort = numBlocks - rawCodewords % numBlocks;
        var shortLen = rawCodewords / numBlocks;

        var blocks = new byte[numBlocks][];
        var rsDiv = RsComputeDivisor(blockEccLen);
        var k = 0;
        for (var i = 0; i < numBlocks; i++)
        {
            var datLen = shortLen - blockEccLen + (i < numShort ? 0 : 1);
            var dat = new byte[datLen];
            Array.Copy(data, k, dat, 0, datLen);
            k += datLen;
            var block = new byte[shortLen + 1];
            Array.Copy(dat, 0, block, 0, datLen);
            var ecc = RsComputeRemainder(dat, rsDiv);
            Array.Copy(ecc, 0, block, block.Length - blockEccLen, blockEccLen);
            blocks[i] = block;
        }

        var result = new byte[rawCodewords];
        var idx = 0;
        for (var i = 0; i < blocks[0].Length; i++)
            for (var j = 0; j < numBlocks; j++)
                if (i != shortLen - blockEccLen || j >= numShort)
                    result[idx++] = blocks[j][i];
        return result;
    }

    private static byte[] RsComputeDivisor(int degree)
    {
        var result = new byte[degree];
        result[degree - 1] = 1;
        var root = 1;
        for (var i = 0; i < degree; i++)
        {
            for (var j = 0; j < degree; j++)
            {
                result[j] = (byte)RsMul(result[j] & 0xFF, root);
                if (j + 1 < degree) result[j] ^= result[j + 1];
            }
            root = RsMul(root, 0x02);
        }
        return result;
    }

    private static byte[] RsComputeRemainder(byte[] data, byte[] divisor)
    {
        var result = new byte[divisor.Length];
        foreach (var b in data)
        {
            var factor = (b ^ result[0]) & 0xFF;
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[result.Length - 1] = 0;
            for (var i = 0; i < result.Length; i++)
                result[i] ^= (byte)RsMul(divisor[i] & 0xFF, factor);
        }
        return result;
    }

    private static int RsMul(int x, int y)
    {
        var z = 0;
        for (var i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return z & 0xFF;
    }

    // ---- data placement + masking --------------------------------------------------------

    private void DrawCodewords(byte[] data)
    {
        var i = 0;
        for (var right = _size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            for (var vert = 0; vert < _size; vert++)
                for (var j = 0; j < 2; j++)
                {
                    var x = right - j;
                    var upward = ((right + 1) & 2) == 0;
                    var y = upward ? _size - 1 - vert : vert;
                    if (!_isFunction[y, x] && i < data.Length * 8)
                    {
                        _modules[y, x] = GetBit(data[i >> 3], 7 - (i & 7));
                        i++;
                    }
                }
        }
    }

    private void SelectAndApplyMask()
    {
        var minPenalty = int.MaxValue;
        var best = 0;
        for (var m = 0; m < 8; m++)
        {
            ApplyMask(m);
            DrawFormatBits(m);
            var p = PenaltyScore();
            if (p < minPenalty) { minPenalty = p; best = m; }
            ApplyMask(m); // undo
        }
        ApplyMask(best);
        DrawFormatBits(best);
    }

    private void ApplyMask(int mask)
    {
        for (var y = 0; y < _size; y++)
            for (var x = 0; x < _size; x++)
            {
                if (_isFunction[y, x]) continue;
                var invert = mask switch
                {
                    0 => (x + y) % 2 == 0,
                    1 => y % 2 == 0,
                    2 => x % 3 == 0,
                    3 => (x + y) % 3 == 0,
                    4 => (x / 3 + y / 2) % 2 == 0,
                    5 => x * y % 2 + x * y % 3 == 0,
                    6 => (x * y % 2 + x * y % 3) % 2 == 0,
                    7 => ((x + y) % 2 + x * y % 3) % 2 == 0,
                    _ => false,
                };
                if (invert) _modules[y, x] = !_modules[y, x];
            }
    }

    private int PenaltyScore()
    {
        var result = 0;

        for (var y = 0; y < _size; y++)
        {
            var runColor = false; var run = 0; var hist = new int[7];
            for (var x = 0; x < _size; x++)
            {
                if (_modules[y, x] == runColor) { run++; if (run == 5) result += 3; else if (run > 5) result++; }
                else { FinderAddHistory(run, hist); if (!runColor) result += FinderCount(hist) * 40; runColor = _modules[y, x]; run = 1; }
            }
            result += FinderTerminate(runColor, run, hist) * 40;
        }

        for (var x = 0; x < _size; x++)
        {
            var runColor = false; var run = 0; var hist = new int[7];
            for (var y = 0; y < _size; y++)
            {
                if (_modules[y, x] == runColor) { run++; if (run == 5) result += 3; else if (run > 5) result++; }
                else { FinderAddHistory(run, hist); if (!runColor) result += FinderCount(hist) * 40; runColor = _modules[y, x]; run = 1; }
            }
            result += FinderTerminate(runColor, run, hist) * 40;
        }

        for (var y = 0; y < _size - 1; y++)
            for (var x = 0; x < _size - 1; x++)
            {
                var c = _modules[y, x];
                if (c == _modules[y, x + 1] && c == _modules[y + 1, x] && c == _modules[y + 1, x + 1]) result += 3;
            }

        var dark = 0;
        for (var y = 0; y < _size; y++)
            for (var x = 0; x < _size; x++)
                if (_modules[y, x]) dark++;
        var total = _size * _size;
        var k = (int)((Math.Abs(dark * 20L - total * 10L) + total - 1) / total) - 1;
        result += k * 10;
        return result;
    }

    private void FinderAddHistory(int run, int[] hist)
    {
        if (hist[0] == 0) run += _size; // light border before the first run
        Array.Copy(hist, 0, hist, 1, hist.Length - 1);
        hist[0] = run;
    }

    private static int FinderCount(int[] hist)
    {
        var n = hist[1];
        var core = n > 0 && hist[2] == n && hist[3] == n * 3 && hist[4] == n && hist[5] == n;
        var result = 0;
        if (core && hist[0] >= n * 4 && hist[6] >= n) result++;
        if (core && hist[6] >= n * 4 && hist[0] >= n) result++;
        return result;
    }

    private int FinderTerminate(bool runColor, int run, int[] hist)
    {
        if (runColor) { FinderAddHistory(run, hist); run = 0; }
        run += _size;
        FinderAddHistory(run, hist);
        return FinderCount(hist);
    }

    // ---- bit helpers ---------------------------------------------------------------------

    private static void AppendBits(List<bool> bits, int value, int length)
    {
        for (var i = length - 1; i >= 0; i--) bits.Add(((value >> i) & 1) != 0);
    }

    private static bool GetBit(int x, int i) => ((x >> i) & 1) != 0;
}
