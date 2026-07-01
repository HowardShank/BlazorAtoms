using ZXing;

namespace BlazorAtoms.Barcodes.Tests;

public class EanUpcTests
{
    [Theory]
    [InlineData("5901234123457")]        // 13 digits, valid check
    [InlineData("4006381333931")]
    [InlineData("9780306406157")]        // ISBN-13 style
    public void Ean13_thirteen_digits_roundtrips(string value)
    {
        var modules = Ean13Encoder.Encode(value);
        var decoded = BarcodeDecoder.Decode(modules, BarcodeFormat.EAN_13);
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void Ean13_twelve_digits_gets_check_and_roundtrips()
    {
        var modules = Ean13Encoder.Encode("590123412345"); // check digit = 7
        var decoded = BarcodeDecoder.Decode(modules, BarcodeFormat.EAN_13);
        Assert.Equal("5901234123457", decoded);
    }

    [Fact]
    public void Ean13_bad_check_throws()
    {
        Assert.Throws<System.FormatException>(() => Ean13Encoder.Encode("5901234123450"));
    }

    [Theory]
    [InlineData("036000291452")]         // 12 digits, valid check
    [InlineData("012345678905")]
    public void UpcA_twelve_digits_roundtrips(string value)
    {
        var modules = UpcAEncoder.Encode(value);
        var decoded = BarcodeDecoder.Decode(modules, BarcodeFormat.UPC_A);
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void UpcA_eleven_digits_gets_check_and_roundtrips()
    {
        var modules = UpcAEncoder.Encode("03600029145"); // check digit = 2
        var decoded = BarcodeDecoder.Decode(modules, BarcodeFormat.UPC_A);
        Assert.Equal("036000291452", decoded);
    }
}
