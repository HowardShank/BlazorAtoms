using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Tabs;

/// <summary>
/// One content region in an <see cref="AtomTabs"/>. Shown when the <see cref="AtomTab"/> with the same
/// <see cref="Value"/> is selected.
/// </summary>
/// <remarks>
/// Unlike <see cref="AtomTab"/> this does not register with the parent: it needs nothing but the
/// active value and the shared id prefix, both of which it reads at render time. Only the strip needs
/// an ordered list, and that is for arrow navigation.
/// </remarks>
public partial class AtomTabPanel : AtomComponentBase
{
    /// <summary>Set once this panel has been active, for <see cref="TabPanelRender.Lazy"/>. Never
    /// reset — "has been opened" is the whole point.</summary>
    private bool _hasBeenActive;

    [CascadingParameter] private AtomTabs? Parent { get; set; }

    /// <summary>Key identifying this panel. Must match its tab's <c>Value</c>.</summary>
    [Parameter, EditorRequired] public string Value { get; set; } = "";

    /// <summary>Panel content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Inner padding in px → <c>--tabs-panel-padding</c>. Null (default) inherits the
    /// container's <c>PanelPadding</c>, then the CSS default.</summary>
    [Parameter] public double? Padding { get; set; }

    private bool IsActive => Parent?.IsActive(Value) ?? false;

    private string? Id => Parent?.PanelId(Value);

    private string? TabId => Parent?.TabId(Value);

    /// <summary>Whether this panel's element is in the DOM at all — as opposed to present but
    /// <c>hidden</c>, which is what the two persistent strategies do.</summary>
    private bool ShouldRenderPanel => (Parent?.EffectivePanelRender ?? TabPanelRender.Active) switch
    {
        TabPanelRender.Always => true,
        TabPanelRender.Lazy => _hasBeenActive,
        _ => IsActive,
    };

    private string? PanelStyle =>
        new StyleVars("tabs").Add("panel-padding", Padding ?? Parent?.EffectivePanelPadding).ToString()
            is { Length: > 0 } s ? s : null;

    /// <summary>Latches the Lazy flag. In <c>OnParametersSet</c> rather than the render path so the
    /// component never mutates state while rendering.</summary>
    protected override void OnParametersSet()
    {
        if (IsActive) _hasBeenActive = true;
    }
}
