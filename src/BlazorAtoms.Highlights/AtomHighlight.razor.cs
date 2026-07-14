using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Highlights;

/// <summary>
/// Light, zero-JS text highlighter. Use this when the wrapped content is — or should be
/// treated as — plain text: the component renders matching spans as &lt;mark&gt; elements
/// during Blazor render, so it works in every render mode with no JavaScript.
/// </summary>
public partial class AtomHighlight : AtomComponentBase
{
    /// <summary>The child content whose text should be highlighted. Required.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

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

    private RenderFragment? _renderContent;

    protected override void OnParametersSet()
    {
        var text = ChildContent.AsText();
        var terms = GetTerms();

        if (terms.Count == 0 || string.IsNullOrEmpty(text))
        {
            // Nothing to highlight: render the original child content unchanged.
            _renderContent = ChildContent;
            return;
        }

        var regex = BuildRegex();
        var style = StyleValue;
        _renderContent = builder => RenderHighlighted(builder, text, regex, style);
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

    private Regex BuildRegex()
    {
        var terms = GetTerms();
        if (terms.Count == 0)
        {
            return new Regex("(?!)"); // never matches
        }

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

    private static void RenderHighlighted(RenderTreeBuilder builder, string text, Regex regex, string style)
    {
        var matches = regex.Matches(text).Cast<Match>().ToList();
        if (matches.Count == 0)
        {
            builder.AddContent(0, text);
            return;
        }

        int pos = 0;
        int seq = 0;
        foreach (var match in matches)
        {
            if (match.Index > pos)
            {
                builder.AddContent(seq++, text[pos..match.Index]);
            }

            builder.OpenElement(seq++, "mark");
            builder.AddAttribute(seq++, "class", "atom-highlight");
            builder.AddAttribute(seq++, "data-style", style);
            builder.AddContent(seq++, match.Value);
            builder.CloseElement();

            pos = match.Index + match.Length;
        }

        if (pos < text.Length)
        {
            builder.AddContent(seq++, text[pos..]);
        }
    }
}
