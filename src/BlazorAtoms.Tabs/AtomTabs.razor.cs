using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Tabs;

/// <summary>
/// Tab container: owns the selected value, renders the <c>role="tablist"</c> strip and the panel
/// region, and handles keyboard navigation for the whole family. Pair an <see cref="AtomTab"/> in
/// <see cref="TabList"/> with an <see cref="AtomTabPanel"/> in <see cref="Panels"/> by giving both the
/// same <c>Value</c>.
/// </summary>
/// <remarks>
/// <para><b>Why two named slots instead of one flat child list.</b> Blazor renders children in source
/// order, so interleaved <c>&lt;AtomTab&gt;</c>/<c>&lt;AtomTabPanel&gt;</c> could not be laid out as a
/// strip above a panel region — that needs nodes hoisted across the render tree, which the framework
/// cannot do. Two fragments keep the DOM correct without asking the caller to declare things twice in
/// a specific order.</para>
/// <para><b>Why this cascades the component and not a DTO.</b> <c>ButtonGroupContext</c> and
/// <c>CardContext</c> are plain value carriers, because their children only need to *read* inherited
/// styling. Tabs is different: a tab has to register itself so the strip has an ordered, focusable
/// list to arrow through, and has to be told to re-render when the selection moves. That is behavior,
/// so the component itself is what cascades.</para>
/// <para><b>Selection is derived, never written behind the caller's back.</b> See
/// <see cref="ActiveValue"/>.</para>
/// <para><b>One small JS module.</b> <c>atom-tabs.js</c> exists only to cancel the browser's default
/// scrolling for the navigation keys — something Blazor's render-time <c>:preventDefault</c> cannot do
/// selectively. It is lazy-imported by this component, so there is still no <c>&lt;script&gt;</c> tag or
/// DI registration for a consumer to wire up, and all logic and state stay in C#.</para>
/// </remarks>
public partial class AtomTabs : AtomComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorAtoms.Tabs/atom-tabs.js";

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _listRef;
    private IJSObjectReference? _module;

    /// <summary>Registration order == DOM order, because child components initialize in the order
    /// their parent renders them. That is what makes "next tab" mean what it looks like.</summary>
    private readonly List<AtomTab> _tabs = [];

    /// <summary>Per-instance prefix for the <c>aria-controls</c>/<c>aria-labelledby</c> ids. Generated
    /// once in a field (not per render), so it is stable across the prerender and interactive passes —
    /// the same technique <c>AtomAvatar</c>, <c>AtomShapedTooltip</c> and five others use.</summary>
    private readonly string _idPrefix = "tabs-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>Which tab has keyboard focus. Diverges from <see cref="Value"/> only in
    /// <see cref="TabsActivation.Manual"/>, where arrowing moves focus without selecting.</summary>
    private string? _focusedValue;

    // ---- content -----------------------------------------------------------------------------

    /// <summary>The strip. Put <see cref="AtomTab"/> components here.</summary>
    [Parameter] public RenderFragment? TabList { get; set; }

    /// <summary>The content region. Put <see cref="AtomTabPanel"/> components here.</summary>
    [Parameter] public RenderFragment? Panels { get; set; }

    // ---- selection ---------------------------------------------------------------------------

    /// <summary>Value of the selected tab. Bind with <c>@bind-Value</c>.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Raised when a different tab is selected. Backs <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>What an arrow key does. Default <see cref="TabsActivation.Automatic"/>.</summary>
    [Parameter] public TabsActivation ActivationMode { get; set; } = TabsActivation.Automatic;

    /// <summary>When each panel's content is in the DOM. Default
    /// <see cref="TabPanelRender.Active"/>.</summary>
    [Parameter] public TabPanelRender PanelRender { get; set; } = TabPanelRender.Active;

    // ---- structure ---------------------------------------------------------------------------

    /// <summary>Accessible name for the tablist — worth setting when a page has more than one set of
    /// tabs, so they are distinguishable.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>When true the strip scrolls along its own axis instead of wrapping. Pure CSS
    /// <c>overflow</c>; no scroll buttons (those need measurement, i.e. JS).</summary>
    [Parameter] public bool Scrollable { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    // ---- styling axes ------------------------------------------------------------------------

    /// <summary>Strip look → <c>data-variant</c>. Default <see cref="TabsVariant.Line"/>.</summary>
    [Parameter] public TabsVariant Variant { get; set; } = TabsVariant.Line;

    /// <summary>Density preset → <c>data-size</c>. Default <see cref="TabsSize.Medium"/>.</summary>
    [Parameter] public TabsSize Size { get; set; } = TabsSize.Medium;

    /// <summary>Strip axis → <c>data-orientation</c> and <c>aria-orientation</c>. Default
    /// <see cref="TabsOrientation.Horizontal"/>.</summary>
    [Parameter] public TabsOrientation Orientation { get; set; } = TabsOrientation.Horizontal;

    /// <summary>Tab distribution → <c>data-align</c>. Default <see cref="TabsAlign.Start"/>.</summary>
    [Parameter] public TabsAlign Align { get; set; } = TabsAlign.Start;

    /// <summary>Opt-in CSS motion → <c>data-effect</c>. Default <see cref="TabsEffect.None"/> (no
    /// attribute emitted).</summary>
    [Parameter] public TabsEffect Effect { get; set; } = TabsEffect.None;

    // ---- theming (→ --tabs-* custom properties) -----------------------------------------------

    /// <summary>Accent for the indicator, the active tab and the focus ring →
    /// <c>--tabs-accent</c>.</summary>
    [Parameter] public string? AccentColor { get; set; }

    /// <summary>Idle tab text color → <c>--tabs-tab-color</c>.</summary>
    [Parameter] public string? TabColor { get; set; }

    /// <summary>Selected tab text color → <c>--tabs-active-tab-color</c>. Defaults to the accent (or
    /// to contrasting ink on the filled variants).</summary>
    [Parameter] public string? ActiveTabColor { get; set; }

    /// <summary>Indicator/strip rule color → <c>--tabs-indicator-color</c>. Defaults to the accent.</summary>
    [Parameter] public string? IndicatorColor { get; set; }

    /// <summary>Indicator thickness in px → <c>--tabs-indicator-thickness</c>.</summary>
    [Parameter] public double? IndicatorThickness { get; set; }

    /// <summary>Strip and panel border color → <c>--tabs-border-color</c>.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Panel background → <c>--tabs-panel-bg</c>.</summary>
    [Parameter] public string? PanelBackgroundColor { get; set; }

    /// <summary>Corner radius in px → <c>--tabs-radius</c>.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Panel inner padding in px → <c>--tabs-panel-padding</c>. A panel's own
    /// <c>Padding</c> still wins.</summary>
    [Parameter] public double? PanelPadding { get; set; }

    /// <summary>Space between tabs in px → <c>--tabs-gap</c>.</summary>
    [Parameter] public double? Gap { get; set; }

    /// <summary>Tab font size in px → <c>--tabs-font-size</c>.</summary>
    [Parameter] public double? FontSize { get; set; }

    /// <summary>Transition duration in seconds → <c>--tabs-duration</c>.</summary>
    [Parameter] public double? Duration { get; set; }

    // ---- derived state -----------------------------------------------------------------------

    /// <summary>
    /// The value actually shown as selected: <see cref="Value"/> when it matches a registered tab,
    /// otherwise the first enabled tab.
    /// </summary>
    /// <remarks>
    /// Derived rather than assigned. The obvious alternative — writing the first tab's value into
    /// <see cref="Value"/> during registration and raising <see cref="ValueChanged"/> — mutates the
    /// caller's bound field from inside a child's initialization, before the user has interacted with
    /// anything. This way an unset or stale <c>Value</c> still shows a sensible panel, the caller's
    /// field is only ever changed by a real selection, and <c>@bind-Value</c> has no surprise write on
    /// first render.
    /// </remarks>
    internal string? ActiveValue
    {
        get
        {
            if (!string.IsNullOrEmpty(Value) && _tabs.Any(t => t.Value == Value)) return Value;
            return _tabs.FirstOrDefault(t => !t.Disabled)?.Value;
        }
    }

    internal TabPanelRender EffectivePanelRender => PanelRender;

    internal double? EffectivePanelPadding => PanelPadding;

    /// <summary>Id of a tab button, referenced by its panel's <c>aria-labelledby</c>.</summary>
    internal string TabId(string? value) => $"{_idPrefix}-tab-{Slug(value)}";

    /// <summary>Id of a panel, referenced by its tab's <c>aria-controls</c>.</summary>
    internal string PanelId(string? value) => $"{_idPrefix}-panel-{Slug(value)}";

    internal bool IsActive(string? value) => value is not null && value == ActiveValue;

    /// <summary>Roving tabindex: exactly one tab in the strip is reachable with the Tab key, and the
    /// arrows move between them from there. In Manual mode that is the focused tab, which can differ
    /// from the selected one.</summary>
    internal int TabIndexOf(string? value) =>
        value is not null && value == (_focusedValue ?? ActiveValue) ? 0 : -1;

    internal void Register(AtomTab tab)
    {
        if (!_tabs.Contains(tab)) _tabs.Add(tab);
    }

    internal void Unregister(AtomTab tab)
    {
        _tabs.Remove(tab);
        if (_focusedValue == tab.Value) _focusedValue = null;
    }

    /// <summary>Selects a tab. No-ops when it is disabled or already selected, so a repeat click
    /// raises no event.</summary>
    internal async Task SelectAsync(string? value)
    {
        if (value is null) return;

        var tab = _tabs.FirstOrDefault(t => t.Value == value);
        if (tab is null || tab.Disabled) return;

        _focusedValue = value;

        if (value == ActiveValue)
        {
            // Still re-render: in Manual mode the roving tabindex may have moved even though the
            // selection did not.
            NotifyChildren();
            return;
        }

        Value = value;
        await ValueChanged.InvokeAsync(value);
        NotifyChildren();
    }

    /// <summary>
    /// Arrow/Home/End navigation per the ARIA tabs pattern.
    /// </summary>
    /// <remarks>
    /// <para>Which arrows navigate follows <see cref="Orientation"/>, matching what
    /// <c>aria-orientation</c> advertises. Both axes' keys are accepted regardless, because a user who
    /// presses Right in a vertical strip means "next" and refusing is not helpful.</para>
    /// <para><b>Default scrolling is not suppressed.</b> Blazor's <c>:preventDefault</c> is decided at
    /// render time, not per event, so it could only be applied to *every* keydown on the strip — which
    /// would also swallow Tab and trap focus inside the tablist. An arrow key may therefore also scroll a
    /// scrollable ancestor; in practice that is visible mainly in
    /// <see cref="TabsOrientation.Vertical"/>, whose Up/Down keys usually have somewhere to scroll.
    /// Suppressing it selectively would need a JS module, which this package deliberately does not
    /// ship — see DEVELOPMENT.md.</para>
    /// </remarks>
    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        // A modified keypress is a browser/OS shortcut (Ctrl+Home scrolls to the top of the page), not
        // tab navigation. atom-tabs.js applies the same test before cancelling a default action — the
        // two must agree, or the guard would suppress a scroll this handler declined to replace.
        if (e.CtrlKey || e.MetaKey || e.AltKey || e.ShiftKey) return;

        var enabled = _tabs.Where(t => !t.Disabled).ToList();
        if (enabled.Count == 0) return;

        var current = _focusedValue ?? ActiveValue;
        var index = enabled.FindIndex(t => t.Value == current);
        if (index < 0) index = 0;

        var vertical = Orientation == TabsOrientation.Vertical;
        int target;

        switch (e.Key)
        {
            case "ArrowRight" when !vertical:
            case "ArrowDown" when vertical:
                target = (index + 1) % enabled.Count;
                break;

            case "ArrowLeft" when !vertical:
            case "ArrowUp" when vertical:
                target = (index - 1 + enabled.Count) % enabled.Count;
                break;

            case "Home":
                target = 0;
                break;

            case "End":
                target = enabled.Count - 1;
                break;

            default:
                // Enter/Space need no handling: the tabs are real <button>s, so the browser turns
                // those into a click, which selects.
                return;
        }

        var tab = enabled[target];

        if (ActivationMode == TabsActivation.Automatic)
        {
            await SelectAsync(tab.Value);
        }
        else
        {
            _focusedValue = tab.Value;
            NotifyChildren();
        }

        await tab.FocusAsync();
    }

    /// <summary>
    /// Re-renders the registered tabs after the selection or roving focus moves.
    /// </summary>
    /// <remarks>
    /// Necessary because the cascade is <c>IsFixed</c> and its value (<c>this</c>) never changes
    /// reference — a <c>CascadingValue</c> only notifies subscribers when the value itself changes, so
    /// subscribing would never fire here. Panels are covered by this component's own re-render, since
    /// they read nothing but <see cref="ActiveValue"/> at render time.
    /// </remarks>
    private void NotifyChildren()
    {
        foreach (var tab in _tabs) tab.NotifyStateChanged();
        StateHasChanged();
    }

    /// <summary>
    /// Attaches the key guard once the tablist element exists.
    /// </summary>
    /// <remarks>
    /// <para>Only the default-action suppression lives in JS; the navigation itself is the C#
    /// <c>@onkeydown</c> handler, which works with or without this module. So a failed import degrades
    /// to exactly the behavior the component had before the module existed — arrows still navigate, they
    /// just also scroll — which is why every failure mode here is swallowed rather than surfaced.</para>
    /// <para>Attach runs once. The guard reads the axis off <c>aria-orientation</c> at event time, so
    /// changing <see cref="Orientation"/> at runtime needs no re-attach.</para>
    /// </remarks>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            await _module.InvokeVoidAsync("attach", _listRef);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("detach", _listRef);
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
        finally
        {
            _module = null;
        }
    }

    private string VariantAttr => Kebab(Variant.ToString());

    private string SizeAttr => Kebab(Size.ToString());

    private string AlignAttr => Kebab(Align.ToString());

    /// <summary>Lowercase, because it doubles as the <c>aria-orientation</c> value, which the ARIA
    /// spec defines as <c>horizontal</c>/<c>vertical</c>.</summary>
    private string OrientationAttr => Orientation.ToString().ToLowerInvariant();

    private string? EffectAttr => Effect == TabsEffect.None ? null : Kebab(Effect.ToString());

    private string? ScrollableAttr => Scrollable ? "true" : null;

    private string? RootStyle
    {
        get
        {
            var vars = new StyleVars("tabs")
                .Add("accent", AccentColor)
                .Add("tab-color", TabColor)
                .Add("active-tab-color", ActiveTabColor)
                .Add("indicator-color", IndicatorColor)
                .Add("indicator-thickness", IndicatorThickness)
                .Add("border-color", BorderColor)
                .Add("panel-bg", PanelBackgroundColor)
                .Add("radius", Radius)
                .Add("panel-padding", PanelPadding)
                .Add("gap", Gap)
                .Add("font-size", FontSize)
                .Add("duration", Duration is null
                    ? null
                    : Duration.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "s")
                .ToString();

            var s = (Visible ? "" : "display:none;") + vars;
            return string.IsNullOrEmpty(s) ? null : s;
        }
    }

    /// <summary>Makes a tab's <c>Value</c> safe to embed in an id. Values are author-supplied keys and
    /// may contain spaces or punctuation; an id with a space in it breaks the
    /// <c>aria-controls</c> reference outright.</summary>
    internal static string Slug(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "none";

        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
            sb.Append(char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-');
        return sb.ToString();
    }

    /// <summary>PascalCase enum name → kebab-case attribute value (<c>FadePanel</c> →
    /// <c>fade-panel</c>).</summary>
    internal static string Kebab(string pascal)
    {
        var sb = new System.Text.StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            var c = pascal[i];
            if (char.IsUpper(c) && i > 0) sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
