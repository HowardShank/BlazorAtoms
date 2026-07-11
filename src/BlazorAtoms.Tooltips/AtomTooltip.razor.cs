using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Tooltips;

/// <summary>
/// A hover/keyboard-focus tooltip bubble anchored to arbitrary trigger content. Positioning and
/// show/hide are pure CSS (<c>position:absolute</c> + <c>:hover</c>/<c>:focus-within</c>) for
/// every placement <em>except</em> <see cref="Placement.Cursor"/>, which follows the pointer via
/// a tiny JS module the component loads itself. No JS is loaded unless Cursor placement is used.
/// </summary>
public partial class AtomTooltip : AtomComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The element(s) the tooltip is attached to. Required.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Simple text content for the bubble. Ignored when <see cref="TooltipContent"/> is set.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Rich content for the bubble. Takes priority over <see cref="Text"/> when set.</summary>
    [Parameter] public RenderFragment? TooltipContent { get; set; }

    /// <summary>Side, corner, or cursor mode the bubble is placed by, relative to the trigger.</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.Top;

    /// <summary>Outline shape of the bubble. <see cref="TooltipShape.Burst"/> and
    /// <see cref="TooltipShape.FoldedCorner"/> are fill-only (clip-path removes border + arrow).</summary>
    [Parameter] public TooltipShape Shape { get; set; } = TooltipShape.Rectangle;

    /// <summary>Bubble background. Falls back to the built-in light/dark default when null. Sets <c>--tip-bg</c>.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Bubble text color. Falls back to the built-in default when null. Sets <c>--tip-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Bubble border color. Falls back to the built-in default when null. Sets <c>--tip-border</c>.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Bubble border width in px. Sets <c>--tip-border-width</c>.</summary>
    [Parameter] public double? BorderWidth { get; set; }

    /// <summary>Bubble corner radius in px. Sets <c>--tip-radius</c>.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Arrow size in px. Sets <c>--tip-arrow-size</c>. Ignored when <see cref="ShowArrow"/> is false.</summary>
    [Parameter] public double? ArrowSize { get; set; }

    /// <summary>Bubble max-width (any CSS length, e.g. "16rem"). Sets <c>--tip-max-width</c>.</summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>Gap between the trigger and the bubble in px. Sets <c>--tip-offset</c>.</summary>
    [Parameter] public double? Offset { get; set; }

    /// <summary>Draw the attachment arrow. Ignored in <see cref="Placement.Cursor"/> mode (no fixed edge to point from).</summary>
    [Parameter] public bool ShowArrow { get; set; } = true;

    /// <summary>When true, the bubble never renders (trigger content still renders normally).</summary>
    [Parameter] public bool Disabled { get; set; }


    // Unique per instance so multiple tooltips on a page never collide on ids.
    private readonly string _id = "tt" + Guid.NewGuid().ToString("N")[..8];

    private ElementReference _triggerRef;
    private ElementReference _bubbleRef;

    // JS is loaded only for Cursor placement, once, and torn down cleanly on dispose.
    private IJSObjectReference? _module;
    private bool _cursorActive;

    // Clip-path shapes clip the arrow away; Cursor has no fixed edge. Thought keeps the arrow
    // element but restyles it into the circle trail (see the CSS).
    private bool ShowsArrow => ShowArrow
        && Placement != Placement.Cursor
        && Shape != TooltipShape.Burst
        && Shape != TooltipShape.FoldedCorner;

    private string ShapeValue => Shape switch
    {
        TooltipShape.Rectangle => "rectangle",
        TooltipShape.Pill => "pill",
        TooltipShape.Ellipse => "ellipse",
        TooltipShape.Thought => "thought",
        TooltipShape.Burst => "burst",
        TooltipShape.FoldedCorner => "folded",
        _ => "rectangle",
    };

    private string PlacementValue => Placement switch
    {
        Placement.Top => "top",
        Placement.TopStart => "top-start",
        Placement.TopEnd => "top-end",
        Placement.Bottom => "bottom",
        Placement.BottomStart => "bottom-start",
        Placement.BottomEnd => "bottom-end",
        Placement.Left => "left",
        Placement.LeftStart => "left-start",
        Placement.LeftEnd => "left-end",
        Placement.Right => "right",
        Placement.RightStart => "right-start",
        Placement.RightEnd => "right-end",
        Placement.TopLeft => "top-left",
        Placement.TopRight => "top-right",
        Placement.BottomLeft => "bottom-left",
        Placement.BottomRight => "bottom-right",
        Placement.Cursor => "cursor",
        _ => "top",
    };

    private static string F(double v) => v.ToString(CultureInfo.InvariantCulture);

    private string RootStyle
    {
        get
        {
            var style = string.Concat(
                Background is null ? "" : $"--tip-bg:{Background};",
                TextColor is null ? "" : $"--tip-color:{TextColor};",
                BorderColor is null ? "" : $"--tip-border:{BorderColor};",
                BorderWidth is null ? "" : $"--tip-border-width:{F(BorderWidth.Value)}px;",
                Radius is null ? "" : $"--tip-radius:{F(Radius.Value)}px;",
                ArrowSize is null ? "" : $"--tip-arrow-size:{F(ArrowSize.Value)}px;",
                MaxWidth is null ? "" : $"--tip-max-width:{MaxWidth};",
                Offset is null ? "" : $"--tip-offset:{F(Offset.Value)}px;");
            return style;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Only Cursor placement needs JS. Everything else is pure CSS — no interop here at all.
        var wantCursor = Placement == Placement.Cursor && !Disabled;

        if (wantCursor && !_cursorActive)
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorAtoms.Tooltips/atom-tooltip.js");
            await _module.InvokeVoidAsync("attach", _triggerRef, _bubbleRef);
            _cursorActive = true;
        }
        else if (!wantCursor && _cursorActive)
        {
            if (_module is not null) await _module.InvokeVoidAsync("detach", _triggerRef);
            _cursorActive = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                if (_cursorActive) await _module.InvokeVoidAsync("detach", _triggerRef);
                await _module.DisposeAsync();
            }
        }
        // Circuit already gone / prerender teardown — nothing to clean up on the JS side.
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        finally
        {
            _module = null;
            _cursorActive = false;
        }

        GC.SuppressFinalize(this);
    }
}
