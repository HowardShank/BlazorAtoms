using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Progress.Tests;

/// <summary>bUnit coverage for <see cref="AtomProgressSteps"/>. Purely declarative — no JS interop.
/// The behavior worth pinning is the status derivation from <c>Current</c>, and the markers switching
/// between inert spans and real buttons depending on whether <c>OnStepClick</c> is wired.</summary>
public class AtomProgressStepsTests
{
    private static readonly string[] Three = ["Cart", "Address", "Payment"];

    [Fact]
    public void One_item_per_step_in_order()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p.Add(x => x.Steps, Three));

        var items = cut.FindAll(".atom-progress-steps-item");
        Assert.Equal(3, items.Count);
        Assert.Contains("Cart", items[0].TextContent);
        Assert.Contains("Payment", items[2].TextContent);
    }

    [Fact]
    public void Null_steps_render_an_empty_list_rather_than_throwing()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>();

        Assert.NotNull(cut.Find(".atom-progress-steps-list"));
        Assert.Empty(cut.FindAll(".atom-progress-steps-item"));
    }

    [Fact]
    public void Current_splits_the_steps_into_complete_active_pending()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Current, 1));

        var items = cut.FindAll(".atom-progress-steps-item");
        Assert.Equal("complete", items[0].GetAttribute("data-status"));
        Assert.Equal("active", items[1].GetAttribute("data-status"));
        Assert.Equal("pending", items[2].GetAttribute("data-status"));
    }

    [Fact]
    public void Aria_current_marks_only_the_active_step()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Current, 1));

        var items = cut.FindAll(".atom-progress-steps-item");
        Assert.Null(items[0].GetAttribute("aria-current"));
        Assert.Equal("step", items[1].GetAttribute("aria-current"));
        Assert.Null(items[2].GetAttribute("aria-current"));
    }

    [Fact]
    public void Current_past_the_end_marks_everything_complete()
    {
        using var ctx = new BunitContext();

        // The "finished" state — no step is active any more.
        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Current, 3));

        Assert.All(cut.FindAll(".atom-progress-steps-item"),
            i => Assert.Equal("complete", i.GetAttribute("data-status")));
    }

    [Fact]
    public void Negative_current_marks_everything_pending()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Current, -1));

        Assert.All(cut.FindAll(".atom-progress-steps-item"),
            i => Assert.Equal("pending", i.GetAttribute("data-status")));
    }

    [Fact]
    public void StatusFor_overrides_the_derived_status_and_is_the_only_route_to_error()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Current, 2)
            .Add(x => x.StatusFor, i => i == 1 ? ProgressStepStatus.Error : ProgressStepStatus.Complete));

        var items = cut.FindAll(".atom-progress-steps-item");
        Assert.Equal("complete", items[0].GetAttribute("data-status"));
        Assert.Equal("error", items[1].GetAttribute("data-status"));
        // Index 2 would be "active" from Current; the override wins.
        Assert.Equal("complete", items[2].GetAttribute("data-status"));
    }

    [Fact]
    public void Number_marker_is_one_based()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p.Add(x => x.Steps, Three));

        var numbers = cut.FindAll(".atom-progress-steps-number");
        Assert.Equal("1", numbers[0].TextContent);
        Assert.Equal("3", numbers[2].TextContent);
    }

    [Fact]
    public void Dot_marker_draws_a_dot_instead_of_a_number()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Marker, ProgressStepMarker.Dot));

        Assert.Equal(3, cut.FindAll(".atom-progress-steps-dot").Count);
        Assert.Empty(cut.FindAll(".atom-progress-steps-number"));
    }

    [Fact]
    public void None_marker_draws_nothing_inside_the_circle()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Marker, ProgressStepMarker.None));

        Assert.Empty(cut.FindAll(".atom-progress-steps-number"));
        Assert.Empty(cut.FindAll(".atom-progress-steps-dot"));
        Assert.Empty(cut.FindAll(".atom-progress-steps-icon"));
        Assert.Equal(3, cut.FindAll(".atom-progress-steps-marker").Count);
    }

    [Fact]
    public void Check_marker_swaps_the_number_for_a_tick_once_complete()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Current, 1)
            .Add(x => x.Marker, ProgressStepMarker.Check));

        var items = cut.FindAll(".atom-progress-steps-item");
        Assert.Single(items[0].QuerySelectorAll(".atom-progress-steps-icon"));
        // Active and pending steps still show their number.
        Assert.Single(items[1].QuerySelectorAll(".atom-progress-steps-number"));
        Assert.Single(items[2].QuerySelectorAll(".atom-progress-steps-number"));
    }

    [Fact]
    public void Error_status_draws_a_cross_whatever_the_marker_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Marker, ProgressStepMarker.Number)
            .Add(x => x.StatusFor, i => i == 0 ? ProgressStepStatus.Error : ProgressStepStatus.Pending));

        var first = cut.FindAll(".atom-progress-steps-item")[0];
        Assert.Single(first.QuerySelectorAll(".atom-progress-steps-icon"));
        Assert.Empty(first.QuerySelectorAll(".atom-progress-steps-number"));
    }

    [Fact]
    public void Without_OnStepClick_markers_are_inert_spans_out_of_the_tab_order()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p.Add(x => x.Steps, Three));

        var marker = cut.Find(".atom-progress-steps-marker");
        Assert.Equal("SPAN", marker.TagName);
        Assert.Equal("true", marker.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void With_OnStepClick_markers_are_real_buttons_that_report_their_index()
    {
        using var ctx = new BunitContext();
        var clicked = new List<int>();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.OnStepClick, EventCallback.Factory.Create<int>(this, i => clicked.Add(i))));

        var markers = cut.FindAll(".atom-progress-steps-marker");
        Assert.Equal("BUTTON", markers[0].TagName);
        Assert.Equal("button", markers[0].GetAttribute("type"));

        markers[2].Click();
        Assert.Equal([2], clicked);
    }

    [Fact]
    public void Clickable_markers_are_named_since_the_caption_is_a_sibling()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Current, 1)
            .Add(x => x.OnStepClick, EventCallback.Factory.Create<int>(this, _ => { })));

        var markers = cut.FindAll(".atom-progress-steps-marker");
        Assert.Equal("Cart (complete)", markers[0].GetAttribute("aria-label"));
        Assert.Equal("Address (active)", markers[1].GetAttribute("aria-label"));
    }

    [Fact]
    public void StepTemplate_replaces_the_plain_caption()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.StepTemplate, (RenderFragment<int>)(i => b => b.AddMarkupContent(0, $"<em>step {i}</em>"))));

        var text = cut.Find(".atom-progress-steps-text");
        Assert.Contains("<em>step 0</em>", text.InnerHtml);
        Assert.DoesNotContain("Cart", cut.Markup);
    }

    [Fact]
    public void ShowValue_renders_a_clamped_position_counter()
    {
        using var ctx = new BunitContext();

        var mid = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Current, 1)
            .Add(x => x.ShowValue, true));
        Assert.Equal("2 of 3", mid.Find(".atom-progress-steps-count").TextContent);

        // Past the end reads as the last step, not "4 of 3".
        var done = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Current, 9)
            .Add(x => x.ShowValue, true));
        Assert.Equal("3 of 3", done.Find(".atom-progress-steps-count").TextContent);

        var empty = ctx.Render<AtomProgressSteps>(p => p.Add(x => x.ShowValue, true));
        Assert.Equal("0 of 0", empty.Find(".atom-progress-steps-count").TextContent);
    }

    [Theory]
    [InlineData(ProgressStepsOrientation.Horizontal, "horizontal")]
    [InlineData(ProgressStepsOrientation.Vertical, "vertical")]
    public void Orientation_is_emitted(ProgressStepsOrientation orientation, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Orientation, orientation));

        Assert.Equal(expected, cut.Find(".atom-progress-steps").GetAttribute("data-orientation"));
    }

    [Fact]
    public void Axes_theming_and_naming_reach_the_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressSteps>(p => p
            .Add(x => x.Steps, Three)
            .Add(x => x.Variant, ProgressVariant.Info)
            .Add(x => x.Size, ProgressSize.Large)
            .Add(x => x.Effect, ProgressEffect.Pulse)
            .Add(x => x.FillColor, "#123456"));

        var root = cut.Find(".atom-progress-steps");
        Assert.Equal("info", root.GetAttribute("data-variant"));
        Assert.Equal("large", root.GetAttribute("data-size"));
        Assert.Equal("pulse", root.GetAttribute("data-effect"));
        Assert.Contains("--progress-fill-color:#123456", root.GetAttribute("style"));
        Assert.Equal("Progress steps", cut.Find(".atom-progress-steps-list").GetAttribute("aria-label"));
    }
}
