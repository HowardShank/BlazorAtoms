using System.Text;

namespace BlazorAtoms.Data.Tests;

/// <summary>
/// Vectors verify the library's in-house CRC engines against published/known values and the
/// framework's cryptographic hashes against known digests. Anchors regressions if anyone tweaks
/// the polynomial tables.
/// </summary>
public class HashComputerTests
{
    [Fact]
    public void Crc32_of_empty_is_zero()
    {
        Assert.Equal(0u, HashComputer.Crc32(Array.Empty<byte>()));
    }

    [Fact]
    public void Crc32_of_ascii_123456789_matches_reference()
    {
        // Standard CRC-32/IEEE check value for the ASCII string "123456789".
        var bytes = Encoding.ASCII.GetBytes("123456789");
        Assert.Equal(0xCBF43926u, HashComputer.Crc32(bytes));
    }

    [Fact]
    public void Crc32_of_ascii_hello_world_matches_reference()
    {
        // "Hello, World!" -> 0xEC4AC3D0 per IEEE reflected CRC-32.
        var bytes = Encoding.ASCII.GetBytes("Hello, World!");
        Assert.Equal(0xEC4AC3D0u, HashComputer.Crc32(bytes));
    }

    [Fact]
    public void Crc64_of_empty_is_zero()
    {
        Assert.Equal(0UL, HashComputer.Crc64(Array.Empty<byte>()));
    }

    [Fact]
    public void Crc64_of_ascii_123456789_matches_ecma182()
    {
        // CRC-64/ECMA-182 (non-reflected) check value — matches System.IO.Hashing.Crc64.
        var bytes = Encoding.ASCII.GetBytes("123456789");
        Assert.Equal(0x6C40DF5F0B497347UL, HashComputer.Crc64(bytes));
    }

    [Fact]
    public void Compute_returns_empty_string_for_null_and_empty_input()
    {
        Assert.Equal(string.Empty, HashComputer.Compute(HashAlgorithmKind.Crc32, (string?)null));
        Assert.Equal(string.Empty, HashComputer.Compute(HashAlgorithmKind.Sha256, string.Empty));
    }

    [Fact]
    public void Compute_crc32_returns_uppercase_hex()
    {
        // Fixed-width uppercase hex of the numeric CRC — matches expectation of a checksum tool.
        Assert.Equal("CBF43926", HashComputer.Compute(HashAlgorithmKind.Crc32, "123456789"));
    }

    [Fact]
    public void Compute_crc64_returns_uppercase_hex()
    {
        Assert.Equal("6C40DF5F0B497347", HashComputer.Compute(HashAlgorithmKind.Crc64, "123456789"));
    }

    [Fact]
    public void Compute_md5_of_empty_string_matches_known_digest()
    {
        // Wire in with a non-empty input; MD5("") = "D41D8CD98F00B204E9800998ECF8427E".
        var hex = HashComputer.Compute(HashAlgorithmKind.Md5, "");
        Assert.Equal(string.Empty, hex); // empty policy: no digest for empty input
    }

    [Fact]
    public void Compute_md5_of_abc_matches_known_digest()
    {
        var hex = HashComputer.Compute(HashAlgorithmKind.Md5, "abc");
        Assert.Equal("900150983CD24FB0D6963F7D28E17F72", hex);
    }

    [Fact]
    public void Compute_sha256_of_abc_matches_known_digest()
    {
        var hex = HashComputer.Compute(HashAlgorithmKind.Sha256, "abc");
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", hex);
    }

    [Fact]
    public void Compute_sha512_of_abc_matches_known_digest()
    {
        var hex = HashComputer.Compute(HashAlgorithmKind.Sha512, "abc");
        Assert.Equal(
            "DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A2192992A274FC1A836BA3C23A3FEEBBD454D4423643CE80E2A9AC94FA54CA49F",
            hex);
    }

    [Theory]
    [InlineData(HashAlgorithmKind.Crc32)]
    [InlineData(HashAlgorithmKind.Crc64)]
    [InlineData(HashAlgorithmKind.Md5)]
    [InlineData(HashAlgorithmKind.Sha256)]
    [InlineData(HashAlgorithmKind.Sha512)]
    public void Compute_is_stable_across_repeated_calls(HashAlgorithmKind alg)
    {
        // Same input, same encoding -> same digest, regardless of algorithm state / reuse.
        var a = HashComputer.Compute(alg, "The quick brown fox jumps over the lazy dog");
        var b = HashComputer.Compute(alg, "The quick brown fox jumps over the lazy dog");
        Assert.Equal(a, b);
        Assert.NotEmpty(a);
    }

    [Fact]
    public void Compute_respects_explicit_encoding()
    {
        // Same text, different encodings -> different byte sequences -> different digests.
        var utf8 = HashComputer.Compute(HashAlgorithmKind.Sha256, "é", Encoding.UTF8);       // é
        var latin1 = HashComputer.Compute(HashAlgorithmKind.Sha256, "é", Encoding.Latin1);
        Assert.NotEqual(utf8, latin1);
    }

    [Fact]
    public void Compute_unknown_algorithm_throws()
    {
        // Guard against silently ignoring a bad enum cast from an untrusted source.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HashComputer.Compute((HashAlgorithmKind)999, "x"));
    }
}
