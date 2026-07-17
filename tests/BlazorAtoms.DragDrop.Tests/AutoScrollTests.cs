using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.DragDrop.Tests;

/// <summary>
/// bUnit-level coverage for the auto-scroll JS interop path. Uses bUnit's strict JS interop
/// so unmatched invocations fail loudly — protects the module contract (module path, function
/// names, argument shape) from silent drift.
/// </summary>
public class AutoScrollTests
{
    private const string ModulePath = "./_content/BlazorAtoms.DragDrop/atom-dropzone.js";

    private sealed record Card(string Title);

    private static readonly RenderFragment<Card> CardTemplate = card => builder =>
        builder.AddContent(0, card.Title);

    [Fact]
    public void First_render_imports_module_and_calls_enableAutoScroll_with_defaults()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("enableAutoScroll", _ => true).SetVoidResult();

        ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, new List<Card> { new("A") })
            .Add(x => x.ChildContent, CardTemplate));

        var call = module.VerifyInvoke("enableAutoScroll");
        // ElementReference is arg 0; edge size (60) and speed (10) follow.
        Assert.Equal(60, call.Arguments[1]);
        Assert.Equal(10, call.Arguments[2]);
    }

    [Fact]
    public void Custom_edge_and_speed_values_flow_to_JS()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("enableAutoScroll", _ => true).SetVoidResult();

        ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, new List<Card> { new("A") })
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.AutoScrollEdgeSize, 120)
            .Add(x => x.AutoScrollSpeed, 25));

        var call = module.VerifyInvoke("enableAutoScroll");
        Assert.Equal(120, call.Arguments[1]);
        Assert.Equal(25, call.Arguments[2]);
    }

    [Fact]
    public void AutoScroll_false_skips_module_import_entirely()
    {
        using var ctx = new TestContext();
        // Strict mode (default) — any JS call throws. Ensures no import happens.
        ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, new List<Card> { new("A") })
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.AutoScroll, false));

        Assert.Empty(ctx.JSInterop.Invocations);
    }

    [Fact]
    public async Task Dispose_calls_disableAutoScroll_and_disposes_module()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("enableAutoScroll", _ => true).SetVoidResult();
        module.SetupVoid("disableAutoScroll", _ => true).SetVoidResult();

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, new List<Card> { new("A") })
            .Add(x => x.ChildContent, CardTemplate));

        // Confirm the wire-up happened before we tear down, otherwise the disable path is a
        // no-op and the assertion below is meaningless.
        cut.WaitForAssertion(() => module.VerifyInvoke("enableAutoScroll"));

        await cut.Instance.DisposeAsync();

        module.VerifyInvoke("disableAutoScroll");
    }

    [Fact]
    public async Task Dispose_swallows_JSDisconnectedException_during_teardown()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("enableAutoScroll", _ => true).SetVoidResult();
        // disableAutoScroll throws on invoke — simulates a circuit that died between mount and dispose.
        module.SetupVoid("disableAutoScroll", _ => true)
              .SetException(new JSDisconnectedException("circuit gone"));

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, new List<Card> { new("A") })
            .Add(x => x.ChildContent, CardTemplate));

        cut.WaitForAssertion(() => module.VerifyInvoke("enableAutoScroll"));

        // Must NOT propagate — component swallows the exception per the disposal contract.
        await cut.Instance.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_is_idempotent_when_called_twice()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("enableAutoScroll", _ => true).SetVoidResult();
        module.SetupVoid("disableAutoScroll", _ => true).SetVoidResult();

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, new List<Card> { new("A") })
            .Add(x => x.ChildContent, CardTemplate));

        await cut.Instance.DisposeAsync();
        // Second call must not throw and must not re-invoke disableAutoScroll on a null module.
        await cut.Instance.DisposeAsync();
    }

    [Fact]
    public void Import_failure_does_not_crash_render_pipeline()
    {
        // Simulates SSR / prerender / JS-unavailable environment: the import throws
        // InvalidOperationException. The component must render normally regardless.
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("enableAutoScroll", _ => true)
              .SetException(new InvalidOperationException("JS interop not available (prerender)"));

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, new List<Card> { new("A") })
            .Add(x => x.ChildContent, CardTemplate));

        // Still renders the DOM.
        Assert.NotNull(cut.Find(".atom-dropzone"));
    }
}
