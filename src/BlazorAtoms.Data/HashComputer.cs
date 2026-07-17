using System.Security.Cryptography;
using System.Text;

namespace BlazorAtoms.Data;

/// <summary>
/// Pure-static hash / checksum computation. Split out from <see cref="AtomDataHasher"/> so the
/// engines can be unit-tested without spinning up bUnit, and so a consumer can call
/// <see cref="Compute(HashAlgorithmKind, string, Encoding?)"/> directly without instantiating a
/// component.
/// </summary>
public static class HashComputer
{
    /// <summary>
    /// Compute the hex digest of <paramref name="input"/> using <paramref name="algorithm"/>.
    /// Empty / null input returns an empty string (matches the reference component's UX; keeps
    /// the "no result until user types something" contract).
    /// </summary>
    public static string Compute(HashAlgorithmKind algorithm, string? input, Encoding? encoding = null)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var bytes = (encoding ?? Encoding.UTF8).GetBytes(input);
        return Compute(algorithm, bytes);
    }

    /// <summary>Byte-array overload. Empty array yields the algorithm's empty-input digest.</summary>
    public static string Compute(HashAlgorithmKind algorithm, byte[] data) => algorithm switch
    {
        // CRC values render as fixed-width uppercase hex — byte-order-independent, unlike
        // BitConverter.GetBytes(...) which is little-endian on x86 and would print backwards.
        HashAlgorithmKind.Crc32 => Crc32(data).ToString("X8"),
        HashAlgorithmKind.Crc64 => Crc64(data).ToString("X16"),
        // MD5 is fully implemented in the .NET WASM crypto runtime since .NET 8; the CA1416
        // annotation on the reference assembly is stale, so the warning is suppressed here.
#pragma warning disable CA1416
        HashAlgorithmKind.Md5 => ComputeCryptographic(MD5.Create(), data),
#pragma warning restore CA1416
        HashAlgorithmKind.Sha256 => ComputeCryptographic(SHA256.Create(), data),
        HashAlgorithmKind.Sha512 => ComputeCryptographic(SHA512.Create(), data),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    private static string ComputeCryptographic(HashAlgorithm alg, byte[] data)
    {
        using (alg) return ToHexUpper(alg.ComputeHash(data));
    }

    private static string ToHexUpper(byte[] bytes)
    {
        // Convert.ToHexString is framework-provided (net5+), uppercase, no allocations
        // beyond the string. Matches BitConverter.ToString(x).Replace("-","") from the sample.
        return Convert.ToHexString(bytes);
    }

    // ---- CRC-32 (IEEE 802.3, reflected poly 0xEDB88320) -------------------------------------
    //
    // Table-driven, byte-at-a-time. Matches System.IO.Hashing.Crc32.Hash(...) output exactly (see
    // tests). Kept in-library so we don't need the out-of-band System.IO.Hashing package — every
    // BlazorAtoms library stays framework-only.

    private static readonly uint[] Crc32Table = BuildCrc32Table();

    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var j = 0; j < 8; j++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }

    /// <summary>Compute CRC-32 (IEEE 802.3, reflected). Exposed for tests + direct callers.</summary>
    public static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        for (var i = 0; i < data.Length; i++)
            crc = Crc32Table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
        return ~crc;
    }

    // ---- CRC-64 (ECMA-182, non-reflected poly 0x42F0E1EBA9EA3693) ----------------------------
    //
    // Matches System.IO.Hashing.Crc64 (init 0, no input/output reflection). Check value for
    // "123456789" is 0x6C40DF5F0B497347 per the CRC-64/ECMA-182 reference.

    private static readonly ulong[] Crc64Table = BuildCrc64Table();

    private static ulong[] BuildCrc64Table()
    {
        const ulong poly = 0x42F0E1EBA9EA3693UL;
        var table = new ulong[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = (ulong)i << 56;
            for (var j = 0; j < 8; j++)
                c = (c & 0x8000000000000000UL) != 0 ? (c << 1) ^ poly : c << 1;
            table[i] = c;
        }
        return table;
    }

    /// <summary>Compute CRC-64/ECMA-182 (non-reflected). Exposed for tests + direct callers.</summary>
    public static ulong Crc64(byte[] data)
    {
        var crc = 0UL;
        for (var i = 0; i < data.Length; i++)
            crc = Crc64Table[((crc >> 56) ^ data[i]) & 0xFF] ^ (crc << 8);
        return crc;
    }
}
