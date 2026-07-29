using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Buttons;

/// <summary>
/// Shared surface for every component in this package — the four styling axes, the state flags, the
/// click contract, and the <c>--btn-*</c> theming tokens. Same shape as
/// <c>BlazorAtoms.Badges.ChipFamilyBase</c>: a <see cref="Prefix"/> plus a <see cref="Vars"/> hook for
/// per-component tokens, both feeding <see cref="StyleVars"/>.
/// </summary>
/// <remarks>
/// Unlike the chip family, every button component keeps the <b>same</b> <c>btn</c> prefix rather than
/// one per component, so a consumer can retarget <c>--btn-accent</c> once and move the whole family.
/// </remarks>
public abstract class ButtonFamilyBase : AtomComponentBase
{
    /// <summary>CSS custom-property prefix — <c>btn</c> → <c>--btn-accent</c>. Shared by the family.</summary>
    protected virtual string Prefix => "btn";

    /// <summary>Set by an enclosing <see cref="AtomButtonGroup"/>. Supplies the styling axes this
    /// component didn't set for itself.</summary>
    [CascadingParameter] protected ButtonGroupContext? Group { get; set; }

    // ---- styling axes -----------------------------------------------------------------------

    /// <summary>Semantic color scheme → <c>data-variant</c>. Inherited from an enclosing
    /// <see cref="AtomButtonGroup"/> when not set here.</summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Default;

    /// <summary>Fill treatment → <c>data-appearance</c>. Inherited from the group when not set here.</summary>
    [Parameter] public ButtonAppearance Appearance { get; set; } = ButtonAppearance.Solid;

    /// <summary>Density preset → <c>data-size</c>. Inherited from the group when not set here.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Medium;

    /// <summary>Corner treatment → <c>data-shape</c>. Inherited from the group when not set here.</summary>
    [Parameter] public ButtonShape Shape { get; set; } = ButtonShape.Rounded;

    /// <summary>Opt-in CSS effect → <c>data-effect</c>. Not inherited from the group: an effect is a
    /// per-button decision (a group of seven rainbow buttons is nobody's intent).</summary>
    [Parameter] public ButtonEffect Effect { get; set; } = ButtonEffect.None;

    // ---- state ------------------------------------------------------------------------------

    /// <summary>Greys out and blocks the click (native <c>disabled</c>; <c>aria-disabled</c> plus
    /// removed <c>href</c> in link mode, where the platform has no disabled state).</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Swaps the content for a spinner, blocks the click, and reports <c>aria-busy</c>. The
    /// content keeps its space so the button doesn't resize mid-action.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>Stretches to the full width of the container.</summary>
    [Parameter] public bool FullWidth { get; set; }

    // ---- behavior ---------------------------------------------------------------------------

    /// <summary>Native <c>type</c>. Default <see cref="ButtonType.Button"/> — deliberately not HTML's
    /// <c>submit</c> default. Ignored in link mode.</summary>
    [Parameter] public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>When set, renders an <c>&lt;a href&gt;</c> instead of a <c>&lt;button&gt;</c> — same
    /// styling, real navigation semantics (middle-click, open in new tab, copy link).</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>Anchor <c>target</c> (e.g. <c>_blank</c>). Only meaningful with <see cref="Href"/>.</summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>Click handler. Not invoked while <see cref="Disabled"/> or <see cref="Loading"/>.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Accessible name. Required when there is no text content (see
    /// <see cref="AtomIconButton"/>).</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ---- theming (→ --btn-* custom properties) ----------------------------------------------

    /// <summary>Accent/background override → <c>--btn-accent</c>. Null = the
    /// <see cref="Variant"/> default.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color override → <c>--btn-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Border color override → <c>--btn-border-color</c>.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Border thickness in px → <c>--btn-border-width</c>. <c>0</c> removes it.</summary>
    [Parameter] public double? BorderWidth { get; set; }

    /// <summary>Corner radius in px → <c>--btn-radius</c>. Overrides <see cref="Shape"/>.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Fixed height in px → <c>--btn-height</c>. Null = derived from
    /// <see cref="Size"/> padding.</summary>
    [Parameter] public double? Height { get; set; }

    /// <summary>Minimum width in px → <c>--btn-min-width</c>. Useful to stop a
    /// <see cref="Loading"/> label change from resizing a row of buttons.</summary>
    [Parameter] public double? MinWidth { get; set; }

    /// <summary>Font size in px → <c>--btn-font-size</c>. Overrides the <see cref="Size"/> preset.</summary>
    [Parameter] public double? FontSize { get; set; }

    /// <summary>Font family → <c>--btn-font-family</c>. Null = inherit.</summary>
    [Parameter] public string? FontFamily { get; set; }

    /// <summary>Font weight (e.g. <c>600</c>) → <c>--btn-font-weight</c>.</summary>
    [Parameter] public string? FontWeight { get; set; }

