namespace BlazorAtoms.ActivityIndicators.Tests;

// Smoke tests — prove the wrapper's discovery, name resolution, parameter
// filtering, and unknown-name callback are wired correctly.
public class AtomActivityIndicatorTests : TestContext
{
    [Fact]
    public void No_name_renders_a_random_indicator()
    {
        var cut = RenderComponent<AtomActivityIndicator>();

        // A round AtomActivity* indicator was discovered and rendered.
        Assert.NotNull(cut.Find("svg"));
    }

    [Theory]
    [InlineData("AtomActivityGears")]
    [InlineData("ActivityGears")]        // "Atom" prefix is optional
    [InlineData("Gears")]            // "AtomActivity" prefix is optional
    public void Named_resolves_the_specific_indicator(string name)
    {
        var cut = RenderComponent<AtomActivityIndicator>(p => p.Add(c => c.Name, name));

        Assert.Contains("activity-gears", cut.Markup);
    }

    [Fact]
    public void Unsupported_parameter_is_filtered_not_thrown()
    {
        // AtomActivitySwarm declares no Fill; forwarding it must be silently dropped, never throw.
        var cut = RenderComponent<AtomActivityIndicator>(p => p
            .Add(c => c.Name, "Swarm")
            .Add(c => c.Fill, "red"));

        Assert.Contains("activity-swarm", cut.Markup);
    }

    [Fact]
    public void Unknown_name_invokes_callback_then_falls_back_to_random()
    {
        var called = 0;
        string? requested = null;

        var cut = RenderComponent<AtomActivityIndicator>(p => p
            .Add(c => c.Name, "DoesNotExist")
            .Add(c => c.OnUnknownName, EventCallback.Factory.Create<string>(this, s => { called++; requested = s; })));

        Assert.Equal(1, called);
        Assert.Equal("DoesNotExist", requested);
        Assert.NotNull(cut.Find("svg"));   // still rendered something
    }
}
