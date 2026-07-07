using System;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Avatars;

/// <summary>
/// An avatar: a head/shoulders silhouette (filled with a solid color or a gradient) or an image,
/// cropped to a selectable shape (circle, square, rounded, squircle, hexagon) with an optional
/// corner radius. The background behind the figure/image is a solid color or a gradient (with a
/// configurable angle); an optional border draws a ring. No JS.
/// </summary>
public partial class AtomAvatar : AtomComponentBase
{
    /// <summary>Image URL. When set, the image is shown (cropped to the shape) instead of the silhouette.</summary>
    [Parameter] public string? Src { get; set; }

    /// <summary>Alt text for the image / accessible label for the avatar.</summary>
    [Parameter] public string? Alt { get; set; }

    /// <summary>Initials text. When set (and no <see cref="Src"/>), the initials are shown instead of
    /// the silhouette. Usually set for you by <see cref="AtomInitialsAvatar"/>.</summary>
    [Parameter] public string? Initials { get; set; }

    /// <summary>Text color for initials. Sets <c>--av-color</c>. Default white.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Crop shape.</summary>
    [Parameter] public Shape Shape { get; set; } = Shape.Circle;

    /// <summary>Corner radius in px for <see cref="Shape.Rounded"/>. Sets <c>--av-radius</c>.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Avatar size (width = height) in px. Sets <c>--av-size</c>.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Solid background color. Ignored when a background gradient is set. Null = default.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Background gradient start color. Pair with <see cref="BackgroundGradientTo"/>.</summary>
    [Parameter] public string? BackgroundGradientFrom { get; set; }

    /// <summary>Background gradient end color. Pair with <see cref="BackgroundGradientFrom"/>.</summary>
    [Parameter] public string? BackgroundGradientTo { get; set; }

    /// <summary>Background gradient angle in degrees (CSS convention: 0 = up). Default 135.</summary>
    [Parameter] public double BackgroundGradientAngle { get; set; } = 135;

    /// <summary>Solid fill color for the silhouette. Ignored when a figure gradient is set.</summary>
    [Parameter] public string? FigureColor { get; set; }

    /// <summary>Silhouette gradient start color. Pair with <see cref="FigureGradientTo"/>.</summary>
    [Parameter] public string? FigureGradientFrom { get; set; }

    /// <summary>Silhouette gradient end color. Pair with <see cref="FigureGradientFrom"/>.</summary>
    [Parameter] public string? FigureGradientTo { get; set; }

    /// <summary>Silhouette gradient angle in degrees (SVG rotation about the center). Default 135.</summary>
    [Parameter] public double FigureGradientAngle { get; set; } = 135;

    /// <summary>Border (ring) color. Null = no border.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Border (ring) width in px. Default 0.</summary>
    [Parameter] public double? BorderWidth { get; set; }

    // Per-instance gradient id (avoids collisions when several avatars render on one page).
    private readonly string _id = "av" + Guid.NewGuid().ToString("N")[..8];
    private string FigId => "fig-" + _id;

    private bool HasImage => !string.IsNullOrWhiteSpace(Src);
    private bool HasInitials => !string.IsNullOrWhiteSpace(Initials);
    private bool UseBackgroundGradient =>
        !string.IsNullOrWhiteSpace(BackgroundGradientFrom) && !string.IsNullOrWhiteSpace(BackgroundGradientTo);
    private bool UseFigureGradient =>
        !string.IsNullOrWhiteSpace(FigureGradientFrom) && !string.IsNullOrWhiteSpace(FigureGradientTo);

    private string FigureFill => UseFigureGradient ? $"url(#{FigId})" : (FigureColor ?? "#9ca3af");

    private string ShapeValue => Shape switch
    {
        Shape.Circle => "circle",
        Shape.Square => "square",
        Shape.Rounded => "rounded",
        Shape.Squircle => "squircle",
        Shape.Hexagon => "hexagon",
        _ => "circle",
    };

    private string AccessibleLabel => Alt ?? "avatar";

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);

    private string BackgroundCss => UseBackgroundGradient
        ? $"linear-gradient({N(BackgroundGradientAngle)}deg,{BackgroundGradientFrom},{BackgroundGradientTo})"
        : (Background ?? "#e5e7eb");

    private string RootStyle => string.Concat(
        Size is null ? "" : $"--av-size:{N(Size.Value)}px;",
        Radius is null ? "" : $"--av-radius:{N(Radius.Value)}px;",
        TextColor is null ? "" : $"--av-color:{TextColor};",
        $"background:{BackgroundCss};",
        BorderColor is null ? "" : $"border:{N(BorderWidth ?? 1)}px solid {BorderColor};");
}
