using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Cards;

/// <summary>Shared parameter surface for the hover-reveal card family (<see cref="AtomCardReveal"/>
/// and its siblings). Each card is its own component rather than one component with an effect enum,
/// because each brings a parameter the others cannot use (a reveal size, a flip axis, an expand
/// scale, a curl size) — folding them together would put params on the type that are silently
/// invalid for most values, with no compile-time guard. What they genuinely share lives here.
/// <para>The shared CSS custom properties use a plain <c>--atom-card-*</c> prefix (not a
/// per-component one) since they mean the same thing in every card; effect-specific properties keep
/// their own component prefix.</para></summary>
public abstract class AtomCardBase : AtomComponentBase
{
    /// <summary>Card heading.</summary>
    [Parameter] public string Title { get; set; } = "";

    /// <summary>Subheading rendered under <see cref="Title"/>. Supports markup (e.g.
    /// <c>Kingdom: &lt;em&gt;Plantae&lt;/em&gt;</c>). Omitted entirely when null.</summary>
    [Parameter] public RenderFragment? Subtitle { get; set; }

    /// <summary>URL of the card's background image.</summary>
    [Parameter] public string BackgroundImageUrl { get; set; } = "";

    /// <summary>Theme color for the card's face (the overlay/front/sheet behind the background
    /// image), and the default for <see cref="BorderColor"/> and <see cref="DotBorderColor"/>. Any
    /// CSS color.</summary>
    [Parameter] public string AccentColor { get; set; } = "green";

    /// <summary>Thickness of the frame around the card. <c>"0"</c> removes it entirely. Any CSS
    /// length.
    /// <para>Kept separate from <see cref="AccentColor"/> deliberately: the frame width used to be
    /// hardcoded, so the only way to drop the frame was setting the accent to
    /// <c>transparent</c> — which still left the frame's space as a gap AND made the card's face
    /// see-through, breaking the idle state (on <see cref="AtomCardReveal"/> the body panel showed
    /// through before hover).</para></summary>
    [Parameter] public string BorderWidth { get; set; } = "8px";

    /// <summary>Color of the frame around the card. Any CSS color. Null (default) follows
    /// <see cref="AccentColor"/>, which is how the frame behaved before this parameter existed.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Card width. Any CSS length.</summary>
    [Parameter] public string Width { get; set; } = "85vmin";

    /// <summary>Card height. Any CSS length.</summary>
    [Parameter] public string Height { get; set; } = "65vmin";

    /// <summary>Number of dots in the indicator. 0 hides it entirely.</summary>
    [Parameter] public int DotCount { get; set; } = 3;

    /// <summary>Fill color of the indicator dots at rest. Any CSS color.</summary>
    [Parameter] public string DotColor { get; set; } = "yellow";

    /// <summary>Outline color of the indicator dots. Any CSS color. Null (default) follows
    /// <see cref="AccentColor"/>.</summary>
    [Parameter] public string? DotBorderColor { get; set; }

    /// <summary>Fill color of the indicator dots while the card is hovered. Any CSS color.</summary>
    [Parameter] public string DotHoverColor { get; set; } = "#fff";

    /// <summary>Content of the panel revealed on hover.</summary>
    [Parameter] public RenderFragment? BodyContent { get; set; }

    private const double DotBaseDelaySeconds = 1.8;
    private const double DotDelayStepSeconds = 0.3;

    /// <summary>Entrance-animation delay for the dot at <paramref name="index"/>, so the dots
    /// stagger in rather than appearing together.</summary>
    protected static double DotDelaySeconds(int index) => DotBaseDelaySeconds + index * DotDelayStepSeconds;

    /// <summary>Inline <c>animation-delay</c> declaration for the dot at <paramref name="index"/>.</summary>
    protected static string DotDelayStyle(int index) =>
        $"animation-delay:{DotDelaySeconds(index).ToString(System.Globalization.CultureInfo.InvariantCulture)}s;";

    /// <summary>The custom properties every card in the family exposes. Derived components append
    /// their own effect-specific properties.</summary>
    protected string SharedStyleVars =>
        $"--atom-card-width:{Width};" +
        $"--atom-card-height:{Height};" +
        $"--atom-card-accent:{AccentColor};" +
        $"--atom-card-border-width:{BorderWidth};" +
        $"--atom-card-border-color:{BorderColor ?? AccentColor};" +
        $"--atom-card-dot-color:{DotColor};" +
        $"--atom-card-dot-border-color:{DotBorderColor ?? AccentColor};" +
        $"--atom-card-dot-hover-color:{DotHoverColor};";
}
