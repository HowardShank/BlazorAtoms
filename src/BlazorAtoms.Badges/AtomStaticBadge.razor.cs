using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Badges;

/// <summary>
/// A static (no-animation) badge: a small label/count in a rich variety of shapes. Pill, circle,
/// square and rounded are pure-CSS boxes; star, hexagon, diamond, shield, starburst and ribbon are
/// drawn as inline SVG so fill and border apply to every shape. When wrapped around a host element
/// (<see cref="ChildContent"/>) it overlays a corner (notification style); otherwise it renders
/// inline. Accepts any <see cref="object"/> value with type-aware formatting (or a
/// <see cref="Formatter"/> override). No JS.
/// </summary>
public partial class AtomStaticBadge : AtomComponentBase
{
    /// <summary>Optional host element the badge overlays. When null, the badge renders inline.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The value shown. Any object; converted to a display string (see <see cref="Formatter"/>
    /// and the type-aware defaults). The badge only appears when the value is "present".</summary>
    [Parameter] public object? Value { get; set; }

    /// <summary>Full override for value→string. When set, type-aware defaults are skipped.</summary>
    [Parameter] public Func<object?, string>? Formatter { get; set; }

    /// <summary>For numeric values, the cap above which the badge shows "<c>{Max}+</c>". 0 = no cap.</summary>
    [Parameter] public int Max { get; set; }

    /// <summary>Show a numeric badge when the value is 0. Default false (0 hides the badge).</summary>
    [Parameter] public bool ShowZero { get; set; }

    /// <summary>Render a textless dot (presence indicator) instead of the value text.</summary>
    [Parameter] public bool Dot { get; set; }

    /// <summary>Corner the badge sits at when overlaying a host. Ignored inline.</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.TopEnd;

    /// <summary>Badge outline shape.</summary>
    [Parameter] public Shape Shape { get; set; } = Shape.Pill;

    /// <summary>Preset color scheme (overridden by explicit color params).</summary>
    [Parameter] public Variant Variant { get; set; } = Variant.Default;

    /// <summary>Background / SVG fill. Sets <c>--sb-bg</c>. Null = variant/scheme default.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color. Sets <c>--sb-color</c>. Null = variant/scheme default.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Border / SVG stroke color. Sets <c>--sb-border</c>. Null = variant/scheme default.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Border / stroke width in px. Sets <c>--sb-border-width</c>.</summary>
    [Parameter] public double? BorderWidth { get; set; }

    /// <summary>Badge size in px (drives height, min-width, font-size). Sets <c>--sb-size</c>.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Explicit width (any CSS length). Overrides the size-driven default. Sets <c>--sb-width</c>.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Explicit height (any CSS length). Overrides the size-driven default. Sets <c>--sb-height</c>.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Corner radius in px for <see cref="Shape.Rounded"/>. Sets <c>--sb-radius</c>.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Gap the badge is nudged outward from the host corner, in px. Sets <c>--sb-offset</c>.</summary>
    [Parameter] public double? Offset { get; set; }

    /// <summary>Max width (any CSS length); longer text truncates with an ellipsis. Sets <c>--sb-max-width</c>.</summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>Accessible label. Falls back to the display string.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>The formatted text (may be empty — e.g. for Dot or bool values).</summary>
    private string Display
    {
        get
        {
            if (Formatter is not null) return Formatter(Value) ?? "";
            return Value switch
            {
                null => "",
                bool => "",                       // presence-only; shown as dot or nothing
                string s => s,
                DateTime dt => dt.ToString("d", CultureInfo.InvariantCulture),
                Enum e => EnumDisplay(e),
                _ when IsNumeric(Value) => NumericDisplay(Value!),
                _ => Value.ToString() ?? "",
            };
        }
    }

    /// <summary>Whether there is something to show (gates the popup).</summary>
    private bool IsPresent => Value switch
    {
        null => false,
        string s => s.Length > 0,
        bool b => b,
        _ when IsNumeric(Value) => ShowZero || Convert.ToDecimal(Value, CultureInfo.InvariantCulture) != 0m,
        _ => true,
    };

    private bool ShowBadge => Dot ? IsPresent : (IsPresent && Display.Length > 0);

