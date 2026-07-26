using BlazorAtoms.Behaviors;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Transitions;

/// <summary>Wraps arbitrary child content and glows whichever direct child is currently
/// hovered/focused, sliding between them. Pure CSS (anchor positioning) where supported; a small
/// JS fallback replicates it elsewhere. See the .razor file's comment for the full split.</summary>
public partial class AtomHoverGlow : IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorAtoms.Transitions/atom-hover-glow.js";

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _containerRef;
    private ElementReference _indicatorRef;
    private IJSObjectReference? _module;
    private bool _attachAttempted;

    /// <summary>The wrapped content — any elements; the glow tracks whichever direct child is
    /// hovered or contains focus.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Color of the glow.</summary>
    [Parameter] public string GlowColor { get; set; } = "#ff1493";

    /// <summary>Blur radius of the glow's outer box-shadow. Any CSS length.</summary>
    [Parameter] public string GlowBlur { get; set; } = "32px";

    /// <summary>Corner radius of the glow indicator — tune to roughly match your children's own
    /// shape (pill, rounded rect, square) for the best visual fit.</summary>
    [Parameter] public string GlowRadius { get; set; } = "0.5rem";

    private string RootStyle =>
        $"--atom-hover-glow-color:{GlowColor};" +
        $"--atom-hover-glow-blur:{GlowBlur};" +
        $"--atom-hover-glow-radius:{GlowRadius};";

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _attachAttempted)
        {
            return;
        }

        _attachAttempted = true;

        var nativeSupport = await AtomBrowserSupport.SupportsCssAsync(JS, "anchor-name", "--atom-hover-glow-probe");
        if (nativeSupport)
        {
            return; // pure CSS handles it — see AtomHoverGlow.razor.css; no JS needed
        }

        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            await _module.InvokeVoidAsync("attachHoverGlow", _containerRef, _indicatorRef);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }
}
