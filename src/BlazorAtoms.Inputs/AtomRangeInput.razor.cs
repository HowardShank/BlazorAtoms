using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Inputs;

/// <summary>
/// A labeled slider/range input with help text, min/max/step, and an <see cref="EditContext"/>-aware
/// error state — the first form/validation-integrated component in BlazorAtoms. Renders a native
/// <c>&lt;input type="range"&gt;</c> (free pointer/touch/keyboard support, no JS) styled via CSS
/// custom properties. <see cref="Disabled"/> means "don't render at all"; <see cref="ReadOnly"/>
/// means "render greyed out, no input" — these are deliberately different (see DEVELOPMENT.md).
/// </summary>
public partial class AtomRangeInput<TValue> : AtomComponentBase, IDisposable
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    /// <summary>The current value. Bind with <c>@bind-Value</c>.</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>Raised when the value changes. Backs <c>@bind-Value</c>; only needed directly when
    /// not using <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Identifies the bound value. Populated automatically by <c>@bind-Value</c>; required
    /// if you use <see cref="ValueChanged"/> directly instead.</summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>Minimum value (inclusive), may be negative. Normally less than <see cref="Max"/>;
    /// an inverted range is tolerated (no throw) and left to the native input to clamp. Default 0.</summary>
    [Parameter] public TValue Min { get; set; } = RangeConvert.FromDouble<TValue>(0);

    /// <summary>Maximum value (inclusive), may be negative. Normally greater than <see cref="Min"/>;
    /// an inverted range is tolerated (no throw). Default 100.</summary>
    [Parameter] public TValue Max { get; set; } = RangeConvert.FromDouble<TValue>(100);

    /// <summary>Amount the value changes per tick. May be fractional (e.g. <c>0.5</c>) when
    /// <c>TValue</c> is a floating type. Default 1.</summary>
    [Parameter] public TValue Step { get; set; } = RangeConvert.FromDouble<TValue>(1);

    /// <summary>Form label.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Responsive classes for the label column. Default <c>clr-col-12 clr-col-md-2</c>.</summary>
    [Parameter] public string LabelCol { get; set; } = "clr-col-12 clr-col-md-2";

    /// <summary>Responsive classes for the control column. Default <c>clr-col-12 clr-col-md-10</c>.</summary>
    [Parameter] public string ControlCol { get; set; } = "clr-col-12 clr-col-md-10";

    /// <summary>Help text shown under the control when there is no validation error.</summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>Identifies the property used for form validation — wires this component to an
    /// ancestor <see cref="EditContext"/> (e.g. from <c>&lt;EditForm&gt;</c> +
    /// <c>&lt;DataAnnotationsValidator /&gt;</c>). Falls back to <see cref="ValueExpression"/> when
    /// not set, so a plain <c>@bind-Value</c> inside an <c>EditForm</c> still participates.</summary>
    [Parameter] public Expression<Func<TValue>>? ValidationFor { get; set; }

    /// <summary>When true, the control is greyed out and blocks input. Use <see cref="Visible"/> to
    /// show/hide instead.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Alias of <see cref="Disabled"/> — greys out and blocks input. (A native
    /// <c>&lt;input type="range"&gt;</c> has no meaningful read-only state of its own, so read-only
    /// and disabled are the same thing here.)</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Whether the control is shown. When false it is hidden via CSS <c>display:none</c>
    /// (still in the DOM). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>Shape of the drag handle. Default <see cref="HandleShape.Round"/>.</summary>
    [Parameter] public HandleShape HandleShape { get; set; } = HandleShape.Round;

    /// <summary>Track width in px. Maps to <c>--range-track-width</c>.</summary>
    [Parameter] public double? TrackWidth { get; set; }

    /// <summary>Track height in px. Maps to <c>--range-track-height</c>.</summary>
    [Parameter] public double? TrackHeight { get; set; }

    /// <summary>Handle size in px (width = height). Maps to <c>--range-handle-size</c>.</summary>
    [Parameter] public double? HandleSize { get; set; }

    /// <summary>Fill color of the handle, independent of the track fill (<c>Background</c>/
    /// <c>--range-fill-color</c>). Default white. Maps to <c>--range-handle-color</c>.</summary>
    [Parameter] public string? HandleColor { get; set; }

    /// <summary>Outline (border/stroke) color of the handle — independent of the track fill color.
    /// Default <c>#2563eb</c>. Maps to <c>--range-handle-outline-color</c>.</summary>
    [Parameter] public string? OutlineColor { get; set; }

    /// <summary>Outline (border/stroke) width of the handle in px. Default 2. <c>0</c> = no outline.
    /// Maps to <c>--range-handle-outline-width</c>.</summary>
    [Parameter] public double? OutlineWidth { get; set; }

    /// <summary>Vertical position of the handle relative to the track. Default
    /// <see cref="HandlePosition.Center"/>. Overridden by <see cref="HandleOffset"/> when set.</summary>
    [Parameter] public HandlePosition HandlePosition { get; set; } = HandlePosition.Center;

    /// <summary>Precise vertical handle offset in px (negative = above the track, positive = below,
    /// <c>0</c> = centered). When set, overrides <see cref="HandlePosition"/>. Maps to
    /// <c>--range-handle-offset</c>.</summary>
    [Parameter] public double? HandleOffset { get; set; }

    /// <summary>Rotation of the handle in degrees (clockwise, about its center). Rotates the shape
    /// itself — e.g. to re-aim a Triangle or Teardrop. Maps to <c>--range-handle-rotate</c>.</summary>
    [Parameter] public double? HandleRotation { get; set; }

    /// <summary>Track layout axis. Default <see cref="Inputs.Orientation.Horizontal"/>. Vertical
    /// renders the same internals rotated in place — every other feature (fill, handle
    /// offset/rotation/shape, icons) keeps working unchanged.</summary>
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>Which end of the track holds the maximum value when <see cref="Orientation"/> is
    /// <see cref="Inputs.Orientation.Vertical"/>. Default <see cref="VerticalDirection.BottomToTop"/>
    /// (max at top). Ignored when horizontal.</summary>
    [Parameter] public VerticalDirection VerticalDirection { get; set; } = VerticalDirection.BottomToTop;

    /// <summary>Built-in icon pair (e.g. mute/loud, cold/hot), tied to the value's min/max ends —
    /// see <see cref="RangeIconPreset"/>. Which screen slot (Start/End) each icon lands in tracks
    /// <see cref="Orientation"/>/<see cref="VerticalDirection"/> automatically. Overridden per-slot
    /// by an explicit <see cref="StartIcon"/>/<see cref="EndIcon"/>. Default
    /// <see cref="RangeIconPreset.None"/>.</summary>
    [Parameter] public RangeIconPreset IconPreset { get; set; } = RangeIconPreset.None;

    /// <summary>Swaps which built-in icon represents the min end vs the max end, independent of
    /// <see cref="VerticalDirection"/>. Default false.</summary>
    [Parameter] public bool IconPresetReversed { get; set; }

    /// <summary>Optional content shown at the start of the track (left when horizontal; the end
    /// <see cref="VerticalDirection"/> currently puts first when vertical). Overrides any
    /// <see cref="IconPreset"/> icon for this slot. Rendered in its own
    /// <c>.atom-range-input-icon-start</c> slot; style it independently.</summary>
    [Parameter] public RenderFragment? StartIcon { get; set; }

    /// <summary>Optional content shown at the end of the track. Overrides any <see cref="IconPreset"/>
    /// icon for this slot. Rendered in its own <c>.atom-range-input-icon-end</c> slot; style it
    /// independently.</summary>
    [Parameter] public RenderFragment? EndIcon { get; set; }

    /// <summary>Accessible label for the slider. Falls back to <see cref="Label"/> when null.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ---- EditContext wiring -------------------------------------------------------------------

    protected override void OnInitialized()
    {
        if (CascadedEditContext is not null)
            CascadedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
    }

    protected override void OnParametersSet()
    {
        var expr = ValidationFor ?? ValueExpression;
        _hasFieldIdentifier = expr is not null;
        _fieldIdentifier = expr is not null ? FieldIdentifier.Create(expr) : default;
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) =>
        StateHasChanged();

    public void Dispose()
    {
        if (CascadedEditContext is not null)
            CascadedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
    }

    // ---- derived render state ---------------------------------------------------------------

    private bool HasError =>
        _hasFieldIdentifier && CascadedEditContext is not null &&
        CascadedEditContext.GetValidationMessages(_fieldIdentifier).Any();

    private string? ErrorMessage =>
        HasError ? CascadedEditContext!.GetValidationMessages(_fieldIdentifier).FirstOrDefault() : null;

    private string? DisplayText => HasError ? ErrorMessage : HelpText;

    private string RootClass => "atom-range-input";

    /// <summary>ReadOnly and Disabled are the same state for a range input.</summary>
    private bool IsDisabled => Disabled || ReadOnly;

    private string? State => HasError ? "error" : (IsDisabled ? "disabled" : null);

    private double ValueDouble => RangeConvert.ToDouble(Value);
    private double MinDouble => RangeConvert.ToDouble(Min);
    private double MaxDouble => RangeConvert.ToDouble(Max);
    private double StepDouble => RangeConvert.ToDouble(Step);

    private string EffectiveAriaLabel => AriaLabel ?? Label ?? "Range";

    private string HandleShapeAttr => HandleShape.ToString().ToLowerInvariant();

    private bool IsGlyphHandle => HandleGlyphs.IsGlyph(HandleShape);

    /// <summary>`vertical` for the data-attribute + `aria-orientation`; null (omitted) for the
    /// horizontal default, so existing horizontal usage renders with zero CSS/markup footprint.</summary>
    private string? OrientationAttr => Orientation == Orientation.Vertical ? "vertical" : null;

    /// <summary>`top-to-bottom` when vertical and reversed; null otherwise (horizontal, or the
    /// default bottom-to-top) — the CSS default already matches the common case.</summary>
    private string? VerticalDirectionAttr =>
        Orientation == Orientation.Vertical && VerticalDirection == VerticalDirection.TopToBottom
            ? "top-to-bottom"
            : null;

    // ---- built-in icon presets -----------------------------------------------------------

    private static readonly RenderFragment VolumeMuteIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\" aria-hidden=\"true\" data-icon=\"volume-mute\">" +
        "<path d=\"M4 9v6h4l5 5V4L8 9H4z\"/>" +
        "<path d=\"M16 9l5 6M21 9l-5 6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"/></svg>");

    private static readonly RenderFragment VolumeLoudIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\" aria-hidden=\"true\" data-icon=\"volume-loud\">" +
        "<path d=\"M4 9v6h4l5 5V4L8 9H4z\"/>" +
        "<path d=\"M16 8a5 5 0 0 1 0 8M18.5 5.5a9 9 0 0 1 0 13\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"/></svg>");

    private static readonly RenderFragment ThermostatColdIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\" data-icon=\"thermostat-cold\">" +
        "<path d=\"M12 2v20M3.34 7l17.32 10M3.34 17L20.66 7\"/>" +
        "<path d=\"M12 5l-1.8 1.2M12 5l1.8 1.2M12 19l-1.8-1.2M12 19l1.8-1.2\"/>" +
        "<path d=\"M5 8.6l.6 2.1-2 .8M19 8.6l-.6 2.1 2 .8M5 15.4l.6-2.1-2-.8M19 15.4l-.6-2.1 2-.8\"/></svg>");

    private static readonly RenderFragment ThermostatHotIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\" aria-hidden=\"true\" data-icon=\"thermostat-hot\">" +
        "<path d=\"M12 2c-4 4-7 7-7 11a7 7 0 0 0 14 0c0-2.5-1.2-4-2.5-5.5.3 2-.6 3.2-1.5 3.5.8-3-1-6-3-9z\"/></svg>");

    private static readonly RenderFragment BrightnessLowIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\" data-icon=\"brightness-low\">" +
        "<circle cx=\"12\" cy=\"12\" r=\"3\" fill=\"currentColor\" stroke=\"none\"/>" +
        "<path d=\"M12 4v1M12 19v1M4 12h1M19 12h1\"/></svg>");

    private static readonly RenderFragment BrightnessHighIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\" data-icon=\"brightness-high\">" +
        "<circle cx=\"12\" cy=\"12\" r=\"4.5\" fill=\"currentColor\" stroke=\"none\"/>" +
        "<path d=\"M12 2v2M12 20v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M2 12h2M20 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4\"/></svg>");

    private static readonly RenderFragment SpeedSlowIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\" aria-hidden=\"true\" data-icon=\"speed-slow\">" +
        "<path d=\"M10 7v10l7-5z\"/></svg>");

    private static readonly RenderFragment SpeedFastIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\" aria-hidden=\"true\" data-icon=\"speed-fast\">" +
        "<path d=\"M4 7v10l6-5z\"/><path d=\"M13 7v10l6-5z\"/></svg>");

    private static readonly RenderFragment PriceLowIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\" data-icon=\"price-low\">" +
        "<circle cx=\"12\" cy=\"12\" r=\"8\"/>" +
        "<path d=\"M12 7v10M9.5 9.5c0-1 .8-1.5 2.5-1.5s2.5.6 2.5 1.5-1 1.3-2.5 1.8-2.5 1-2.5 2 1 1.7 2.5 1.7 2.5-.5 2.5-1.5\"/></svg>");

    private static readonly RenderFragment PriceHighIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" aria-hidden=\"true\" data-icon=\"price-high\">" +
        "<ellipse cx=\"12\" cy=\"18\" rx=\"7\" ry=\"3\"/><ellipse cx=\"12\" cy=\"13\" rx=\"7\" ry=\"3\"/><ellipse cx=\"12\" cy=\"8\" rx=\"7\" ry=\"3\"/></svg>");

    private static readonly RenderFragment OpacityLowIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-dasharray=\"3 3\" aria-hidden=\"true\" data-icon=\"opacity-low\">" +
        "<circle cx=\"12\" cy=\"12\" r=\"8\"/></svg>");

    private static readonly RenderFragment OpacityHighIcon = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\" aria-hidden=\"true\" data-icon=\"opacity-high\">" +
        "<circle cx=\"12\" cy=\"12\" r=\"8\"/></svg>");

    private RenderFragment? MinIconMarkup => IconPreset switch
    {
        RangeIconPreset.Volume => VolumeMuteIcon,
        RangeIconPreset.Thermostat => ThermostatColdIcon,
        RangeIconPreset.Brightness => BrightnessLowIcon,
        RangeIconPreset.PlaybackSpeed => SpeedSlowIcon,
        RangeIconPreset.Price => PriceLowIcon,
        RangeIconPreset.Opacity => OpacityLowIcon,
        _ => null,
    };

    private RenderFragment? MaxIconMarkup => IconPreset switch
    {
        RangeIconPreset.Volume => VolumeLoudIcon,
        RangeIconPreset.Thermostat => ThermostatHotIcon,
        RangeIconPreset.Brightness => BrightnessHighIcon,
        RangeIconPreset.PlaybackSpeed => SpeedFastIcon,
        RangeIconPreset.Price => PriceHighIcon,
        RangeIconPreset.Opacity => OpacityHighIcon,
        _ => null,
    };

    private RenderFragment? EffectiveMinIcon => IconPresetReversed ? MaxIconMarkup : MinIconMarkup;
    private RenderFragment? EffectiveMaxIcon => IconPresetReversed ? MinIconMarkup : MaxIconMarkup;

    /// <summary>Whether the `Start` DOM slot currently holds the min end of the value range.
    /// Horizontal has no reverse concept, so Start (left) is always min. Vertical flips with
    /// <see cref="VerticalDirection"/> since Start always renders visually first (top).</summary>
    private bool MinIsAtStartSlot =>
        Orientation == Orientation.Horizontal || VerticalDirection == VerticalDirection.TopToBottom;

    /// <summary>Explicit <see cref="StartIcon"/> wins; otherwise the preset icon routed to whichever
    /// end (min/max) currently occupies the Start slot.</summary>
    private RenderFragment? StartSlotContent =>
        StartIcon ?? (MinIsAtStartSlot ? EffectiveMinIcon : EffectiveMaxIcon);

    private RenderFragment? EndSlotContent =>
        EndIcon ?? (MinIsAtStartSlot ? EffectiveMaxIcon : EffectiveMinIcon);

    /// <summary>`above`/`below` for the data attribute the CSS keys the offset off; null (no
    /// attribute) for the centered default or when a numeric <see cref="HandleOffset"/> takes over.</summary>
    private string? HandlePositionAttr =>
        HandleOffset is not null ? null
        : HandlePosition == HandlePosition.Above ? "above"
        : HandlePosition == HandlePosition.Below ? "below"
        : null;

    /// <summary>Vertical room (px) the raised/dropped handle needs reserved as wrapper padding so it
    /// isn't clipped and doesn't overlap the label/help rows. Zero when centered.</summary>
    private double HandleRoom
    {
        get
        {
            if (HandleOffset is not null) return Math.Abs(HandleOffset.Value);
            if (HandlePosition == HandlePosition.Center) return 0;
            return ((HandleSize ?? 18) + (TrackHeight ?? 6)) / 2;
        }
    }

    private const string HandleColorDefault = "#ffffff";
    private const string OutlineColorDefault = "#2563eb";
    private const string ErrorColorDefault = "#dc2626";
    private const double OutlineWidthDefault = 2;

    /// <summary>Inline CSS for the native input: the fill percent (drives the track gradient), the
    /// handle/outline color custom properties (consumed by the box-shape thumb CSS), and — for a
    /// glyph handle — a baked <c>background-image</c> SVG carrying the handle's fill + stroke
    /// (a mask can't hold two colors, and a background-image can't read CSS vars, so the glyph is
    /// painted from resolved values here).</summary>
    private string InputStyle
    {
        get
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"--range-fill:{FillPercent.ToString("0.###", Inv)}%;");

            // Box-shape thumbs (Round/Square) read these vars from CSS; only emit when set.
            if (!string.IsNullOrEmpty(HandleColor)) sb.Append($"--range-handle-color:{HandleColor};");
            if (!string.IsNullOrEmpty(OutlineColor)) sb.Append($"--range-handle-outline-color:{OutlineColor};");
            if (OutlineWidth is not null) sb.Append($"--range-handle-outline-width:{OutlineWidth.Value.ToString(Inv)}px;");

            // Vertical handle offset: a numeric HandleOffset overrides the enum (inline var beats the
            // data-attribute rule); either way reserve room so the moved handle isn't clipped.
            if (HandleOffset is not null) sb.Append($"--range-handle-offset:{HandleOffset.Value.ToString(Inv)}px;");
            if (HandleRoom > 0) sb.Append($"--range-handle-room:{HandleRoom.ToString(Inv)}px;");
            if (HandleRotation is not null) sb.Append($"--range-handle-rotate:{HandleRotation.Value.ToString(Inv)}deg;");

            var path = HandleGlyphs.Path(HandleShape);
            if (path is not null) sb.Append(GlyphBackground(path));
            return sb.ToString();
        }
    }

    /// <summary>Builds the baked glyph <c>background-image</c>: an inline SVG with the resolved fill
    /// and stroke. View box is padded (<c>-3 -3 30 30</c>) so the stroke isn't clipped; stroke width
    /// is converted from px to view-box units against the handle size.</summary>
    private string GlyphBackground(string path)
    {
        var fill = string.IsNullOrEmpty(HandleColor) ? HandleColorDefault : HandleColor;
        var stroke = HasError ? ErrorColorDefault : (string.IsNullOrEmpty(OutlineColor) ? OutlineColorDefault : OutlineColor);
        var widthPx = OutlineWidth ?? OutlineWidthDefault;

        var strokeAttr = "";
        if (widthPx > 0)
        {
            var sw = widthPx * 30 / (HandleSize ?? 18);
            strokeAttr = $" stroke=\"{stroke}\" stroke-width=\"{sw.ToString("0.###", Inv)}\" stroke-linejoin=\"round\"";
        }

        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"-3 -3 30 30\">" +
                  $"<path d=\"{path}\" fill=\"{fill}\"{strokeAttr}/></svg>";

        // Encode '#' as %23: a literal '#' in a data: URL starts a fragment and truncates the SVG,
        // so hex colors (#2563eb, …) would otherwise cut the glyph off and it renders nothing.
        svg = svg.Replace("#", "%23");

        // Delivered as a custom property (not `background-image` directly): the thumb is a pseudo-
        // element, and only inherited custom properties reach it — a background-image set on the
        // <input> would paint the input box, not the thumb.
        return $"--range-handle-glyph:url('data:image/svg+xml;utf8,{svg}');";
    }

    /// <summary>Percent of the track that is "filled" (from Min to the current value), clamped to
    /// 0-100. Drives the two-tone track gradient; WebKit has no native filled-track pseudo-element,
    /// so the CSS paints it from this. Handles an inverted Min/Max range without dividing by zero.</summary>
    private double FillPercent
    {
        get
        {
            var lo = Math.Min(MinDouble, MaxDouble);
            var hi = Math.Max(MinDouble, MaxDouble);
            if (hi <= lo) return 0;
            return Math.Clamp((ValueDouble - lo) / (hi - lo) * 100, 0, 100);
        }
    }


    private string? RootStyle
    {
        get
        {
            var vars = new StyleVars("range")
                .Add("track-width", TrackWidth)
                .Add("track-height", TrackHeight)
                .Add("handle-size", HandleSize)
                .ToString();
            var s = (Visible ? "" : "display:none;") + vars;
            return string.IsNullOrEmpty(s) ? null : s;
        }
    }

    // ---- interaction --------------------------------------------------------------------------

    private async Task OnInput(ChangeEventArgs e)
    {
        if (IsDisabled) return;
        if (!double.TryParse((string?)e.Value, NumberStyles.Float, Inv, out var d)) return;

        var newValue = RangeConvert.FromDouble<TValue>(d);
        if (Equals(newValue, Value)) return;

        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
        if (_hasFieldIdentifier)
            CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier);
    }
}
