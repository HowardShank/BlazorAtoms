using System;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Tooltips;

/// <summary>
/// Tooltip whose bubble is both shaped <em>and painted</em> by inline SVG: a linear-gradient fill
/// (when <see cref="GradientFrom"/>/<see cref="GradientTo"/> are set), SVG stroke border, and an
/// optional soft drop shadow — across every shape. Falls back to the solid <c>--tip-bg</c> token
/// when no gradient is given, so it also works as a plain shaped tooltip. Positioning + show/hide
/// are pure CSS; only <see cref="Placement.Cursor"/> uses a tiny self-loaded JS module.
/// </summary>
public partial class AtomPaintedTooltip : AtomComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The element(s) the tooltip is attached to. Required.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Simple text content. Ignored when <see cref="TooltipContent"/> is set.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Rich content. Takes priority over <see cref="Text"/>.</summary>
    [Parameter] public RenderFragment? TooltipContent { get; set; }

    /// <summary>Side, corner, or cursor mode the bubble is placed by.</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.Top;

    /// <summary>Bubble outline shape (drawn as SVG).</summary>
    [Parameter] public PaintedTooltipShape Shape { get; set; } = PaintedTooltipShape.Rectangle;

    /// <summary>Solid fill when no gradient is set. Sets <c>--tip-bg</c>.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Gradient start color. Set together with <see cref="GradientTo"/> to paint a
    /// linear-gradient fill (overrides <see cref="Background"/>).</summary>
    [Parameter] public string? GradientFrom { get; set; }

    /// <summary>Gradient end color. See <see cref="GradientFrom"/>.</summary>
    [Parameter] public string? GradientTo { get; set; }

    /// <summary>Gradient direction in degrees (0 = left→right, 90 = top→bottom). Default 90.</summary>
    [Parameter] public double GradientAngle { get; set; } = 90;

    /// <summary>Draw a soft drop shadow behind the bubble.</summary>
    [Parameter] public bool Shadow { get; set; } = true;

    /// <summary>Bubble text color. Sets <c>--tip-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Border (SVG stroke) color. Sets <c>--tip-border</c>.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Border (SVG stroke) width in px. Sets <c>--tip-border-width</c>. Uniform (non-scaling).</summary>
    [Parameter] public double? BorderWidth { get; set; }

    /// <summary>Corner rounding for the <see cref="PaintedTooltipShape.Rectangle"/> shape, in viewBox units (0–50).</summary>
    [Parameter] public double Radius { get; set; } = 12;

    /// <summary>Arrow size in px. Sets <c>--tip-arrow-size</c>.</summary>
    [Parameter] public double? ArrowSize { get; set; }

    /// <summary>Bubble max-width (any CSS length). Sets <c>--tip-max-width</c>.</summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>Explicit bubble width (any CSS length). Null = fit content. Useful for
    /// <see cref="PaintedTooltipShape.Cloud"/>/<see cref="PaintedTooltipShape.Ellipse"/>, whose outline needs room.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Explicit bubble height (any CSS length). Null = fit content.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Gap between trigger and bubble (px); in Cursor mode, gap from the pointer. Sets <c>--tip-offset</c>.</summary>
    [Parameter] public double? Offset { get; set; }

    /// <summary>Draw the attachment arrow / cloud trail. Ignored in Cursor mode and on Burst/FoldedCorner.</summary>
    [Parameter] public bool ShowArrow { get; set; } = true;

    /// <summary>When true, the bubble never renders (trigger still renders).</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Extra CSS class(es) on the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Extra inline style appended after the built-in theme style.</summary>
    [Parameter] public string? Style { get; set; }

    private readonly string _id = "pt" + Guid.NewGuid().ToString("N")[..8];
    private string GradId => "grad-" + _id;

    private ElementReference _triggerRef;
    private ElementReference _bubbleRef;
    private IJSObjectReference? _module;
    private bool _cursorActive;

    private bool HasGradient => GradientFrom is not null && GradientTo is not null;

    // Inline fill override so the gradient beats the stylesheet's fill:var(--tip-bg) on .app-path.
    private string? PathFillStyle => HasGradient ? $"fill:url(#{GradId})" : null;

    private bool ShowsArrow => ShowArrow
        && Placement != Placement.Cursor
        && Shape != PaintedTooltipShape.Burst
        && Shape != PaintedTooltipShape.FoldedCorner;

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

    private string ShapeValue => Shape switch
    {
        PaintedTooltipShape.Rectangle => "rectangle",
        PaintedTooltipShape.Pill => "pill",
        PaintedTooltipShape.Ellipse => "ellipse",
        PaintedTooltipShape.Cloud => "cloud",
        PaintedTooltipShape.Burst => "burst",
        PaintedTooltipShape.FoldedCorner => "folded",
        _ => "rectangle",
    };

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);

    private string RxStr => N(Clamp(Radius, 0, 50));

    private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);

    private string? BubbleStyle => string.Concat(
        Width is null ? "" : $"width:{Width};",
        Height is null ? "" : $"height:{Height};") is { Length: > 0 } s ? s : null;

    // Gradient vector across the object bounding box (0..1). 0deg = left→right, 90 = top→bottom.
    private string GradX1 => N(Math.Round(0.5 - Math.Cos(Rad) * 0.5, 4));
    private string GradY1 => N(Math.Round(0.5 - Math.Sin(Rad) * 0.5, 4));
    private string GradX2 => N(Math.Round(0.5 + Math.Cos(Rad) * 0.5, 4));
    private string GradY2 => N(Math.Round(0.5 + Math.Sin(Rad) * 0.5, 4));
    private double Rad => GradientAngle * Math.PI / 180.0;

    private const string CloudPath =
        "M22 80 C9 80 6 61 17 56 C10 43 24 35 34 42 C38 27 62 27 66 42 " +
        "C79 35 92 46 85 58 C95 62 92 80 79 80 Z";

    private static readonly (double X, double Y)[] BurstPoints =
    {
        (50,0),(61,12),(77,6),(76,24),(94,24),(82,38),(100,50),(82,62),(94,76),(76,76),
        (77,94),(61,88),(50,100),(39,88),(23,94),(24,76),(6,76),(18,62),(0,50),(18,38),
        (6,24),(24,24),(23,6),(39,12),
    };

    private static readonly string BurstPath = BuildBurstPath();

    private static string BuildBurstPath()
    {
        const double shrink = 0.94;
        var sb = new StringBuilder();
        for (var i = 0; i < BurstPoints.Length; i++)
        {
            var (x, y) = BurstPoints[i];
            var px = 50 + (x - 50) * shrink;
            var py = 50 + (y - 50) * shrink;
            sb.Append(i == 0 ? "M" : "L").Append(N(Math.Round(px, 2))).Append(' ').Append(N(Math.Round(py, 2))).Append(' ');
        }
        return sb.Append('Z').ToString();
    }

    private string RootStyle
    {
        get
        {
            var style = string.Concat(
                Background is null ? "" : $"--tip-bg:{Background};",
                // Arrow is a solid CSS square; match it to the gradient's start so it doesn't clash.
                HasGradient ? $"--tip-arrow-fill:{GradientFrom};" : "",
                TextColor is null ? "" : $"--tip-color:{TextColor};",
                BorderColor is null ? "" : $"--tip-border:{BorderColor};",
                BorderWidth is null ? "" : $"--tip-border-width:{N(BorderWidth.Value)}px;",
                ArrowSize is null ? "" : $"--tip-arrow-size:{N(ArrowSize.Value)}px;",
                MaxWidth is null ? "" : $"--tip-max-width:{MaxWidth};",
                Offset is null ? "" : $"--tip-offset:{N(Offset.Value)}px;");
            return Style is null ? style : style + Style;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var wantCursor = Placement == Placement.Cursor && !Disabled;
        if (wantCursor && !_cursorActive)
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorAtoms.Tooltips/atom-painted-tooltip.js");
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
