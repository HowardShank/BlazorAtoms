using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Tabs;

/// <summary>
/// One button in an <see cref="AtomTabs"/> strip. Pairs with the <see cref="AtomTabPanel"/> that has
/// the same <see cref="Value"/>.
/// </summary>
/// <remarks>
/// Registers itself with the parent on initialization, which is what gives the strip an ordered list
/// to arrow through — and unregisters on dispose, so a tab removed by an <c>@if</c> stops being a
/// navigation target. Registration order matches DOM order, because Blazor initializes children in
/// the order their parent renders them.
/// </remarks>
public partial class AtomTab : AtomComponentBase, IDisposable
{
    private ElementReference _element;

    [CascadingParameter] private AtomTabs? Parent { get; set; }

    /// <summary>Key identifying this tab. Must match its panel's <c>Value</c>, and be unique within
    /// the strip.</summary>
    [Parameter, EditorRequired] public string Value { get; set; } = "";

    /// <summary>Tab caption. Ignored when <see cref="ChildContent"/> is supplied.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Custom caption markup. Replaces <see cref="Title"/> entirely.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Leading slot — an icon or glyph. Marked <c>aria-hidden</c>, since the caption already
    /// names the tab.</summary>
    [Parameter] public RenderFragment? Icon { get; set; }

    /// <summary>Trailing count/label chip (e.g. <c>"3"</c>). Rendered as text, so it is announced with
    /// the tab — set <see cref="AriaLabel"/> if that reads badly.</summary>
    [Parameter] public string? Badge { get; set; }

    /// <summary>When true the tab can't be selected or focused, and arrow navigation skips it (native
    /// <c>disabled</c> plus <c>aria-disabled</c>).</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Accessible name, when the visible caption isn't the whole story.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private bool IsActive => Parent?.IsActive(Value) ?? false;

    /// <summary>Falls back to <c>-1</c> with no parent so a stray tab never joins the tab order.</summary>
    private int TabIndex => Parent?.TabIndexOf(Value) ?? -1;

    private string? Id => Parent?.TabId(Value);

    private string? PanelId => Parent?.PanelId(Value);

    protected override void OnInitialized() => Parent?.Register(this);

    private Task SelectAsync() => Parent?.SelectAsync(Value) ?? Task.CompletedTask;

    /// <summary>
    /// Moves keyboard focus here, via the framework's own <see cref="ElementReference.FocusAsync"/> — JS
    /// interop, but no module of this package's own.
    /// </summary>
    /// <remarks>
    /// Swallows the three ways interop can fail while the page is going away: a Blazor Server circuit
    /// that has already disconnected, a cancelled interop call, and a JS-side failure (which for a
    /// focus call means the element is no longer in the document). All three are reachable by pressing
    /// an arrow key during teardown or right after a tab is removed, and none of them are worth
    /// surfacing — the user was moving focus, not performing an operation whose failure matters. This
    /// mirrors how every other interop call in the repo is guarded.
    /// </remarks>
    internal async Task FocusAsync()
    {
        try
        {
            await _element.FocusAsync();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
    }

    /// <summary>Called by the parent when the selection or roving focus moves. Needed because the
    /// cascade is <c>IsFixed</c> and never changes reference, so nothing else would re-render this
    /// tab.</summary>
    internal void NotifyStateChanged() => StateHasChanged();

    public void Dispose() => Parent?.Unregister(this);
}
