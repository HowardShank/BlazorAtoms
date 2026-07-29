using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Boolean checkbox. A native <c>&lt;input type="checkbox"&gt;</c> carries the semantics (focus, tab
/// order, form submission, screen-reader role) while a sibling <c>&lt;span&gt;</c> paints the visible
/// box — the only way to style a checkbox consistently across engines, and what lets
/// <see cref="Indeterminate"/> work without JS (see DEVELOPMENT.md). No JS.
/// </summary>
public partial class AtomCheckbox : AtomInputBase<bool>
{
    /// <summary>Caption rendered beside the box, inside the same <c>&lt;label&gt;</c> — clicking it
    /// toggles the checkbox. Independent of the column-layout <see cref="AtomInputBase{TValue}.Label"/>.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Draws the mixed/partial state (a dash instead of a check) and reports
    /// <c>aria-checked="mixed"</c>. Purely presentational — <see cref="AtomInputBase{TValue}.Value"/>
    /// is still the bound boolean, and clicking clears it the same way it always does.</summary>
    [Parameter] public bool Indeterminate { get; set; }

    /// <summary>Outline shape of the box. Default <see cref="CheckShape.Rounded"/>.</summary>
    [Parameter] public CheckShape BoxShape { get; set; } = CheckShape.Rounded;

    /// <summary>Which side of the box <see cref="Text"/> sits on. Default
    /// <see cref="LabelPlacement.End"/>.</summary>
    [Parameter] public LabelPlacement TextPlacement { get; set; } = LabelPlacement.End;

    /// <summary>Box edge length in px → <c>--field-control-size</c>. Defaults to the
    /// <see cref="AtomInputBase{TValue}.Size"/> preset.</summary>
    [Parameter] public double? BoxSize { get; set; }

    // ---- derived render state ---------------------------------------------------------------

    // SupportsNativeReadOnly stays false: the HTML spec ignores `readonly` on a checkbox, so
    // ReadOnly folds into the native `disabled` attribute (same call AtomRangeInput made).

    protected override string DefaultAriaLabel => "Checkbox";

    private string RootClass => "atom-checkbox";

    private string? RootStyle => BuildRootStyle(new StyleVars("field").Add("control-size", BoxSize).ToString());

    private string ShapeAttr => Kebab(BoxShape.ToString());

    private string PlacementAttr => Kebab(TextPlacement.ToString());

    /// <summary>Only emitted when set, so the normal state carries no attribute.</summary>
    private string? IndeterminateAttr => Indeterminate ? "true" : null;

    /// <summary><c>mixed</c> overrides the native checked state for assistive tech; otherwise the
    /// attribute is omitted and the native <c>checked</c> speaks for itself.</summary>
    private string? AriaCheckedAttr => Indeterminate ? "mixed" : null;

    // ---- interaction --------------------------------------------------------------------------

    /// <summary>A checkbox has no <c>oninput</c> worth honoring — the browser fires <c>change</c> on
    /// toggle — so there is no <c>UpdateOn</c> parameter here.</summary>
    private Task OnChangeEvent(ChangeEventArgs e) =>
        e.Value is bool b ? SetValueAsync(b) : Task.CompletedTask;
}
