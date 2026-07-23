using System.Globalization;
using BlazorAtoms.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Layout;

/// <summary>
/// An overlay drawer panel that opens from any viewport edge with configurable transitions,
/// sizing, and styling. Supports a close button, backdrop click to close, header/footer slots,
/// and child content. While open it behaves as a modal: it traps focus, closes on Escape, and
/// locks body scroll (each toggleable). Designed for viewport anchoring with upgrade path to
/// container anchoring.
/// </summary>
public partial class AtomDrawer : AtomComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorAtoms.Layout/atom-layout.js";

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _panelRef;
    private IJSObjectReference? _module;
    private DotNetObjectReference<AtomDrawer>? _selfRef;
    private bool _jsActivated;
    /// <summary>Controls whether the drawer is visible.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Callback invoked when <see cref="Open"/> changes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Which viewport edge the drawer opens from.</summary>
    [Parameter]
    public AtomDrawerPosition Position { get; set; } = AtomDrawerPosition.Left;

    /// <summary>Enter animation style.</summary>
    [Parameter]
    public AtomDrawerTransition Transition { get; set; } = AtomDrawerTransition.Slide;

    /// <summary>Animation duration in milliseconds.</summary>
    [Parameter]
    public int AnimationDuration { get; set; } = 240;

    /// <summary>Drawer width. Any valid CSS width value, or a raw number that is interpreted as pixels.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>Drawer height. Any valid CSS height value, or a raw number that is interpreted as pixels.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>Minimum width constraint. Any valid CSS width value, or a raw number that is interpreted as pixels.</summary>
    [Parameter]
    public string? MinWidth { get; set; }

    /// <summary>Minimum height constraint. Any valid CSS height value, or a raw number that is interpreted as pixels.</summary>
    [Parameter]
    public string? MinHeight { get; set; }

    /// <summary>Maximum width constraint. Any valid CSS width value, or a raw number that is interpreted as pixels.</summary>
    [Parameter]
    public string? MaxWidth { get; set; }

    /// <summary>Maximum height constraint. Any valid CSS height value, or a raw number that is interpreted as pixels.</summary>
    [Parameter]
    public string? MaxHeight { get; set; }

    /// <summary>Border width in pixels.</summary>
    [Parameter]
    public double? BorderWidth { get; set; }

    /// <summary>Border color. Any valid CSS color value.</summary>
    [Parameter]
    public string? BorderColor { get; set; }

    /// <summary>Corner radius in pixels.</summary>
    [Parameter]
    public double? BorderRadius { get; set; }

    /// <summary>Drawer background color. Any valid CSS color value.</summary>
    [Parameter]
    public string? BackgroundColor { get; set; }

    /// <summary>Backdrop overlay color. Any valid CSS color value.</summary>
    [Parameter]
    public string? BackdropColor { get; set; } = "rgba(0,0,0,0.5)";

    /// <summary>Renders a drop shadow behind the drawer panel for a floating appearance. Off by
    /// default (drawers are typically flush against the viewport edge).</summary>
    [Parameter]
    public bool ShowShadow { get; set; }

    /// <summary>Shadow color. Any valid CSS color value. Default <c>rgba(0,0,0,0.25)</c>.</summary>
    [Parameter]
    public string? ShadowColor { get; set; } = "rgba(0,0,0,0.25)";

    /// <summary>Shadow blur radius in pixels. Default 16.</summary>
    [Parameter]
    public double ShadowBlur { get; set; } = 16;

    /// <summary>Shadow spread radius in pixels. Default 0.</summary>
    [Parameter]
    public double ShadowSpread { get; set; }

    /// <summary>Horizontal shadow offset in pixels. Positive = right. Null (default) auto-biases
    /// away from the pinned edge — e.g. a <see cref="AtomDrawerPosition.Right"/> drawer sits flush
    /// against the viewport's right edge, so a shadow centered there wastes half its reach
    /// off-screen; the default instead casts it entirely toward the visible left side.</summary>
    [Parameter]
    public double? ShadowOffsetX { get; set; }

    /// <summary>Vertical shadow offset in pixels. Positive = down. Null (default) auto-biases away
    /// from the pinned edge, the same way <see cref="ShadowOffsetX"/> does for horizontal
    /// positions.</summary>
    [Parameter]
    public double? ShadowOffsetY { get; set; }

    // Effective offsets used for rendering: an explicit value always wins; otherwise bias the
    // shadow toward the panel's one edge that can actually be seen (Left/Right positions default
    // to full viewport height and Top/Bottom to full viewport width, so three of the four edges
    // sit flush against the screen and any shadow reach there is invisible).
    private double EffectiveShadowOffsetX => ShadowOffsetX ?? Position switch
    {
        AtomDrawerPosition.Left => ShadowBlur,
        AtomDrawerPosition.Right => -ShadowBlur,
        _ => 0,
    };

    private double EffectiveShadowOffsetY => ShadowOffsetY ?? Position switch
    {
        AtomDrawerPosition.Top => ShadowBlur,
        AtomDrawerPosition.Bottom => -ShadowBlur,
        _ => 0,
    };

    /// <summary>Renders the backdrop overlay.</summary>
    [Parameter]
    public bool ShowBackdrop { get; set; } = true;

    /// <summary>Renders the close button in the upper right corner of the drawer.</summary>
    [Parameter]
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Closes the drawer when the backdrop is clicked.</summary>
    [Parameter]
    public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>Closes the drawer when the Escape key is pressed while it has focus.</summary>
    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>Keeps keyboard focus cycling within the drawer while open (modal focus trap).</summary>
    [Parameter]
    public bool TrapFocus { get; set; } = true;

    /// <summary>Locks <c>document.body</c> scrolling while the drawer is open.</summary>
    [Parameter]
    public bool LockScroll { get; set; } = true;

    /// <summary>Z-index for the drawer panel. Backdrop renders one level below.</summary>
    [Parameter]
    public int? ZIndex { get; set; }

    /// <summary>Optional header content rendered above the body.</summary>
    [Parameter]
    public RenderFragment? HeaderContent { get; set; }

    /// <summary>Main drawer content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Optional footer content rendered below the body.</summary>
    [Parameter]
    public RenderFragment? FooterContent { get; set; }

    /// <summary>Fired after the drawer opens.</summary>
    [Parameter]
    public EventCallback OnOpen { get; set; }

    /// <summary>Fired after the drawer closes.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    // Enter/exit animation state. The panel must be mounted in its *hidden* state for one frame
    // before the `atom-drawer-open` class is added, otherwise the CSS transition has no start
    // state to animate from and the drawer just snaps open. Likewise on close we keep it mounted
    // for the animation duration so the exit transition can play before it leaves the DOM.
    private bool _prevOpen;   // last Open value we reacted to — distinguishes real transitions
    private bool _visible;    // present in the DOM (true through the whole exit animation)
    private bool _entered;    // `atom-drawer-open` applied (drives the transition to the open state)
    private int _closeGeneration; // guards the delayed unmount against a re-open mid-animation

    private bool IsHorizontal => Position is AtomDrawerPosition.Left or AtomDrawerPosition.Right;
    private bool IsVertical => Position is AtomDrawerPosition.Top or AtomDrawerPosition.Bottom;

    private string EffectiveWidth => NormalizeLength(Width) ?? (IsVertical ? "100vw" : "280px");
    private string EffectiveHeight => NormalizeLength(Height) ?? (IsHorizontal ? "100vh" : "240px");

    private static string? NormalizeLength(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            return $"{trimmed}px";
        }

        return trimmed;
    }

    // Composed box-shadow value, or null when ShowShadow is off (the CSS falls back to `none`).
    // A single CSS property can't be expressed as separate custom-property pieces the way
    // border-width/border-color/border-radius are, so this builds the full value in C#.
    private string? EffectiveBoxShadow => ShowShadow
        ? string.Create(CultureInfo.InvariantCulture,
            $"{EffectiveShadowOffsetX}px {EffectiveShadowOffsetY}px {ShadowBlur}px {ShadowSpread}px {ShadowColor ?? "rgba(0,0,0,0.25)"}")
        : null;

    // How far box-shadow visually reaches beyond the border box on each side — the standard CSS
    // shadow-extent formula (blur+spread, biased by the offset toward/away from that side), clamped
    // to zero. The Grow transition animates via clip-path, and clip-path clips everything outside
    // its shape INCLUDING box-shadow (unlike overflow, which only affects children) — so its inset()
    // must be pulled back by these amounts on every non-animating edge, or the shadow gets clipped
    // exactly at the border box (visible mid-animation, since clip-path can't interpolate from
    // `none`, then snapped away the instant the transition finishes).
    private double ShadowReachTop => ShowShadow ? Math.Max(0, ShadowBlur + ShadowSpread - EffectiveShadowOffsetY) : 0;
    private double ShadowReachBottom => ShowShadow ? Math.Max(0, ShadowBlur + ShadowSpread + EffectiveShadowOffsetY) : 0;
    private double ShadowReachLeft => ShowShadow ? Math.Max(0, ShadowBlur + ShadowSpread - EffectiveShadowOffsetX) : 0;
    private double ShadowReachRight => ShowShadow ? Math.Max(0, ShadowBlur + ShadowSpread + EffectiveShadowOffsetX) : 0;

    private string GetDrawerStyle()
    {
        var vars = new StyleVars("atom-drawer")
            .Add("width", EffectiveWidth)
            .Add("height", EffectiveHeight)
            .Add("min-width", NormalizeLength(MinWidth))
            .Add("min-height", NormalizeLength(MinHeight))
            .Add("max-width", NormalizeLength(MaxWidth))
            .Add("max-height", NormalizeLength(MaxHeight))
            .Add("border-width", BorderWidth)
            .Add("border-color", BorderColor)
            .Add("border-radius", BorderRadius)
            .Add("bg", BackgroundColor)
            .Add("backdrop-color", BackdropColor)
            .Add("shadow", EffectiveBoxShadow)
            .Add("shadow-top", ShowShadow ? ShadowReachTop : (double?)null)
            .Add("shadow-right", ShowShadow ? ShadowReachRight : (double?)null)
            .Add("shadow-bottom", ShowShadow ? ShadowReachBottom : (double?)null)
            .Add("shadow-left", ShowShadow ? ShadowReachLeft : (double?)null)
            .Add("duration", $"{AnimationDuration}ms")
            .Add("z-drawer", ZIndex?.ToString(CultureInfo.InvariantCulture) ?? "1000")
            .Add("z-backdrop", ZIndex.HasValue ? (ZIndex.Value - 1).ToString(CultureInfo.InvariantCulture) : "999");

        return vars.ToString();
    }

    private string DrawerClasses =>
        $"atom-drawer atom-drawer-{Position.ToString().ToLowerInvariant()} atom-drawer-{Transition.ToString().ToLowerInvariant()}";

    // Base classes plus the open-state class once the enter frame has run (see OnAfterRender).
    private string OpenAwareClasses => _entered ? $"{DrawerClasses} atom-drawer-open" : DrawerClasses;

    // Close driven from inside the drawer (close button / backdrop). Flips Open, notifies the
    // parent binding, and runs the same close transition as an external Open=false.
    private async Task CloseAsync()
    {
        if (!_visible)
        {
            return;
        }

        Open = false;
        await BeginCloseAsync();               // sets _prevOpen=false before we notify the parent
        await OpenChanged.InvokeAsync(false);  // parent echo re-renders with Open=false → no double transition
    }

    private async Task OnBackdropClickAsync()
    {
        if (CloseOnBackdropClick)
        {
            await CloseAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // React only to a real open/closed *transition*, not to every parameter change — otherwise
        // OnOpen would fire on every keystroke while the drawer is open.
        if (Open && !_prevOpen)
        {
            await BeginOpenAsync();
        }
        else if (!Open && _prevOpen)
        {
            await BeginCloseAsync();
        }
    }

    private async Task BeginOpenAsync()
    {
        _prevOpen = true;
        _closeGeneration++;   // cancel any pending unmount from a prior close
        _visible = true;
        _entered = false;     // mount hidden; OnAfterRender adds the open class one frame later
        StateHasChanged();
        await OnOpen.InvokeAsync();
    }

    private async Task BeginCloseAsync()
    {
        _prevOpen = false;
        _entered = false;     // drop the open class → the exit transition plays
        StateHasChanged();
        await OnClose.InvokeAsync();

        // Keep the panel mounted for the animation, then remove it — but DON'T await this on the
        // close path, or the parent's OpenChanged notification would be delayed a whole animation.
        _ = ScheduleUnmountAsync(++_closeGeneration);
    }

    private async Task ScheduleUnmountAsync(int generation)
    {
        var duration = AnimationDuration > 0 ? AnimationDuration : 0;
        try
        {
            await Task.Delay(duration);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        // Only unmount if no re-open happened meanwhile (generation bumped) and we're still closed.
        if (generation == _closeGeneration && !Open)
        {
            _visible = false;
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Add the open class the render *after* the panel first mounts hidden, so the CSS
        // transition animates from the hidden start state to the open state instead of snapping.
        // A real painted frame must sit between the two renders, else Blazor coalesces them into a
        // single paint and the drawer snaps open — so wait a double-rAF via JS before flipping.
        if (_visible && Open && !_entered)
        {
            await WaitForFrameAsync();
            _entered = true;
            StateHasChanged();
            return; // let the open-class render flush before wiring the modal behaviour
        }

        // Engage / release the modal behaviour (focus trap, Escape, scroll lock) via JS.
        if (_visible && Open && _entered && !_jsActivated)
        {
            await ActivateModalAsync();
        }
        else if (_jsActivated && (!Open || !_visible))
        {
            await DeactivateModalAsync();
        }
    }

    // Called from JS (atom-layout.js) when Escape is pressed inside the drawer.
    /// <summary>Internal: invoked by the drawer's JS module to close on Escape. Not for consumer use.</summary>
    [JSInvokable]
    public Task CloseFromJsAsync() => CloseAsync();

    private async ValueTask<IJSObjectReference?> EnsureModuleAsync()
    {
        try
        {
            return _module ??= await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
        }
        catch (JSDisconnectedException) { return null; }
        catch (OperationCanceledException) { return null; }
        catch (InvalidOperationException) { return null; } // SSR / prerender — no JS yet
    }

    // Wait one painted frame so the CSS enter transition has a hidden start state. Best-effort:
    // if JS isn't available (prerender) the drawer just appears without the enter animation.
    private async Task WaitForFrameAsync()
    {
        var module = await EnsureModuleAsync();
        if (module is null)
        {
            return;
        }

        try
        {
            await module.InvokeVoidAsync("nextFrame");
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
        catch (InvalidOperationException) { }
    }

    private async Task ActivateModalAsync()
    {
        // Nothing to trap/lock if all three modal behaviours are off.
        if (!CloseOnEscape && !TrapFocus && !LockScroll)
        {
            return;
        }

        var module = await EnsureModuleAsync();
        if (module is null)
        {
            return;
        }

        try
        {
            _selfRef ??= DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("activate", _panelRef, _selfRef,
                new { closeOnEscape = CloseOnEscape, trapFocus = TrapFocus, lockScroll = LockScroll });
            _jsActivated = true;
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { /* prerender / DOM race — non-fatal */ }
        catch (InvalidOperationException) { /* SSR / prerender — no JS yet */ }
    }

    private async Task DeactivateModalAsync()
    {
        _jsActivated = false;
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("deactivate", _panelRef);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
        catch (InvalidOperationException) { }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is { } module)
        {
            // Release the listener / scroll lock before the DotNetObjectReference goes away, so a
            // late Escape keydown can't fire into a disposed reference (same discipline as
            // AtomScrollTo's teardown).
            try
            {
                await module.InvokeVoidAsync("deactivate", _panelRef);
            }
            catch (JSDisconnectedException) { }
            catch (OperationCanceledException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }

            try
            {
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
            catch (OperationCanceledException) { }

            _module = null;
        }

        _selfRef?.Dispose();
        _selfRef = null;
        GC.SuppressFinalize(this);
    }
}
