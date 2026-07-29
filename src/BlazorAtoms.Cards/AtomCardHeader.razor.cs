using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Cards;

/// <summary>
/// The top section of an <see cref="AtomCard"/>: an optional avatar/icon slot, a title and subtitle,
/// and an end-aligned actions slot. Works standalone as well as nested.
/// </summary>
public partial class AtomCardHeader : AtomCardSectionBase
{
    /// <summary>Heading text. Rendered as a real heading element at
    /// <see cref="HeadingLevel"/>. Ignored when <c>ChildContent</c> is supplied.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Secondary line under the title. Markup is allowed (a link, an <c>&lt;em&gt;</c>).
    /// Ignored when <c>ChildContent</c> is supplied.</summary>
    [Parameter] public RenderFragment? Subtitle { get; set; }

    /// <summary>Heading level for <see cref="Title"/>, 1-6. Default 3.</summary>
    /// <remarks>A real <c>&lt;h*&gt;</c> rather than a styled <c>div</c> with
    /// <c>role="heading"</c>: cards land at different depths on different pages, and the level has to
    /// be the caller's choice for the document outline to stay correct.</remarks>
    [Parameter] public int HeadingLevel { get; set; } = 3;

    /// <summary>Leading slot — an avatar, icon or thumbnail, placed before the text.</summary>
    [Parameter] public RenderFragment? Avatar { get; set; }

    /// <summary>Trailing slot — buttons, a menu, a badge — pushed to the end of the row.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Whether to draw the hairline rule under the header. Null (default) inherits the
    /// enclosing card's setting, then true. Declared here and on <see cref="AtomCardFooter"/> rather
    /// than on the shared base, because <see cref="AtomCardBody"/> has no rule of its own.</summary>
    [Parameter] public bool? Divider { get; set; }

    /// <summary>Built in code rather than markup because the element <i>name</i> varies; a
    /// <c>@switch</c> over six near-identical heading branches in the .razor would say the same thing
    /// at six times the length.</summary>
    private RenderFragment HeadingElement => builder =>
    {
        builder.OpenElement(0, $"h{Math.Clamp(HeadingLevel, 1, 6)}");
        builder.AddAttribute(1, "class", "atom-card-header-title");
        builder.AddContent(2, Title);
        builder.CloseElement();
    };
}
