using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Cards;

/// <summary>
/// A plain structural card: a themed surface holding an optional media slot plus
/// <see cref="AtomCardHeader"/>/<see cref="AtomCardBody"/>/<see cref="AtomCardFooter"/> sections.
/// Sections can be supplied either as the <see cref="Header"/>/<see cref="Body"/>/<see cref="Footer"/>
/// slots or nested directly in <see cref="ChildContent"/>. Pure CSS — no JS in any render mode.
/// </summary>
/// <remarks>
/// <para><b>Not part of the hover-reveal family.</b> <see cref="AtomCardReveal"/> and its siblings
/// share <c>AtomCardBase</c>, whose <c>BackgroundImageUrl</c>/<c>DotCount</c>/<c>AccentColor</c> exist
/// to serve a reveal animation; this card has no reveal and would inherit those as dead parameters.
/// Two families, one package.</para>
/// <para><b>Root element follows the semantics:</b> an <c>&lt;a&gt;</c> when <see cref="Href"/> is set
/// (it navigates, so no <c>role="button"</c>), a <c>&lt;button&gt;</c> when only
/// <see cref="OnClick"/> is set (focusable, Enter/Space-activatable), and a plain <c>&lt;div&gt;</c>
/// otherwise. A <c>div</c> with a click handler would be none of those things.</para>
/// </remarks>
public partial class AtomCard : AtomComponentBase
{
    // ---- content -----------------------------------------------------------------------------

    /// <summary>Card content. Nest <see cref="AtomCardHeader"/>/<see cref="AtomCardBody"/>/
    /// <see cref="AtomCardFooter"/> here, or arbitrary markup. Renders between the
    /// <see cref="Body"/> and <see cref="Footer"/> slots.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Shorthand for a nested <see cref="AtomCardHeader"/>.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>Shorthand for a nested <see cref="AtomCardBody"/>.</summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>Shorthand for a nested <see cref="AtomCardFooter"/>.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Edge-to-edge media (an image, a chart, a map) placed per
    /// <see cref="MediaPosition"/> and never padded, so it can bleed to the card's corners.</summary>
    [Parameter] public RenderFragment? Media { get; set; }

    /// <summary>Where <see cref="Media"/> sits. <c>Start</c>/<c>End</c> turn the card into a
    /// horizontal media object. Default <see cref="CardMediaPosition.Top"/>.</summary>
    [Parameter] public CardMediaPosition MediaPosition { get; set; } = CardMediaPosition.Top;

    /// <summary>Width of the media column when <see cref="MediaPosition"/> is <c>Start</c> or
    /// <c>End</c> → <c>--card-media-size</c>. Any CSS length; default <c>33%</c>. Ignored for
    /// <c>Top</c>/<c>Bottom</c>, where the media spans the full width.</summary>
    [Parameter] public string? MediaSize { get; set; }

    // ---- interaction -------------------------------------------------------------------------

    /// <summary>Destination for a whole-card link. When set the root renders as an
    /// <c>&lt;a&gt;</c>.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>Anchor target (e.g. <c>_blank</c>). Only meaningful alongside
    /// <see cref="Href"/>.</summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>Raised when the card is activated. With no <see cref="Href"/> this makes the root a
    /// <c>&lt;button&gt;</c>.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Accessible name for a clickable/linked card — worth setting when the card's own text
    /// doesn't describe the destination.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ---- styling axes ------------------------------------------------------------------------

    /// <summary>Frame treatment → <c>data-variant</c>. Default
    /// <see cref="CardVariant.Elevated"/>.</summary>
    [Parameter] public CardVariant Variant { get; set; } = CardVariant.Elevated;

    /// <summary>Shadow depth → <c>data-elevation</c>. Only <see cref="CardVariant.Elevated"/> draws a
    /// shadow at all. Default <see cref="CardElevation.Medium"/>.</summary>
    [Parameter] public CardElevation Elevation { get; set; } = CardElevation.Medium;

    /// <summary>Opt-in CSS hover/press treatment → <c>data-effect</c>. Default
    /// <see cref="CardEffect.None"/> (no attribute emitted).</summary>
    [Parameter] public CardEffect Effect { get; set; } = CardEffect.None;

    // ---- theming (→ --card-* custom properties) ----------------------------------------------

    /// <summary>Section padding in px, cascaded to every section via <see cref="CardContext"/>. A
    /// section's own <c>Padding</c> still wins.</summary>
    [Parameter] public double? Padding { get; set; }

    /// <summary>Default for every section's divider rule, cascaded via <see cref="CardContext"/>. A
    /// section's own <c>Divider</c> still wins. Default true.</summary>
    [Parameter] public bool Divider { get; set; } = true;

    /// <summary>Corner radius in px → <c>--card-radius</c>.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Border thickness in px → <c>--card-border-width</c>. <c>0</c> removes the frame on
    /// <see cref="CardVariant.Outlined"/>.</summary>
    [Parameter] public double? BorderWidth { get; set; }

    /// <summary>Card width. Any CSS length.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Card height. Any CSS length. Null (default) grows with content.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Card background (any CSS color) → <c>--card-bg</c>.</summary>
    [Parameter] public string? BackgroundColor { get; set; }

    /// <summary>Border color → <c>--card-border-color</c>.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Text color → <c>--card-text-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Accent used by the hover effects and the header's title rule →
    /// <c>--card-accent</c>.</summary>
    [Parameter] public string? AccentColor { get; set; }

    /// <summary>Divider rule color → <c>--card-divider-color</c>.</summary>
    [Parameter] public string? DividerColor { get; set; }

    /// <summary>Duration in seconds for the hover/press transitions →
    /// <c>--card-duration</c>.</summary>
    [Parameter] public double? Duration { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    // ---- derived render state ----------------------------------------------------------------

    private const string BaseClass = "atom-card";

    private string RootClass => BaseClass;

    private string VariantAttr => Kebab(Variant.ToString());

    private string ElevationAttr => Kebab(Elevation.ToString());

    /// <summary>Null for <see cref="CardEffect.None"/> so the default emits no attribute.</summary>
    private string? EffectAttr => Effect == CardEffect.None ? null : Kebab(Effect.ToString());

    /// <summary>Null when there's no media, so the layout rules key off presence as well as side.</summary>
    private string? MediaAttr => Media is null ? null : Kebab(MediaPosition.ToString());

    /// <summary>Rebuilt each render rather than cached: <see cref="Padding"/>/<see cref="Divider"/>
    /// are parameters, and a cached instance would hand sections stale values after a re-render.</summary>
    private CardContext Context => new() { Padding = Padding, Divider = Divider };

    private string? RootStyle
    {
        get
        {
            var vars = new StyleVars("card")
                .Add("radius", Radius)
                .Add("border-width", BorderWidth)
                .Add("bg", BackgroundColor)
                .Add("border-color", BorderColor)
                .Add("text-color", TextColor)
                .Add("accent", AccentColor)
                .Add("divider-color", DividerColor)
                .Add("media-size", MediaSize)
                .Add("duration", Duration is null
                    ? null
                    : Duration.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "s")
                .ToString();

            var s = (Visible ? "" : "display:none;")
                  + vars
                  + (Width is null ? "" : $"width:{Width};")
                  + (Height is null ? "" : $"height:{Height};");

            return string.IsNullOrEmpty(s) ? null : s;
        }
    }

    /// <summary>PascalCase enum name → kebab-case attribute value (<c>HoverLift</c> →
    /// <c>hover-lift</c>).</summary>
    internal static string Kebab(string pascal)
    {
        var sb = new System.Text.StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            var c = pascal[i];
            if (char.IsUpper(c) && i > 0) sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
