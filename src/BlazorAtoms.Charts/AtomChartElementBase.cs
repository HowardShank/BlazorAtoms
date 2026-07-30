using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// Base class for the chart element components — the opt-in pieces of chrome a chart renders into one of
/// its slots.
/// </summary>
/// <remarks>
/// <para><b>An element is presentation only.</b> It reads its data and its coordinates from the cascaded
/// <see cref="ChartContext"/> and decides class, style and markup shape. It cannot decide where it is
/// drawn: the chart placed the slot, and the chart computed the coordinates. That split is what keeps the
/// geometry in one place and makes the elements trivial to test.</para>
/// <para><b>Standalone is not an error.</b> Used outside a chart, <see cref="Chart"/> is null and every
/// list an element would read is empty, so it renders its own root and nothing inside it — the same
/// convention as <c>AtomCardSectionBase</c> outside an <c>AtomCard</c>. Throwing would turn a markup
/// mistake into a broken page.</para>
/// <para><b>Its own stylesheet, necessarily.</b> Scoped CSS stamps the scope id of the component that
/// <i>declares</i> the markup, so an element rendered inside a chart's slot carries the element's id and
/// not the chart's — the chart's stylesheet cannot reach it. Each element therefore owns its own
/// <c>.razor.css</c>, and anything a chart needs to communicate across that boundary goes through a
/// <c>--chart-*</c> custom property, which inherits down the DOM regardless of scope.</para>
/// </remarks>
public abstract class AtomChartElementBase : AtomComponentBase
{
    /// <summary>
    /// The owning chart's per-render state, or null when this element is used outside a chart.
    /// </summary>
    /// <remarks>
    /// Cascaded with <c>IsFixed="false"</c> and rebuilt by the chart every render, so a parameter change
    /// on the chart re-renders its elements through Blazor's own change detection. Deliberately not the
    /// <c>AtomTabs</c> approach of cascading <c>this</c> and pushing <c>StateHasChanged</c> into a
    /// registration list: that needs a second render pass, and the gutter has to be reserved on the
    /// first one.
    /// </remarks>
    [CascadingParameter] protected ChartContext? Chart { get; set; }

    /// <summary>Invariant coordinate formatting — see <see cref="ChartNumber.N"/>.</summary>
    protected static string N(double v) => ChartNumber.N(v);

    /// <summary>
    /// Renders a list of <see cref="ChartTextMark"/>s as SVG <c>&lt;text&gt;</c> elements.
    /// </summary>
    /// <remarks>
    /// A builder rather than markup because <b>Razor reserves <c>&lt;text&gt;</c> as a control construct
    /// and rejects attributes on it</b> — <c>RZ1023</c> — so an SVG text element cannot be written in a
    /// <c>.razor</c> file at all. Each mark is wrapped in its own <c>OpenRegion</c> so the sequence
    /// numbers inside stay constant across iterations and the diff behaves like ordinary markup.
    /// </remarks>
    /// <param name="marks">The marks to draw. An empty list renders nothing.</param>
    /// <param name="cssClass">Class for each <c>&lt;text&gt;</c>. The element's own
    /// <c>CssClass</c>/<c>Style</c> go on the wrapping <c>&lt;g&gt;</c>, which the text inherits from.</param>
    protected static RenderFragment TextMarks(IReadOnlyList<ChartTextMark> marks, string cssClass) => builder =>
    {
        for (var i = 0; i < marks.Count; i++)
        {
            var mark = marks[i];

            builder.OpenRegion(i);
            builder.OpenElement(0, "text");
            builder.AddAttribute(1, "class", cssClass);
            builder.AddAttribute(2, "x", N(mark.X));
            builder.AddAttribute(3, "y", N(mark.Y));
            builder.AddAttribute(4, "text-anchor", mark.Anchor);
            builder.AddContent(5, mark.Text);
            builder.CloseElement();
            builder.CloseRegion();
        }
    };

    /// <summary>
    /// Renders a list of <see cref="ChartLine"/>s as SVG <c>&lt;line&gt;</c> elements.
    /// </summary>
    /// <remarks>
    /// <c>&lt;line&gt;</c> is not reserved and could be written as markup, but it is built here anyway so
    /// the gridline and baseline elements stay symmetrical with the text ones — and so no coordinate can
    /// reach the markup without going through <see cref="N"/>.
    /// </remarks>
    protected static RenderFragment Lines(IReadOnlyList<ChartLine> lines, string cssClass) => builder =>
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            builder.OpenRegion(i);
            builder.OpenElement(0, "line");
            builder.AddAttribute(1, "class", cssClass);
            builder.AddAttribute(2, "x1", N(line.X1));
            builder.AddAttribute(3, "y1", N(line.Y1));
            builder.AddAttribute(4, "x2", N(line.X2));
            builder.AddAttribute(5, "y2", N(line.Y2));
            builder.CloseElement();
            builder.CloseRegion();
        }
    };
}
