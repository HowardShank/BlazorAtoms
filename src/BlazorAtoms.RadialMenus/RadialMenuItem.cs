namespace BlazorAtoms.RadialMenus;

/// <summary>
/// One entry in a radial menu. An item with <see cref="Children"/> is a branch that opens its own
/// ring; an item without is a leaf that raises <c>OnItemInvoked</c>. Nesting is unlimited.
/// </summary>
/// <remarks>
/// Every geometry member is nullable and defaults to "inherit from the menu", so the common case is
/// a label and maybe an icon. The overrides exist for the one item in a menu that needs to sit at a
/// particular angle, or to be a different shape from its siblings.
/// </remarks>
public sealed class RadialMenuItem
{
    /// <summary>Stable identifier for this item, used as the element id suffix and echoed back on
    /// callbacks. Optional — the item's path is used when it is absent.</summary>
    public string? Id { get; init; }

    /// <summary>Text shown on (or beside) the shape, and the item's accessible name.</summary>
    public string? Label { get; init; }

    /// <summary>CSS class for an icon font glyph, rendered inside the shape above the label.
    /// For arbitrary markup use the menu's <c>ItemTemplate</c> instead.</summary>
    public string? Icon { get; init; }

    /// <summary>Tooltip text. Falls back to <see cref="Label"/>, which matters most under
    /// <see cref="RadialMenuLabelPlacement.TooltipOnly"/>.</summary>
    public string? Tooltip { get; init; }

    /// <summary>Rendered but not interactive, and skipped by keyboard navigation.</summary>
    public bool Disabled { get; init; }

    /// <summary>Child items. Non-empty makes this a branch.</summary>
    public IReadOnlyList<RadialMenuItem>? Children { get; init; }

    /// <summary>Whatever the consumer wants to carry through to the invoke callback — a command, a
    /// route, a domain object. The library never inspects it.</summary>
    public object? Data { get; init; }

    /// <summary>Overrides the start of this item's own child arc. Beats <c>ArcMode</c>.</summary>
    public double? StartAngle { get; init; }

    /// <summary>Overrides the end of this item's own child arc. Beats <c>ArcMode</c>.</summary>
    public double? EndAngle { get; init; }

    /// <summary>Overrides the menu's <c>ItemShape</c> for this one item.</summary>
    public RadialMenuShape? Shape { get; init; }

    /// <summary>Extra CSS class on this item's button.</summary>
    public string? CssClass { get; init; }

    /// <summary>True when this item opens a ring of its own rather than raising an action.</summary>
    public bool IsBranch => Children is { Count: > 0 };
}
