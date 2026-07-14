using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace BlazorAtoms.Highlights;

/// <summary>
/// Helpers for inspecting <see cref="RenderFragment"/> output. Used by the light
/// highlighter to extract plain text from a fragment without JavaScript.
/// </summary>
internal static partial class RenderFragmentExtensions
{
    // Matches tags and HTML entities when stripping markup content.
    [GeneratedRegex(@"<[^>]+>|&\w+;|&#\d+;")]
    private static partial Regex TagOrEntityRegex();

    /// <summary>
    /// Renders a fragment into a temporary render tree and concatenates plain
    /// text from text and markup frames, ignoring elements, components, and regions.
    /// </summary>
    public static string AsText(this RenderFragment fragment)
    {
        var builder = new RenderTreeBuilder();
        fragment(builder);

#pragma warning disable BL0006 // RenderTreeFrame is internal by design; used here only to extract plain text from a RenderFragment for the zero-JS highlighter.
        var (frames, count) = GetFrames(builder);

        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            if (frames[i].FrameType == RenderTreeFrameType.Text)
            {
                sb.Append(frames[i].TextContent);
            }
            else if (frames[i].FrameType == RenderTreeFrameType.Markup)
            {
                sb.Append(TagOrEntityRegex().Replace(frames[i].MarkupContent, string.Empty));
            }
        }
#pragma warning restore BL0006

        return sb.ToString();
    }

    private static (RenderTreeFrame[] Array, int Count) GetFrames(RenderTreeBuilder builder)
    {
        // RenderTreeBuilder.GetFrames is internal; access it via reflection so this
        // library can stay free of runtime dependencies on ASP.NET internals.
        var method = typeof(RenderTreeBuilder).GetMethod(
            "GetFrames",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (method == null)
        {
            return ([], 0);
        }

        var range = method.Invoke(builder, null)!;
        var type = range.GetType();
        var array = (RenderTreeFrame[]?)type.GetField("Array")?.GetValue(range) ?? [];
        var count = (int?)type.GetField("Count")?.GetValue(range) ?? 0;
        return (array, count);
    }
}
