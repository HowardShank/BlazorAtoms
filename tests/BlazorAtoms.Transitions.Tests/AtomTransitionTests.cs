using Bunit;
using Xunit;

namespace BlazorAtoms.Transitions.Tests;

/// <summary>bUnit coverage for <see cref="AtomTransition"/>: default hidden state, the shown-class
/// flip after first render, effect classes, duration style var, and OnEntered/OnExited events.
/// JS interop flows through BlazorAtoms.Behaviors's module, not a module of this package's own.</summary>
public class AtomTransitionTests
{
    private const string ModulePath = "./_content/BlazorAtoms.Behaviors/atom-behaviors.js";

    private static void SetupNativeSupport(BunitContext ctx, bool supported)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("supportsCss", _ => true).SetResult(supported);
        module.SetupVoid("nextFrame", _ => true).SetVoidResult();
    }

    [Fact]
    public void Hidden_by_default_renders_without_shown_class()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx, supported: true);

        var cut = ctx.Render<AtomTransition>();

        var root = cut.Find(".atom-transition");
        Assert.DoesNotContain("atom-transition-shown", root.ClassList);
        Assert.Equal("true", root.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Show_true_gets_the_shown_class_after_render_native_path()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx, supported: true);

        var cut = ctx.Render<AtomTransition>(p => p.Add(x => x.Show, true));

        var root = cut.Find(".atom-transition");
        Assert.Contains("atom-transition-shown", root.ClassList);
        Assert.Equal("false", root.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Show_true_gets_the_shown_class_after_render_fallback_path()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx, supported: false);

        var cut = ctx.Render<AtomTransition>(p => p.Add(x => x.Show, true));

        var root = cut.Find(".atom-transition");
        Assert.Contains("atom-transition-shown", root.ClassList);
    }

    [Theory]
    [InlineData(AtomTransitionEffect.Fade, "atom-transition-fade")]
    [InlineData(AtomTransitionEffect.Pop, "atom-transition-pop")]
    [InlineData(AtomTransitionEffect.FadeScale, "atom-transition-fadescale")]
    [InlineData(AtomTransitionEffect.SlideUp, "atom-transition-slideup")]
    [InlineData(AtomTransitionEffect.SlideDown, "atom-transition-slidedown")]
    [InlineData(AtomTransitionEffect.SlideLeft, "atom-transition-slideleft")]
    [InlineData(AtomTransitionEffect.SlideRight, "atom-transition-slideright")]
    [InlineData(AtomTransitionEffect.ShiftBlur, "atom-transition-shiftblur")]
    [InlineData(AtomTransitionEffect.FlipY20, "atom-transition-flipy20")]
    [InlineData(AtomTransitionEffect.FlipYNeg20, "atom-transition-flipyneg20")]
    [InlineData(AtomTransitionEffect.FlipX20, "atom-transition-flipx20")]
    [InlineData(AtomTransitionEffect.FlipXNeg20, "atom-transition-flipxneg20")]
    [InlineData(AtomTransitionEffect.BounceUp, "atom-transition-bounceup")]
    [InlineData(AtomTransitionEffect.BounceDown, "atom-transition-bouncedown")]
    [InlineData(AtomTransitionEffect.BounceLeft, "atom-transition-bounceleft")]
    [InlineData(AtomTransitionEffect.BounceRight, "atom-transition-bounceright")]
    [InlineData(AtomTransitionEffect.GrowLeft, "atom-transition-growleft")]
    [InlineData(AtomTransitionEffect.GrowRight, "atom-transition-growright")]
    [InlineData(AtomTransitionEffect.GrowTop, "atom-transition-growtop")]
    [InlineData(AtomTransitionEffect.GrowBottom, "atom-transition-growbottom")]
    public void Effect_emits_class(AtomTransitionEffect effect, string cls)
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx, supported: true);

        var cut = ctx.Render<AtomTransition>(p => p.Add(x => x.Effect, effect));

        Assert.Contains(cls, cut.Find(".atom-transition").ClassList);
    }

    [Fact]
    public void Duration_emits_style_variable()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx, supported: true);

        var cut = ctx.Render<AtomTransition>(p => p.Add(x => x.Duration, 500));

        Assert.Contains("--atom-transition-duration:500ms;", cut.Find(".atom-transition").GetAttribute("style"));
    }

    [Fact]
    public void Toggling_show_after_mount_fires_OnEntered_and_OnExited()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx, supported: true);
        var entered = 0;
        var exited = 0;

        var cut = ctx.Render<AtomTransition>(p => p
            .Add(x => x.OnEntered, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => entered++))
            .Add(x => x.OnExited, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => exited++)));

        cut.Render(p => p.Add(x => x.Show, true));
        Assert.Equal(1, entered);

        cut.Render(p => p.Add(x => x.Show, false));
        Assert.Equal(1, exited);
    }

    [Fact]
    public void ChildContent_renders_inside_wrapper()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx, supported: true);

        var cut = ctx.Render<AtomTransition>(p => p.AddChildContent("<span>hello</span>"));

        Assert.Contains("hello", cut.Find(".atom-transition").TextContent);
    }
}
