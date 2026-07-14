namespace BlazorAtoms.Highlights.Tests;

public class HighlightColorTests
{
    [Theory]
    [InlineData("#ffffff", "#1f2937")]
    [InlineData("#fde047", "#1f2937")]
    [InlineData("#000000", "#ffffff")]
    [InlineData("#1e3a8a", "#ffffff")]
    [InlineData("#fff", "#1f2937")]
    [InlineData("#000", "#ffffff")]
    public void SuggestForeground_Picks_Readable_Color(string bg, string expected)
    {
        Assert.Equal(expected, HighlightColor.SuggestForeground(bg));
    }

    [Theory]
    [InlineData("#1f2937", "#fde047")]
    [InlineData("#ffffff", "#1e3a8a")]
    public void SuggestBackground_BestContrast_Picks_Readable_Color(string fg, string expected)
    {
        Assert.Equal(expected, HighlightColor.SuggestBackground(fg, HighlightColorStrategy.BestContrast));
    }

    [Theory]
    [InlineData("#ffffff", HighlightColorStrategy.BestContrast)]
    [InlineData("#ff0000", HighlightColorStrategy.Complementary)]
    public void SuggestBackground_Supports_Strategies(string fg, HighlightColorStrategy strategy)
    {
        var result = HighlightColor.SuggestBackground(fg, strategy);
        Assert.NotNull(result);
        Assert.StartsWith("#", result);
    }

    [Theory]
    [InlineData("#ffffff")]
    [InlineData("#1f2937")]
    public void SuggestForeground_BestContrast_Matches_Default(string fg)
    {
        var a = HighlightColor.SuggestForeground(fg);
        var b = HighlightColor.SuggestForeground(fg, HighlightColorStrategy.BestContrast);
        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData("#ffffff", "#000000", 21.0)]
    [InlineData("#ffffff", "#ffffff", 1.0)]
    public void ContrastRatio_Known_Pairs(string a, string b, double expected)
    {
        Assert.Equal(expected, HighlightColor.ContrastRatio(a, b), precision: 1);
    }
}