    /// <summary>Letter spacing (e.g. <c>.05em</c>) → <c>--btn-letter-spacing</c>.</summary>
    [Parameter] public string? LetterSpacing { get; set; }

    /// <summary>Text transform (e.g. <c>uppercase</c>) → <c>--btn-text-transform</c>.</summary>
    [Parameter] public string? TextTransform { get; set; }

    // ---- group inheritance -------------------------------------------------------------------

    private bool _setVariant, _setAppearance, _setSize, _setShape;

    /// <summary>
    /// Records which styling axes the caller actually supplied, so
    /// <see cref="AtomButtonGroup"/> can fill in the rest. Comparing against the enum defaults instead
    /// would make an explicit <c>Size="Medium"</c> inside a <c>Large</c> group indistinguishable from
    /// not setting it at all.
    /// </summary>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _setVariant = _setAppearance = _setSize = _setShape = false;

        foreach (var p in parameters)
        {
            switch (p.Name)
            {
                case nameof(Variant): _setVariant = true; break;
                case nameof(Appearance): _setAppearance = true; break;
                case nameof(Size): _setSize = true; break;
                case nameof(Shape): _setShape = true; break;
            }
        }

        return base.SetParametersAsync(parameters);
    }

    protected ButtonVariant EffectiveVariant =>
        !_setVariant && Group is not null ? Group.Variant : Variant;

    protected ButtonAppearance EffectiveAppearance =>
        !_setAppearance && Group is not null ? Group.Appearance : Appearance;

    protected ButtonSize EffectiveSize =>
        !_setSize && Group is not null ? Group.Size : Size;

    protected ButtonShape EffectiveShape =>
        !_setShape && Group is not null ? Group.Shape : Shape;

    // ---- derived render state ---------------------------------------------------------------

    /// <summary>True when the control must not act on a click, whichever flag caused it.</summary>
    protected bool IsBlocked => Disabled || Loading;

    /// <summary>True when rendering an anchor rather than a button.</summary>
    protected bool IsLink => !string.IsNullOrEmpty(Href);

    protected string VariantAttr => Kebab(EffectiveVariant.ToString());
    protected string AppearanceAttr => Kebab(EffectiveAppearance.ToString());
    protected string SizeAttr => Kebab(EffectiveSize.ToString());
    protected string ShapeAttr => Kebab(EffectiveShape.ToString());

    /// <summary>Null for <see cref="ButtonEffect.None"/> so the default emits no attribute.</summary>
    protected string? EffectAttr => Effect == ButtonEffect.None ? null : Kebab(Effect.ToString());

    /// <summary>Value for <c>data-state</c>: disabled wins over loading; null in the normal state.</summary>
    protected string? State => Disabled ? "disabled" : Loading ? "loading" : null;

    protected string TypeAttr => Type switch
    {
        ButtonType.Submit => "submit",
        ButtonType.Reset => "reset",
        _ => "button",
    };

    /// <summary>Hook for a component to add its own tokens to the shared style builder.</summary>
    protected virtual StyleVars Vars(StyleVars s) => s;

    /// <summary>Root inline style — visibility, the shared <c>--btn-*</c> tokens, and any per-component
    /// extras from <see cref="Vars"/>.</summary>
    protected string? RootStyle
    {
        get
        {
            var vars = Vars(new StyleVars(Prefix)
                .Add("accent", Background)
                .Add("color", TextColor)
                .Add("border-color", BorderColor)
                .Add("border-width", BorderWidth)
                .Add("radius", Radius)
                .Add("height", Height)
                .Add("min-width", MinWidth)
                .Add("font-size", FontSize)
                .Add("font-family", FontFamily)
                .Add("font-weight", FontWeight)
                .Add("letter-spacing", LetterSpacing)
                .Add("text-transform", TextTransform)).ToString();

            var s = (Visible ? "" : "display:none;") + vars;
            return string.IsNullOrEmpty(s) ? null : s;
        }
    }

    /// <summary>
    /// PascalCase enum member → kebab-case attribute value (<c>GradientBorder</c> →
    /// <c>gradient-border</c>), so multi-word members read as ordinary CSS attribute selectors.
    /// </summary>
    /// <remarks>
    /// The start of a digit run is a word boundary too, which is what makes <c>Press3d</c> render as
    /// <c>press-3d</c> rather than <c>press3d</c>. Digits inside the run, and letters that follow one,
    /// stay attached — so it is <c>press-3d</c>, not <c>press-3-d</c>.
    /// </remarks>
    internal static string Kebab(string pascal)
    {
        var sb = new System.Text.StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            var c = pascal[i];
            if (i > 0)
            {
                var startsDigitRun = char.IsDigit(c) && !char.IsDigit(pascal[i - 1]);
                if (char.IsUpper(c) || startsDigitRun) sb.Append('-');
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
