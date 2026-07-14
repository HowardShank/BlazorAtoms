using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Highlights;

/// <summary>
/// Deep, Blazor-native text highlighter for rich HTML content (mixed markup such as
/// headings, paragraphs, lists, tables, and links). Unlike <see cref="AtomHighlight"/>,
/// which treats its child content as plain text, this component highlights matches inside
/// the <em>text content</em> of arbitrary markup while leaving tags and attributes intact.
/// <para>
/// Highlighting is produced during Blazor''s own render pass and emitted as a
/// <see cref="MarkupString"/> the component fully owns - there is no JavaScript and no
/// post-render DOM manipulation, so it is safe across re-renders and every render mode.
/// </para>
/// <para>
/// Content is supplied as an HTML string via <see cref="Html"/> and is rendered as trusted
/// markup. Do not pass untrusted user input.
/// </para>
/// </summary>
public partial class AtomHighlightDeep : AtomComponentBase
{
    /// <summary>The HTML content whose text should be highlighted. Rendered as trusted markup. Required.</summary>
    [Parameter, EditorRequired] public string Html { get; set; } = default!;

    /// <summary>A single search term; merged with <see cref="Terms"/>.</summary>
    [Parameter] public string? Term { get; set; }

    /// <summary>Multiple search terms.</summary>
    [Parameter] public IReadOnlyList<string>? Terms { get; set; }

    /// <summary>Case-sensitive matching. Default: false.</summary>
    [Parameter] public bool CaseSensitive { get; set; }

    /// <summary>Match whole words only. Default: false.</summary>
    [Parameter] public bool WholeWord { get; set; }

    /// <summary>Visual treatment for highlighted matches. Default: Mark.</summary>
    [Parameter] public HighlightStyle HighlightStyle { get; set; } = HighlightStyle.Mark;

    /// <summary>Background color (or underline/outline color). Sets --highlight-bg.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color of highlighted matches. Sets --highlight-color.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Corner radius of highlighted background. Sets --highlight-radius.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Inline padding of highlighted background. Sets --highlight-padding.</summary>
    [Parameter] public string? Padding { get; set; }

    /// <summary>Accessible label for the highlighted region.</summary>
    [Parameter] public string AriaLabel { get; set; } = "Highlighted content";

    private MarkupString _highlighted;

    protected override void OnParametersSet()
    {
        var terms = GetTerms();

        if (terms.Count == 0 || string.IsNullOrEmpty(Html))
        {
            _highlighted = new MarkupString(Html ?? string.Empty);
            return;
        }

        var regex = BuildRegex(terms);
        _highlighted = new MarkupString(HtmlHighlighter.Highlight(Html, regex, StyleValue));
    }

    private string RootStyle => new StyleVars("highlight")
        .Add("bg", Background)
        .Add("color", Color)
        .Add("radius", Radius)
        .Add("padding", Padding)
        .ToString();

    private string StyleValue => HighlightStyle switch
    {
        HighlightStyle.Underline => "underline",
        HighlightStyle.Outline => "outline",
        _ => "mark",
    };

    private IReadOnlyList<string> GetTerms()
    {
        var result = Terms?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(Term) && !result.Contains(Term))
        {
            result.Insert(0, Term);
        }
        return result;
    }

    private Regex BuildRegex(IReadOnlyList<string> terms)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < terms.Count; i++)
        {
            if (i > 0) sb.Append('|');
            sb.Append(Regex.Escape(terms[i]));
        }

        var pattern = WholeWord ? $"\\b(?:{sb})\\b" : sb.ToString();
        var options = CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        return new Regex(pattern, options);
    }
}
