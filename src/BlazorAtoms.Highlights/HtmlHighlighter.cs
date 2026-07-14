using System.Text;
using System.Text.RegularExpressions;

namespace BlazorAtoms.Highlights;

/// <summary>
/// Dependency-free HTML text highlighter. Scans an HTML string and wraps matching
/// terms in <c>&lt;mark class="atom-highlight"&gt;</c> elements, but only inside text
/// content — tag names, attribute values, comments, and the contents of
/// <c>&lt;script&gt;</c>/<c>&lt;style&gt;</c> blocks are left untouched.
/// <para>
/// This is the engine behind the Blazor-native deep highlighter: the component renders
/// the returned string as a <see cref="Microsoft.AspNetCore.Components.MarkupString"/> it
/// fully owns, so highlighting is produced during Blazor's own render pass and therefore
/// survives re-renders with no JavaScript and no post-render DOM manipulation.
/// </para>
/// </summary>
public static class HtmlHighlighter
{
    private const string HighlightClass = "atom-highlight";

    // Elements whose text content must never be highlighted (raw-text / scripting elements).
    private static readonly HashSet<string> SkippedElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "textarea", "title",
    };

    /// <summary>
    /// Returns <paramref name="html"/> with every match of <paramref name="regex"/> that falls
    /// inside text content wrapped in a highlight <c>&lt;mark&gt;</c>. Markup is preserved verbatim.
    /// </summary>
    /// <param name="html">Trusted HTML markup to highlight. Rendered as-is; do not pass untrusted input.</param>
    /// <param name="regex">Compiled term matcher. When <see langword="null"/> the input is returned unchanged.</param>
    /// <param name="style">Value written to the <c>data-style</c> attribute of each mark (mark/underline/outline).</param>
    public static string Highlight(string? html, Regex? regex, string style)
    {
        if (string.IsNullOrEmpty(html) || regex is null)
        {
            return html ?? string.Empty;
        }

        var result = new StringBuilder(html.Length + 64);
        int i = 0;
        int length = html.Length;

        // Name of the raw-text element we are currently inside, if any (e.g. "script").
        string? rawTextElement = null;

        while (i < length)
        {
            char c = html[i];

            if (c == '<')
            {
                // Comment: <!-- ... -->
                if (Matches(html, i, "<!--"))
                {
                    int end = html.IndexOf("-->", i + 4, StringComparison.Ordinal);
                    end = end < 0 ? length : end + 3;
                    result.Append(html, i, end - i);
                    i = end;
                    continue;
                }

                // Declaration / processing instruction: <! ...> or <? ...>
                if (i + 1 < length && (html[i + 1] == '!' || html[i + 1] == '?'))
                {
                    int end = html.IndexOf('>', i + 1);
                    end = end < 0 ? length : end + 1;
                    result.Append(html, i, end - i);
                    i = end;
                    continue;
                }

                // A tag: copy it verbatim and track raw-text element open/close.
                int tagEnd = html.IndexOf('>', i);
                if (tagEnd < 0)
                {
                    // Malformed trailing '<': emit the rest as-is and stop.
                    result.Append(html, i, length - i);
                    break;
                }

                string tag = html.Substring(i, tagEnd - i + 1);
                result.Append(tag);

                string? tagName = GetTagName(tag, out bool isClosing);
                if (tagName is not null && SkippedElements.Contains(tagName))
                {
                    if (isClosing)
                    {
                        if (string.Equals(rawTextElement, tagName, StringComparison.OrdinalIgnoreCase))
                        {
                            rawTextElement = null;
                        }
                    }
                    else if (!tag.EndsWith("/>", StringComparison.Ordinal))
                    {
                        rawTextElement = tagName;
                    }
                }

                i = tagEnd + 1;
                continue;
            }

            // Text run up to the next tag.
            int nextTag = html.IndexOf('<', i);
            if (nextTag < 0) nextTag = length;
            string text = html.Substring(i, nextTag - i);

            if (rawTextElement is not null)
            {
                // Inside <script>/<style>/etc.: never highlight, copy verbatim.
                result.Append(text);
            }
            else
            {
                AppendHighlightedText(result, text, regex, style);
            }

            i = nextTag;
        }

        return result.ToString();
    }

    private static void AppendHighlightedText(StringBuilder result, string text, Regex regex, string style)
    {
        if (text.Length == 0) return;

        // Decode entities so terms match human-readable text, then re-encode when emitting.
        // We match against the raw text (entities are rare inside plain words) but always
        // HTML-encode the pieces we write so the output stays valid and injection-safe.
        var matches = regex.Matches(text);
        if (matches.Count == 0)
        {
            AppendEncoded(result, text);
            return;
        }

        int pos = 0;
        foreach (Match match in matches)
        {
            if (match.Length == 0) continue;

            if (match.Index > pos)
            {
                AppendEncoded(result, text.AsSpan(pos, match.Index - pos));
            }

            result.Append("<mark class=\"").Append(HighlightClass)
                  .Append("\" data-style=\"").Append(style).Append("\">");
            AppendEncoded(result, match.Value.AsSpan());
            result.Append("</mark>");

            pos = match.Index + match.Length;
        }

        if (pos < text.Length)
        {
            AppendEncoded(result, text.AsSpan(pos));
        }
    }

    private static void AppendEncoded(StringBuilder result, string text) =>
        AppendEncoded(result, text.AsSpan());

    private static void AppendEncoded(StringBuilder result, ReadOnlySpan<char> text)
    {
        foreach (char ch in text)
        {
            switch (ch)
            {
                case '&': result.Append("&amp;"); break;
                case '<': result.Append("&lt;"); break;
                case '>': result.Append("&gt;"); break;
                default: result.Append(ch); break;
            }
        }
    }

    private static string? GetTagName(string tag, out bool isClosing)
    {
        isClosing = false;
        // tag starts with '<' and ends with '>'.
        int idx = 1;
        if (idx < tag.Length && tag[idx] == '/')
        {
            isClosing = true;
            idx++;
        }

        int start = idx;
        while (idx < tag.Length)
        {
            char ch = tag[idx];
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == ':')
            {
                idx++;
            }
            else
            {
                break;
            }
        }

        return idx > start ? tag.Substring(start, idx - start) : null;
    }

    private static bool Matches(string s, int index, string value) =>
        index + value.Length <= s.Length &&
        string.CompareOrdinal(s, index, value, 0, value.Length) == 0;
}
