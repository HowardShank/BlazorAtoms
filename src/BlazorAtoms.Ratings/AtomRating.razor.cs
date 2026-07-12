using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Ratings;

/// <summary>
/// A star/heart/… rating drawn as pure inline SVG. One component covers both jobs:
/// a read-only display that fills icons to any fraction of the value (e.g. 4.3 of 5), and — when
/// <see cref="ReadOnly"/> is false — an interactive input with hover preview, keyboard control, and a
/// configurable <see cref="Step"/>. The value is <see cref="Nullable{Double}"/>: <c>null</c> is the
/// "unrated" state (all icons empty), distinct from a real <c>0</c>. Two-way bind it with
/// <c>@bind-Value</c>.
/// </summary>
public partial class AtomRating : AtomComponentBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Transient value being previewed while the pointer/keyboard hovers; not committed.</summary>
    private double? _preview;

    /// <summary>The current rating. <c>null</c> means "not rated yet" (every icon empty). Bind with
    /// <c>@bind-Value</c>.</summary>
    [Parameter] public double? Value { get; set; }

    /// <summary>Raised when the user commits a new value (or clears it to <c>null</c>). Backs
    /// <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<double?> ValueChanged { get; set; }

    /// <summary>Number of icons in the scale. Default 5.</summary>
    [Parameter] public int Max { get; set; } = 5;

    /// <summary>Granularity the input snaps to (whole = 1, half-stars = 0.5, finer = 0.25, …). Display
    /// still fills to any fraction of <see cref="Value"/>; only user input snaps. Default 0.5.</summary>
    [Parameter] public double Step { get; set; } = 0.5;

    /// <summary>Show only (no hover/click/keyboard). Default false (interactive input).</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Dim and block all interaction. Default false.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Let the user clear the rating back to <c>null</c> by clicking the current value again
    /// (or pressing Delete/0). Default false.</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>Built-in icon shape. Ignored when <see cref="IconPath"/> is set. Default
    /// <see cref="RatingIcon.Star"/>.</summary>
    [Parameter] public RatingIcon Icon { get; set; } = RatingIcon.Star;

    /// <summary>Custom SVG path data for the filled icon (overrides <see cref="Icon"/>). Author it for a
    /// <c>0 0 24 24</c> view box, or set <see cref="IconViewBox"/> to match yours.</summary>
    [Parameter] public string? IconPath { get; set; }

    /// <summary>Optional distinct shape for the empty portion of each icon. When null the empty portion
    /// uses the same glyph as the filled one, tinted with <see cref="EmptyColor"/>.</summary>
    [Parameter] public RatingIcon? EmptyIcon { get; set; }

    /// <summary>Custom SVG path data for the empty icon (overrides <see cref="EmptyIcon"/>).</summary>
    [Parameter] public string? EmptyIconPath { get; set; }

    /// <summary>View box for the icon paths. Default <c>0 0 24 24</c> (matches the built-in glyphs).</summary>
    [Parameter] public string IconViewBox { get; set; } = RatingGlyphs.ViewBox;

    /// <summary>Rotate every icon glyph by this many degrees (clockwise, about its center) — e.g. to
    /// re-aim a triangle or teardrop. Purely visual; hit-testing and fractional fill stay horizontal.
    /// Maps to <c>--rating-rotate</c>. Default none.</summary>
    [Parameter] public double? Rotation { get; set; }

    /// <summary>Fill color of the rated portion (CSS color). Maps to <c>--rating-color</c>.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Fill color of the empty/track portion (CSS color). Maps to <c>--rating-empty</c>.</summary>
    [Parameter] public string? EmptyColor { get; set; }

    /// <summary>Fill color used while previewing a hover/keyboard selection (CSS color). Maps to
    /// <c>--rating-hover</c>.</summary>
    [Parameter] public string? HoverColor { get; set; }

    /// <summary>Icon size in px (width = height). Maps to <c>--rating-size</c>. Default 24.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Gap between icons (and the value/count labels) in px. Maps to <c>--rating-gap</c>.</summary>
    [Parameter] public double? Gap { get; set; }

    /// <summary>Show the numeric value beside the icons. Default false.</summary>
    [Parameter] public bool ShowValue { get; set; }

    /// <summary>Format string for the value label. Default <c>0.#</c>.</summary>
    [Parameter] public string ValueFormat { get; set; } = "0.#";

    /// <summary>Text shown in place of the value label when the rating is <c>null</c>. Default "Unrated".</summary>
    [Parameter] public string UnratedText { get; set; } = "Unrated";

    /// <summary>Optional review/vote count shown after the icons (e.g. "(1,204)").</summary>
    [Parameter] public int? Count { get; set; }

    /// <summary>Format string for <see cref="Count"/>. Default <c>N0</c>.</summary>
    [Parameter] public string CountFormat { get; set; } = "N0";

    /// <summary>Accessible label. When null a sensible default is generated from the value and
    /// <see cref="Max"/>.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ---- derived render state ---------------------------------------------------------------

    private bool Interactive => !ReadOnly && !Disabled;

    private double SizePx => Size ?? 24;

    /// <summary>Value to draw: the live preview when hovering, otherwise the committed value.</summary>
    private double? Effective => _preview ?? Value;

    private string FilledPath => IconPath ?? RatingGlyphs.Path(Icon);

    private string EmptyPath =>
        EmptyIconPath ?? (EmptyIcon.HasValue ? RatingGlyphs.Path(EmptyIcon.Value) : FilledPath);

    private string RootClass => _preview is not null ? "atom-rating atom-rating-preview" : "atom-rating";

    private string? Role => Interactive ? "slider" : "img";

    private string? TabIndex => Interactive ? "0" : null;

    private string AriaLabelValue =>
        AriaLabel ?? (Value.HasValue
            ? $"{Value.Value.ToString(Inv)} out of {Max}"
            : $"Unrated, out of {Max}");

    private string? AriaValueNow => Interactive ? Value?.ToString(Inv) : null;

    private string RootStyle => new StyleVars("rating")
        .Add("size", Size)
        .Add("gap", Gap)
        .Add("color", Color)
        .Add("empty", EmptyColor)
        .Add("hover", HoverColor)
        .Add("rotate", Rotation.HasValue ? Rotation.Value.ToString(Inv) + "deg" : null)
        .ToString();

    /// <summary>Inline width for icon <paramref name="index"/>'s filled overlay (0-based).</summary>
    private string FillWidth(int index)
    {
        var f = Math.Clamp((Effective ?? 0) - index, 0, 1);
        return $"{Math.Round(f * 100, 3).ToString(Inv)}%";
    }

    private string ValueText =>
        Effective.HasValue ? Effective.Value.ToString(ValueFormat, Inv) : UnratedText;

    // ---- interaction ------------------------------------------------------------------------

    /// <summary>Round a continuous position up to the nearest <see cref="Step"/>, clamped to
    /// [Step, Max].</summary>
    private double Snap(double raw)
    {
        var step = Step <= 0 ? 1 : Step;
        var v = Math.Ceiling(raw / step - 1e-9) * step;
        v = Math.Clamp(v, step, Max);
        return Math.Round(v, 4);
    }

    /// <summary>Value under the pointer within icon <paramref name="index"/> (0-based).</summary>
    private double ValueAt(int index, MouseEventArgs e)
    {
        var withinIcon = SizePx > 0 ? e.OffsetX / SizePx : 0;
        return Snap(index + Math.Clamp(withinIcon, 0, 1));
    }

    private void OnMove(int index, MouseEventArgs e)
    {
        if (!Interactive) return;
        _preview = ValueAt(index, e);
    }

    private void OnLeave()
    {
        if (_preview is null) return;
        _preview = null;
    }

    private async Task OnClickItem(int index, MouseEventArgs e)
    {
        if (!Interactive) return;
        var v = ValueAt(index, e);
        if (Clearable && Value.HasValue && Math.Abs(Value.Value - v) < 1e-9)
            await Commit(null);
        else
            await Commit(v);
    }

    private async Task OnKey(KeyboardEventArgs e)
    {
        if (!Interactive) return;
        var step = Step <= 0 ? 1 : Step;
        var current = Value ?? 0;
        switch (e.Key)
        {
            case "ArrowRight":
            case "ArrowUp":
                await Commit(ClampValue(current + step));
                break;
            case "ArrowLeft":
            case "ArrowDown":
                var dec = current - step;
                await Commit(dec < step ? (Clearable ? (double?)null : step) : Math.Round(dec, 4));
                break;
            case "Home":
                await Commit(step);
                break;
            case "End":
                await Commit((double)Max);
                break;
            case "0":
            case "Delete":
            case "Backspace":
                await Commit(Clearable ? (double?)null : step);
                break;
            default:
                return;
        }
    }

    private double ClampValue(double v) => Math.Round(Math.Clamp(v, Step <= 0 ? 1 : Step, Max), 4);

    private async Task Commit(double? v)
    {
        _preview = null;
        if (v != Value)
        {
            Value = v;
            await ValueChanged.InvokeAsync(v);
        }
    }
}
