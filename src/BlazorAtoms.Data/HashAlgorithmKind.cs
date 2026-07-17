namespace BlazorAtoms.Data;

/// <summary>
/// Which hash / checksum engine <see cref="AtomDataHasher"/> uses. The CRC entries are computed by
/// the library's own table-based implementations (so no <c>System.IO.Hashing</c> dependency);
/// the cryptographic entries wrap <see cref="System.Security.Cryptography"/>.
/// </summary>
public enum HashAlgorithmKind
{
    /// <summary>CRC-32 / IEEE 802.3 (reflected polynomial 0xEDB88320). Non-cryptographic.</summary>
    Crc32,
    /// <summary>CRC-64 / ECMA-182 (reflected polynomial 0xC96C5795D7870F42). Non-cryptographic.</summary>
    Crc64,
    /// <summary>MD5 — 128-bit cryptographic (broken; kept for checksums / interop only).</summary>
    Md5,
    /// <summary>SHA-256 — 256-bit cryptographic.</summary>
    Sha256,
    /// <summary>SHA-512 — 512-bit cryptographic.</summary>
    Sha512,
}
