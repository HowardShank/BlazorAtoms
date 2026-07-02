namespace BlazorAtoms.Barcodes.Tests;

public class QrTests
{
    [Theory]
    [InlineData("HELLO WORLD")]
    [InlineData("https://github.com/HowardShank/BlazorAtoms")]
    [InlineData("1234567890")]
    [InlineData("The quick brown fox jumps over the lazy dog.")]
    [InlineData("A")]
    public void Roundtrips_at_default_level(string value)
    {
        var m = QrEncoder.Encode(value, QrErrorCorrection.M);
        Assert.Equal(value, BarcodeDecoder.DecodeMatrix(m));
    }

    [Theory]
    [InlineData(QrErrorCorrection.L)]
    [InlineData(QrErrorCorrection.M)]
    [InlineData(QrErrorCorrection.Q)]
    [InlineData(QrErrorCorrection.H)]
    public void Roundtrips_at_every_ec_level(QrErrorCorrection level)
    {
        const string value = "BlazorAtoms.Barcodes — QR round-trip 0123456789";
        var m = QrEncoder.Encode(value, level);
        Assert.Equal(value, BarcodeDecoder.DecodeMatrix(m));
    }

    [Fact]
    public void Longer_payload_pushes_to_a_higher_version_and_still_roundtrips()
    {
        var value = string.Concat(System.Linq.Enumerable.Repeat("ABC123-", 40)); // 280 chars
        var m = QrEncoder.Encode(value, QrErrorCorrection.L);
        Assert.Equal(value, BarcodeDecoder.DecodeMatrix(m));
    }

    [Fact]
    public void Matrix_is_square_with_expected_size()
    {
        var m = QrEncoder.Encode("A", QrErrorCorrection.M); // smallest → version 1 → 21×21
        Assert.Equal(21, m.GetLength(0));
        Assert.Equal(21, m.GetLength(1));
    }
}