    private static bool IsNumeric(object? v) =>
        v is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private string NumericDisplay(object v)
    {
        var d = Convert.ToDecimal(v, CultureInfo.InvariantCulture);
        if (Max > 0 && d > Max) return $"{Max}+";
        // Trim trailing zeros for fractional values; integers render plainly.
        return d == Math.Truncate(d)
            ? ((long)d).ToString(CultureInfo.InvariantCulture)
            : d.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string EnumDisplay(Enum e)
    {
        var fi = e.GetType().GetField(e.ToString());
        var desc = fi?.GetCustomAttribute<DescriptionAttribute>(false);
        return desc?.Description ?? e.ToString();
    }

    private string EffectiveAriaLabel => AriaLabel ?? (Dot ? "" : Display);

    // Star/Hexagon/Diamond/Shield/Burst/Ribbon are drawn as an SVG path; the rest are CSS boxes.
    private bool IsSvgShape => Shape is Shape.Star or Shape.Hexagon or Shape.Diamond
        or Shape.Shield or Shape.Burst or Shape.Ribbon;

    private string PlacementValue => Placement switch
    {
        Placement.TopEnd => "top-end",
        Placement.TopStart => "top-start",
        Placement.BottomEnd => "bottom-end",
        Placement.BottomStart => "bottom-start",
        Placement.TopCenter => "top-center",
        Placement.BottomCenter => "bottom-center",
        _ => "top-end",
    };

    private string ShapeValue => Shape switch
    {
        Shape.Pill => "pill",
        Shape.Circle => "circle",
        Shape.Square => "square",
        Shape.Rounded => "rounded",
        Shape.Star => "star",
        Shape.Hexagon => "hexagon",
        Shape.Diamond => "diamond",
        Shape.Shield => "shield",
        Shape.Burst => "burst",
        Shape.Ribbon => "ribbon",
        _ => "pill",
    };

    private string VariantValue => Variant switch
    {
        Variant.Info => "info",
        Variant.Success => "success",
        Variant.Warning => "warning",
        Variant.Danger => "danger",
        _ => "default",
    };

    // --- SVG path geometry (viewBox 0 0 100 100, drawn with preserveAspectRatio="none"). ---
    private const string StarPath =
        "M50 4 L61.8 38.2 98 38.2 68.1 60 79.4 94 50 73 20.6 94 31.9 60 2 38.2 38.2 38.2 Z";
    private const string HexagonPath = "M27 4 H73 L98 50 73 96 H27 L2 50 Z";
    private const string DiamondPath = "M50 3 L97 50 50 97 3 50 Z";
    private const string ShieldPath =
        "M50 3 L94 16 V44 C94 70 74 89 50 97 C26 89 6 70 6 44 V16 Z";
    private const string RibbonPath = "M2 24 H98 L86 50 98 76 H2 L14 50 Z";

    private string CurrentPath => Shape switch
    {
        Shape.Star => StarPath,
        Shape.Hexagon => HexagonPath,
        Shape.Diamond => DiamondPath,
        Shape.Shield => ShieldPath,
        Shape.Ribbon => RibbonPath,
        Shape.Burst => BurstPath,
        _ => "",
    };

    // 12-point starburst, shrunk 6% toward centre (50,50) so the uniform stroke isn't clipped.
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

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);

    private string RootStyle => string.Concat(
        Background is null ? "" : $"--sb-bg:{Background};",
        TextColor is null ? "" : $"--sb-color:{TextColor};",
        BorderColor is null ? "" : $"--sb-border:{BorderColor};",
        BorderWidth is null ? "" : $"--sb-border-width:{N(BorderWidth.Value)}px;",
        Size is null ? "" : $"--sb-size:{N(Size.Value)}px;",
        Width is null ? "" : $"--sb-width:{Width};",
        Height is null ? "" : $"--sb-height:{Height};",
        Radius is null ? "" : $"--sb-radius:{N(Radius.Value)}px;",
        Offset is null ? "" : $"--sb-offset:{N(Offset.Value)}px;",
        MaxWidth is null ? "" : $"--sb-max-width:{MaxWidth};");
}
