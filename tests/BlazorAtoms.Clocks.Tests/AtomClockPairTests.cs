namespace BlazorAtoms.Clocks.Tests;

public class AtomClockPairTests : BunitContext
{
    // Default SecondaryKind is Browser → touches JS; Loose mode keeps it from throwing.
    public AtomClockPairTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Renders_two_clocks_and_a_divider()
    {
        var cut = Render<AtomClockPair>(p => p.Add(c => c.Live, false));

        Assert.Equal(2, cut.FindAll(".atom-clock").Count);
        Assert.NotNull(cut.Find(".atom-clock-divider"));
    }

    [Fact]
    public void Default_labels_are_server_and_local()
    {
        var cut = Render<AtomClockPair>(p => p.Add(c => c.Live, false));

        var labels = cut.FindAll(".atom-clock-label");
        Assert.Equal("Server", labels[0].TextContent);
        Assert.Equal("Local", labels[1].TextContent);
    }

    [Fact]
    public void Layout_sets_data_attribute()
    {
        var side = Render<AtomClockPair>(p => p.Add(c => c.Live, false));
        Assert.Equal("side-by-side", side.Find(".atom-clock-pair").GetAttribute("data-layout"));

        var stacked = Render<AtomClockPair>(p => p
            .Add(c => c.Layout, ClockLayout.Stacked)
            .Add(c => c.Live, false));
        Assert.Equal("stacked", stacked.Find(".atom-clock-pair").GetAttribute("data-layout"));
    }

    [Fact]
    public void Custom_kinds_and_labels_flow_to_children()
    {
        var cut = Render<AtomClockPair>(p => p
            .Add(c => c.PrimaryKind, ClockKind.Utc)
            .Add(c => c.PrimaryLabel, "UTC")
            .Add(c => c.SecondaryKind, ClockKind.Utc)
            .Add(c => c.SecondaryLabel, "Also UTC")
            .Add(c => c.Live, false));

        var clocks = cut.FindAll(".atom-clock");
        Assert.All(clocks, c => Assert.Equal("utc", c.GetAttribute("data-kind")));
        var labels = cut.FindAll(".atom-clock-label");
        Assert.Equal("UTC", labels[0].TextContent);
        Assert.Equal("Also UTC", labels[1].TextContent);
    }

    [Fact]
    public void Gap_token_emitted_when_set()
    {
        var cut = Render<AtomClockPair>(p => p
            .Add(c => c.Gap, 20)
            .Add(c => c.Live, false));

        Assert.Contains("--clkp-gap:20px", cut.Find(".atom-clock-pair").GetAttribute("style") ?? "");
    }
}
