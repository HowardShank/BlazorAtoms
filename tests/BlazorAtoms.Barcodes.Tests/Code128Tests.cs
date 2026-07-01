using ZXing;

namespace BlazorAtoms.Barcodes.Tests;

public class Code128Tests
{
    [Theory]
    [InlineData("HELLO")]
    [InlineData("Code128!")]
    [InlineData("1234567890")]
    [InlineData("ABC-abc-123")]
    [InlineData("https://example.com/x")]
    [InlineData("A")]
    [InlineData(" ")]                 // space (value 0)
    [InlineData("~")]                 // ASCII 126, top of Set B
    public void Encodes_and_roundtrips_through_ZXing(string value)
    {
        var modules = Code128Encoder.Encode(value);
        var decoded = BarcodeDecoder.Decode(modules, BarcodeFormat.CODE_128);
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void Non_ascii_throws()
    {
        Assert.Throws<System.FormatException>(() => Code128Encoder.Encode("café"));
    }
}
