using System;
using System.Linq;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Avatars;

/// <summary>
/// An avatar that shows a person's initials on a colored background. The initials are derived from
/// <see cref="Name"/> (or given explicitly via <see cref="Initials"/>), and — unless a background is
/// supplied — the background color is picked deterministically from a palette by hashing the name,
/// so the same name always gets the same color. Shape, size, border and gradients pass through to
/// the underlying <see cref="AtomAvatar"/>.
/// </summary>
public partial class AtomInitialsAvatar : AtomComponentBase
{
    /// <summary>Full name the initials are derived from (e.g. "Ada Lovelace" → "AL").</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>Explicit initials, overriding derivation from <see cref="Name"/> (not truncated).</summary>
    [Parameter] public string? Initials { get; set; }

    /// <summary>Max number of derived initials. Default 2. Ignored when <see cref="Initials"/> is set.</summary>
    [Parameter] public int MaxInitials { get; set; } = 2;

    /// <summary>Crop shape.</summary>
    [Parameter] public Shape Shape { get; set; } = Shape.Circle;

    /// <summary>Corner radius in px for <see cref="Shape.Rounded"/>.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Avatar size (width = height) in px.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Text color for the initials. Default white.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Explicit solid background. Overrides the auto palette color.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Background gradient start (overrides auto color when paired with <see cref="BackgroundGradientTo"/>).</summary>
    [Parameter] public string? BackgroundGradientFrom { get; set; }

    /// <summary>Background gradient end.</summary>
    [Parameter] public string? BackgroundGradientTo { get; set; }

    /// <summary>Background gradient angle (deg).</summary>
    [Parameter] public double BackgroundGradientAngle { get; set; } = 135;

    /// <summary>Border (ring) color.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Border (ring) width in px.</summary>
    [Parameter] public double? BorderWidth { get; set; }

    // Pleasant, reasonably-saturated palette; white text reads on all of these.
    private static readonly string[] Palette =
    {
        "#ef4444", "#f97316", "#f59e0b", "#16a34a", "#0d9488",
        "#0ea5e9", "#2563eb", "#7c3aed", "#c026d3", "#db2777",
    };

    private bool UseGradient =>
        !string.IsNullOrWhiteSpace(BackgroundGradientFrom) && !string.IsNullOrWhiteSpace(BackgroundGradientTo);

    /// <summary>The initials actually shown: explicit override, else derived from the name.</summary>
    private string ComputedInitials
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Initials)) return Initials.Trim();
            if (string.IsNullOrWhiteSpace(Name)) return "";

            var words = Name.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            string result;
            if (words.Length == 1)
                result = words[0].Length >= MaxInitials ? words[0][..MaxInitials] : words[0];
            else
                result = string.Concat(words.Take(MaxInitials).Select(w => w[0]));
            return result.ToUpperInvariant();
        }
    }

    // Auto background from a stable hash of the name (only when no explicit background/gradient set).
    private string? AutoBackground
    {
        get
        {
            if (Background is not null || UseGradient) return null;
            var key = string.IsNullOrWhiteSpace(Name) ? ComputedInitials : Name;
            if (string.IsNullOrEmpty(key)) return Palette[0];
            var hash = 0;
            foreach (var ch in key) hash = (hash * 31 + ch) & 0x7fffffff;
            return Palette[hash % Palette.Length];
        }
    }

    private string? EffectiveBackground => Background ?? AutoBackground;

    private string EffectiveAlt => Name ?? ComputedInitials;
}
