using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Highlights;

/// <summary>
/// Keyword highlighter for arbitrary nested content — including live child components — that
/// operates on the real, rendered DOM instead of Blazor's render tree. Wraps <see cref="ChildContent"/>
/// in a container and, after every render, calls into a small self-imported JS module that walks the
/// container's text nodes and wraps matches in <c>&lt;mark&gt;</c> elements, scoped to that container only.
/// Because it works against the live DOM, nesting depth doesn't matter: a Grandparent that hosts a Parent
/// that hosts a Child is highlighted correctly without touching any of those components individually.
/// </summary>
public partial class AtomHighlighter : AtomComponentCore, IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorAtoms.Highlights/atom-highlighter.js";

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _container;
    private IJSObjectReference? _module;

    // Stable per-instance identity for marks this instance owns — decoupled from HighlightClass
    // (purely visual) so two nested instances sharing the same class never strip each other's
    // marks. Set once for this instance's lifetime; a real remount gets a fresh id, which is fine.
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    /// <summary>The content to highlight. May contain arbitrarily nested child components. Required.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Keywords to match inside the container's text content.</summary>
    [Parameter] public string[] Keywords { get; set; } = [];

    /// <summary>CSS class applied to each injected <c>&lt;mark&gt;</c> element. Default: <c>"atom-highlighter"</c>.</summary>
    [Parameter] public string HighlightClass { get; set; } = "atom-highlighter";

    /// <summary>Case-sensitive matching. Default: false.</summary>
    [Parameter] public bool CaseSensitive { get; set; }

    /// <summary>Match whole words only. Default: false.</summary>
    [Parameter] public bool WholeWord { get; set; }

    /// <summary>Visual treatment for highlighted matches. Default: Mark.</summary>
    [Parameter] public HighlightStyle HighlightStyle { get; set; } = HighlightStyle.Mark;

    /// <summary>Background color (or underline/outline color) of highlighted matches. Sets <c>--highlighter-bg</c>.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color of highlighted matches. Sets <c>--highlighter-color</c>.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Corner radius of the highlighted background. Sets <c>--highlighter-radius</c>.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Inline padding of the highlighted background. Sets <c>--highlighter-padding</c>.</summary>
    [Parameter] public string? Padding { get; set; }

    private string? RootStyle
    {
        get
        {
            var vars = new StyleVars("highlighter")
                .Add("bg", Background)
                .Add("color", Color)
                .Add("radius", Radius)
                .Add("padding", Padding)
                .ToString();
            return string.IsNullOrEmpty(vars) ? null : vars;
        }
    }

    private string StyleValue => HighlightStyle switch
    {
        HighlightStyle.Underline => "underline",
        HighlightStyle.Outline => "outline",
        _ => "mark",
    };

    private object BuildOptions() => new
    {
        style = StyleValue,
        caseSensitive = CaseSensitive,
        wholeWord = WholeWord,
        owner = _instanceId,
    };

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            }
            catch (JSDisconnectedException) { return; }
            catch (OperationCanceledException) { return; }
        }

        // Always call through, even with zero keywords — the JS side unmarks its own previous
        // matches before (re)scanning, so clearing Keywords must still reach it to clean up.
        if (_module is null) return;

        try
        {
            await _module.InvokeVoidAsync(
                "highlightTextInElement", _container, Keywords, HighlightClass, BuildOptions());
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
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
            catch (OperationCanceledException) { }
        }
    }
}
