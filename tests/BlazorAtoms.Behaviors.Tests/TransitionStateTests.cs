using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorAtoms.Behaviors.Tests;

/// <summary>bUnit coverage for <see cref="TransitionState"/>'s hybrid first-paint sequencing and
/// plain post-first-render toggling.</summary>
public class TransitionStateTests
{
    private const string ModulePath = "./_content/BlazorAtoms.Behaviors/atom-behaviors.js";

    [Fact]
    public void SetShown_is_synchronous_and_needs_no_js()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict; // any JS call would fail the test outright

        var state = new TransitionState();
        state.SetShown(true);

        Assert.True(state.Shown);
    }

    [Fact]
    public async Task InitializeAsync_with_initialShow_false_does_not_call_js()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict; // any JS call would fail the test outright

        var state = new TransitionState();
        var needsRerender = await state.InitializeAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), initialShow: false);

        Assert.False(needsRerender);
        Assert.False(state.Shown);
    }

    [Fact]
    public async Task InitializeAsync_native_support_shows_without_waiting_a_frame()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("supportsCss", _ => true).SetResult(true);

        var state = new TransitionState();
        var needsRerender = await state.InitializeAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), initialShow: true);

        Assert.True(needsRerender);
        Assert.True(state.Shown);
        Assert.DoesNotContain(module.Invocations, i => i.Identifier == "nextFrame");
    }

    [Fact]
    public async Task InitializeAsync_falls_back_to_nextFrame_when_unsupported()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("supportsCss", _ => true).SetResult(false);
        module.SetupVoid("nextFrame", _ => true).SetVoidResult();

        var state = new TransitionState();
        var needsRerender = await state.InitializeAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), initialShow: true);

        Assert.True(needsRerender);
        Assert.True(state.Shown);
        module.VerifyInvoke("nextFrame");
    }

    [Fact]
    public async Task InitializeAsync_is_a_no_op_after_the_first_call()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("supportsCss", _ => true).SetResult(true);

        var state = new TransitionState();
        await state.InitializeAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), initialShow: true);
        var second = await state.InitializeAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), initialShow: false);

        Assert.False(second);
        Assert.True(state.Shown); // untouched by the second, ignored call
    }
}
