namespace BlazorAtoms.ActivityIndicators.Tests;

// Step 1 smoke tests — prove the wrapper's discovery, name resolution, parameter
// filtering, and unknown-name callback are wired correctly. Full coverage is Step 2.
public class ActivityIndicatorTests : TestContext
{
    [Fact]
    public void No_name_renders_a_random_indicator()
    {
        var cut = RenderComponent<ActivityIndicator>();

        // A round Busy* indicator was discovered and rendered.
        Assert.NotNull(cut.Find("svg"));
    }

    [Theory]
    [InlineData("BusyGears")]
    [InlineData("Gears")]            // "Busy" prefix is optional
    public void Named_resolves_the_specific_indicator(string name)
    {
        var cut = RenderComponent<ActivityIndicator>(p => p.Add(c => c.Name, name));

        Assert.Contains("busy-gears", cut.Markup);
    }

    [Fact]
    public void Unsupported_parameter_is_filtered_not_thrown()
    {
        // BusySwarm declares no Fill; forwarding it must be silently dropped, never throw.
        var cut = RenderComponent<ActivityIndicator>(p => p
            .Add(c => c.Name, "Swarm")
            .Add(c => c.Fill, "red"));

        Assert.Contains("busy-swarm", cut.Markup);
    }

    [Fact]
    public void Unknown_name_invokes_callback_then_falls_back_to_random()
    {
        var called = 0;
        string? requested = null;

        var cut = RenderComponent<ActivityIndicator>(p => p
            .Add(c => c.Name, "DoesNotExist")
            .Add(c => c.OnUnknownName, EventCallback.Factory.Create<string>(this, s => { called++; requested = s; })));

        Assert.Equal(1, called);
        Assert.Equal("DoesNotExist", requested);
        Assert.NotNull(cut.Find("svg"));   // still rendered something
    }
}
