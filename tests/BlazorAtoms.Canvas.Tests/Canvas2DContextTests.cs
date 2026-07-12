namespace BlazorAtoms.Canvas.Tests;

// The imperative escape hatch must batch a whole frame of ops into ONE interop call — that is the
// guarantee that keeps it usable over a Blazor Server circuit (one round-trip, not one per op).
public class Canvas2DContextTests : TestContext
{
    [Fact]
    public async Task Batches_all_queued_ops_into_a_single_runCommands_call()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/BlazorAtoms.Canvas/atom-canvas.js");

        var cut = RenderComponent<AtomCanvas>();
        var ctx = await cut.Instance.GetContext2DAsync();

        ctx.FillStyle("#0ea5e9")
           .FillRect(0, 0, 10, 10)
           .BeginPath()
           .MoveTo(0, 0)
           .LineTo(5, 5)
           .Stroke();
        await ctx.FlushAsync();

        var invocation = module.VerifyInvoke("runCommands");
        var batch = Assert.IsAssignableFrom<System.Collections.IEnumerable>(invocation.Arguments[1]!);
        Assert.Equal(6, batch.Cast<object>().Count());
    }

    [Fact]
    public async Task Empty_context_does_not_call_runCommands()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/BlazorAtoms.Canvas/atom-canvas.js");

        var cut = RenderComponent<AtomCanvas>();
        var ctx = await cut.Instance.GetContext2DAsync();
        await ctx.FlushAsync(); // nothing queued

        Assert.DoesNotContain(module.Invocations, i => i.Identifier == "runCommands");
    }
}
