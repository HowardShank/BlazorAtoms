using ZXing;

namespace BlazorAtoms.Barcodes.Tests;

public class Code39Tests
{
    [Theory]
    [InlineData("HELLO")]
    [InlineData("CODE39")]
    [InlineData("12345")]
    [InlineData("ABC-123")]
    [InlineData("A")]
    [InlineData("0123456789")]
    public void Encodes_and_roundtrips_through_ZXing(string value)
    {
        var modules = Code39Encoder.Encode(value);
        var decoded = BarcodeDecoder.Decode(modules, BarcodeFormat.CODE_39);
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void Lowercase_is_upper_cased()
    {
        var modules = Code39Encoder.Encode("abc");
        var decoded = BarcodeDecoder.Decode(modules, BarcodeFormat.CODE_39);
        Assert.Equal("ABC", decoded);
    }

    [Fact]
    public void Unsupported_character_throws()
    {
        Assert.Throws<System.FormatException>(() => Code39Encoder.Encode("NO!"));
    }
}
