using System.Globalization;
using System.Text;
using BlazorAtoms.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Navigation;

/// <summary>
/// A scroll-to button. By default it renders an SVG chevron and, on click, smoothly scrolls the
/// page (or the nearest scrollable ancestor) to the top or bottom. Point it at a named anchor or
/// element id via <see cref="Target"/> to jump there instead. Supply <see cref="ChildContent"/> to
/// replace the default arrow with any icon/graphic. Set <see cref="VisibleAfter"/> to auto-hide the
/// button until the user has scrolled past that many pixels (the classic "back to top" affordance).
/// </summary>
public partial class AtomScrollTo : AtomComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorAtoms.Navigation/atom-navigation.js";

    private ElementReference _rootRef;
    private IJSObjectReference? _module;
    private DotNetObjectReference<AtomScrollTo>? _selfRef;
    private bool _watching;
    private bool _collisionWatching;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ---- target -------------------------------------------------------------------------------

    /// <summary>Anchor name, element id, or CSS selector to scroll to. When set, wins over
    /// <see cref="Direction"/> — the button jumps to that element via <c>scrollIntoView</c>.
    /// Accepts a bare id/anchor name (<c>"section-3"</c>), an id selector (<c>"#section-3"</c>),
    /// or any CSS selector.</summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>Which end to scroll to when <see cref="Target"/> is not set. Also picks the default
    /// arrow glyph. Default <see cref="ScrollDirection.Up"/> (scroll to top).</summary>
    [Parameter] public ScrollDirection Direction { get; set; } = ScrollDirection.Up;

    /// <summary>Whether to scroll the whole page or the nearest scrollable ancestor of the button.
    /// Default <see cref="ScrollScope.Page"/>. Ignored when <see cref="ScrollContainer"/> is set
    /// (that names the scroller explicitly).</summary>
    [Parameter] public ScrollScope Scope { get; set; } = ScrollScope.Page;

    /// <summary>CSS selector naming the scrollable element to act on (scroll + auto-hide watch).
    /// When set, the button can live anywhere — e.g. an overlay sibling of the scroll box — and no
    /// longer relies on <see cref="Scope"/>'s ancestor-walk to find its scroller. Unset = the
    /// ancestor-walk behaviour driven by <see cref="Scope"/>.</summary>
    [Parameter] public string? ScrollContainer { get; set; }

    /// <summary>Scroll animation. Default <see cref="ScrollMotion.Smooth"/>.</summary>
    [Parameter] public ScrollMotion Motion { get; set; } = ScrollMotion.Smooth;

    // ---- appearance ---------------------------------------------------------------------------

    /// <summary>Custom icon/graphic. When null, a default SVG chevron (matching <see cref="Direction"/>)
    /// renders.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Tooltip text — rendered as the <c>title</c> attribute and, when set, the
    /// <c>aria-label</c>. E.g. "Scroll to top", "Back to top", "Jump to bottom".</summary>
    [Parameter] public string? Tooltip { get; set; }

    /// <summary>Icon/foreground color. Maps to CSS custom property <c>--scrollto-color</c>.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Button background. Maps to <c>--scrollto-bg</c>.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Button diameter (any CSS length). Maps to <c>--scrollto-size</c>.</summary>
    [Parameter] public string? Size { get; set; }

    /// <summary>Corner radius (any CSS length). Maps to <c>--scrollto-radius</c>. Default is a circle.</summary>
    [Parameter] public string? Radius { get; set; }

    /// <summary>Pins the button to a viewport corner (<c>Fixed*</c>) or a positioned-ancestor corner
    /// (<c>Absolute*</c>) with no consumer CSS. Default <see cref="ScrollPosition.Inline"/> — the
    /// button flows in place and you position it via <c>Style</c>/<c>CssClass</c> if you want.</summary>
    [Parameter] public ScrollPosition Position { get; set; } = ScrollPosition.Inline;

    /// <summary>Vertical distance from the pinned top/bottom edge (any CSS length — <c>px</c>,
    /// <c>rem</c>, <c>%</c>, <c>vh</c>, <c>calc()</c>/<c>clamp()</c>/<c>env()</c>, …) for a
    /// <c>Fixed*</c>/<c>Absolute*</c> <see cref="Position"/>. Maps to <c>--scrollto-offset-v</c>.
    /// Default <c>1.5rem</c>.</summary>
    [Parameter] public string? OffsetV { get; set; }

    /// <summary>Horizontal distance from the pinned left/right edge (any CSS length) for a
    /// <c>Fixed*</c>/<c>Absolute*</c> <see cref="Position"/>. Ignored for <c>*Center</c> positions
    /// (those center via <c>translate</c>). Maps to <c>--scrollto-offset-h</c>. Default <c>1.5rem</c>.</summary>
    [Parameter] public string? OffsetH { get; set; }

    /// <summary>Default-arrow stroke width in SVG user units. Ignored when <see cref="ChildContent"/>
    /// is supplied. Default 2.</summary>
    [Parameter] public double ArrowStrokeWidth { get; set; } = 2;

    // ---- behavior -----------------------------------------------------------------------------

    /// <summary>When set, the button stays hidden until the scroll position passes this many pixels,
    /// then fades in — the classic auto-appearing "back to top" button. Null keeps it always visible.
    /// Uses a passive, rAF-coalesced scroll watcher.</summary>
    [Parameter] public int? VisibleAfter { get; set; }

    /// <summary>CSS selector of an element the button must not cover (a footer, a call-to-action,
    /// end-of-content). While any matching element is visible in the scroller, the button fades out
    /// and returns when it leaves — an <c>IntersectionObserver</c> drives it. Combines with
    /// <see cref="VisibleAfter"/> (both must allow it for the button to show).</summary>
    [Parameter] public string? HideNear { get; set; }

    /// <summary>Fires after a scroll is triggered by a click.</summary>
    [Parameter] public EventCallback OnScrolled { get; set; }

    /// <summary>Fires when the auto-hide visibility state flips (only when <see cref="VisibleAfter"/>
    /// is set). <c>true</c> = now visible.</summary>
    [Parameter] public EventCallback<bool> OnVisibilityChanged { get; set; }

    // ---- render helpers -----------------------------------------------------------------------

    private static string RootClass => "atom-scroll-to";

    private string DefaultAriaLabel => Direction == ScrollDirection.Up ? "Scroll to top" : "Scroll to bottom";

    private string? RootStyle
    {
        get
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Color)) sb.Append($"--scrollto-color:{Color};");
            if (!string.IsNullOrEmpty(Background)) sb.Append($"--scrollto-bg:{Background};");
            if (!string.IsNullOrEmpty(Size)) sb.Append($"--scrollto-size:{Size};");
            if (!string.IsNullOrEmpty(Radius)) sb.Append($"--scrollto-radius:{Radius};");
            if (!string.IsNullOrEmpty(OffsetV)) sb.Append($"--scrollto-offset-v:{OffsetV};");
            if (!string.IsNullOrEmpty(OffsetH)) sb.Append($"--scrollto-offset-h:{OffsetH};");
            return sb.Length == 0 ? null : sb.ToString();
        }
    }

    // Kebab token consumed by the scoped-CSS attribute selectors (e.g. "fixed-bottom-right").
    // Inline → null so no attribute renders and the button keeps its default in-flow position.
    private string? PositionAttr => Position switch
    {
        ScrollPosition.Inline => null,
        ScrollPosition.FixedBottomRight => "fixed-bottom-right",
        ScrollPosition.FixedBottomLeft => "fixed-bottom-left",
        ScrollPosition.FixedTopRight => "fixed-top-right",
        ScrollPosition.FixedTopLeft => "fixed-top-left",
        ScrollPosition.FixedBottomCenter => "fixed-bottom-center",
        ScrollPosition.FixedTopCenter => "fixed-top-center",
        ScrollPosition.AbsoluteBottomRight => "absolute-bottom-right",
        ScrollPosition.AbsoluteBottomLeft => "absolute-bottom-left",
        ScrollPosition.AbsoluteTopRight => "absolute-top-right",
        ScrollPosition.AbsoluteTopLeft => "absolute-top-left",
        _ => null,
    };

    private string DefaultArrowSvg
    {
        get
        {
            var sw = ArrowStrokeWidth.ToString("0.###", CultureInfo.InvariantCulture);
            // Chevron: up = "∧", down = "∨". Drawn in a 24×24 box.
            var d = Direction == ScrollDirection.Up ? "M6 15l6-6 6 6" : "M6 9l6 6 6-6";
            return $"<svg class=\"atom-scroll-to-arrow\" viewBox=\"0 0 24 24\" width=\"60%\" height=\"60%\" " +
                   $"fill=\"none\" stroke=\"currentColor\" stroke-width=\"{sw}\" stroke-linecap=\"round\" " +
                   $"stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"{d}\"/></svg>";
        }
    }

    // ---- interop --------------------------------------------------------------------------------

    private async Task ScrollAsync()
    {
        var mode = !string.IsNullOrEmpty(Target)
            ? "selector"
            : (Direction == ScrollDirection.Up ? "top" : "bottom");
        var scope = Scope == ScrollScope.Container ? "container" : "page";
        var motion = Motion == ScrollMotion.Auto ? "auto" : "smooth";

        try
        {
            var module = await LoadModuleAsync();
            if (module is null) return;
            await module.InvokeAsync<bool>("scrollToTarget", _rootRef, mode, Target, scope, motion, ScrollContainer);
            if (OnScrolled.HasDelegate) await OnScrolled.InvokeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { /* selector not found / DOM race — non-fatal */ }
        catch (InvalidOperationException) { /* SSR / prerender — no JS yet */ }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var needVisibility = VisibleAfter is not null && !_watching;
        var needCollision = !string.IsNullOrEmpty(HideNear) && !_collisionWatching;
        if (!needVisibility && !needCollision) return;

        var scope = Scope == ScrollScope.Container ? "container" : "page";
        try
        {
            var module = await LoadModuleAsync();
            if (module is null) return;

            if (needVisibility)
            {
                _selfRef ??= DotNetObjectReference.Create(this);
                await module.InvokeVoidAsync("watchVisibility", _rootRef, _selfRef, VisibleAfter!.Value, scope, ScrollContainer);
                _watching = true;
            }
            if (needCollision)
            {
                await module.InvokeVoidAsync("watchCollision", _rootRef, HideNear, scope, ScrollContainer);
                _collisionWatching = true;
            }
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { /* SSR / prerender */ }
    }

    [JSInvokable]
    public async Task OnVisibilityChangedInternal(bool visible)
    {
        if (OnVisibilityChanged.HasDelegate)
            await OnVisibilityChanged.InvokeAsync(visible);
    }

    private async ValueTask<IJSObjectReference?> LoadModuleAsync()
    {
        try
        {
            return _module ??= await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
        }
        catch (JSDisconnectedException) { return null; }
        catch (OperationCanceledException) { return null; }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                if (_watching) await _module.InvokeVoidAsync("unwatch", _rootRef);
                if (_collisionWatching) await _module.InvokeVoidAsync("unwatchCollision", _rootRef);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            _module = null;
        }
        _selfRef?.Dispose();
        _selfRef = null;
        GC.SuppressFinalize(this);
    }
}
