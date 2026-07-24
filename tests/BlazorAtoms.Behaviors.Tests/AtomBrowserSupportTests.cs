using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorAtoms.Behaviors.Tests;

/// <summary>bUnit coverage for <see cref="AtomBrowserSupport"/>: the supported/unsupported
/// branches and per-runtime result caching. No component under test needed — bUnit's
/// <c>ctx.JSInterop</c> itself implements <see cref="Microsoft.JSInterop.IJSRuntime"/>.</summary>
public class AtomBrowserSupportTests
{
    private const string ModulePath = "./_content/BlazorAtoms.Behaviors/atom-behaviors.js";

    [Fact]
    public async Task SupportsCssAsync_returns_true_when_browser_supports()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("supportsCss", _ => true).SetResult(true);

        var result = await AtomBrowserSupport.SupportsCssAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), "transition-behavior", "allow-discrete");

        Assert.True(result);
    }

    [Fact]
    public async Task SupportsCssAsync_returns_false_when_browser_does_not_support()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("supportsCss", _ => true).SetResult(false);

        var result = await AtomBrowserSupport.SupportsCssAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), "transition-behavior", "allow-discrete");

        Assert.False(result);
    }

    [Fact]
    public async Task SupportsCssAsync_caches_per_property_value_pair_on_the_same_runtime()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("supportsCss", _ => true).SetResult(true);

        await AtomBrowserSupport.SupportsCssAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), "transition-behavior", "allow-discrete");
        await AtomBrowserSupport.SupportsCssAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), "transition-behavior", "allow-discrete");

        Assert.Single(module.Invocations, i => i.Identifier == "supportsCss");
    }

    [Fact]
    public async Task SupportsCssAsync_does_not_cache_across_different_property_value_pairs()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("supportsCss", _ => true).SetResult(true);

        await AtomBrowserSupport.SupportsCssAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), "transition-behavior", "allow-discrete");
        await AtomBrowserSupport.SupportsCssAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), "gap", "1px");

        Assert.Equal(2, module.Invocations.Count(i => i.Identifier == "supportsCss"));
    }

    [Fact]
    public async Task NextFrameAsync_invokes_the_shared_helper()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("nextFrame", _ => true).SetVoidResult();

        await AtomBrowserSupport.NextFrameAsync(ctx.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>());

        module.VerifyInvoke("nextFrame");
    }
}
