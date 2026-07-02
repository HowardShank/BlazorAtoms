using ZXing;
using ZXing.OneD;

namespace BlazorAtoms.Barcodes.Tests;

public class ItfCodabarTests
{
    // ITF has a single canonical encoding (unlike Code 128), so verify against ZXing's own
    // ITFWriter byte-for-byte. (Decoding a synthetic ITF image is unreliable in ZXing's
    // PureBarcode mode — a start-pattern-detection trait of ITF, not an encoding fault.)
    [Theory]
    [InlineData("1234")]
    [InlineData("12345678")]
    [InlineData("98765432")]
    [InlineData("00000000")]
    public void Itf_matches_ZXing_reference_encoding(string value)
    {
        var actual = ItfEncoder.Encode(value);
        var expected = new ITFWriter().encode(value);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Itf_odd_length_is_zero_padded()
    {
        // "123" pads to "0123": the module patterns must be identical (structural check,
        // independent of the reader's leading-zero quirk).
        Assert.Equal(ItfEncoder.Encode("0123"), ItfEncoder.Encode("123"));
    }

    [Fact]
    public void Itf_non_digit_throws()
    {
        Assert.Throws<System.FormatException>(() => ItfEncoder.Encode("12A4"));
    }

    [Theory]
    [InlineData("A12345A", "12345")]   // ZXing returns the payload without the A–D guards
    [InlineData("A1234B", "1234")]
    public void Codabar_guarded_value_roundtrips(string value, string expected)
    {
        var modules = CodabarEncoder.Encode(value);
        var decoded = BarcodeDecoder.Decode(modules, BarcodeFormat.CODABAR);
        Assert.Equal(expected, decoded);
    }

    [Fact]
    public void Codabar_auto_wraps_and_roundtrips()
    {
        var modules = CodabarEncoder.Encode("1234567"); // auto-wrapped with 'A' guards
        var decoded = BarcodeDecoder.Decode(modules, BarcodeFormat.CODABAR);
        Assert.Equal("1234567", decoded);
    }
}
