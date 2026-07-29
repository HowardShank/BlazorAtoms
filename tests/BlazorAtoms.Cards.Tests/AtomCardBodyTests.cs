using Bunit;
using Xunit;

namespace BlazorAtoms.Cards.Tests;

/// <summary>bUnit coverage for <see cref="AtomCardBody"/>. Purely declarative — no JS interop.</summary>
public class AtomCardBodyTests
{
    [Fact]
    public void Renders_its_content()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardBody>(p => p.AddChildContent("<p>text</p>"));

        Assert.Contains("text", cut.Find(".atom-card-body").TextContent);
    }

    [Fact]
    public void Not_scrollable_by_default_and_adds_nothing_to_the_tab_order()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardBody>();

        var body = cut.Find(".atom-card-body");
        Assert.Null(body.GetAttribute("data-scrollable"));
        Assert.Null(body.GetAttribute("tabindex"));
    }

    [Fact]
    public void Scrollable_marks_the_body_and_makes_it_keyboard_reachable()
    {
        using var ctx = new BunitContext();

        // A scroll container that can't be focused is unreachable without a mouse.
        var cut = ctx.Render<AtomCardBody>(p => p.Add(x => x.Scrollable, true));

        var body = cut.Find(".atom-card-body");
        Assert.Equal("true", body.GetAttribute("data-scrollable"));
        Assert.Equal("0", body.GetAttribute("tabindex"));
    }

    [Fact]
    public void MaxHeight_is_applied_inline_and_is_independent_of_Scrollable()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardBody>(p => p.Add(x => x.MaxHeight, "10rem"));

        Assert.Contains("max-height:10rem", cut.Find(".atom-card-body").GetAttribute("style"));
        Assert.Null(cut.Find(".atom-card-body").GetAttribute("data-scrollable"));
    }

    [Fact]
    public void Section_styling_parameters_come_before_the_bodys_own_declarations()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardBody>(p => p
            .Add(x => x.Padding, 6d)
            .Add(x => x.MaxHeight, "8rem"));

        var style = cut.Find(".atom-card-body").GetAttribute("style") ?? "";
        Assert.Contains("--card-section-padding:6px", style);
        Assert.True(style.IndexOf("max-height") > style.IndexOf("--card-section-padding"));
    }

    [Fact]
    public void Body_has_no_divider_attribute_of_its_own()
    {
        using var ctx = new BunitContext();

        // The header and footer own the rules; the body is what they separate.
        var cut = ctx.Render<AtomCardBody>();

        Assert.Null(cut.Find(".atom-card-body").GetAttribute("data-divider"));
    }

    [Fact]
    public void Body_does_not_declare_a_Divider_parameter_at_all()
    {
        // It draws no rule, so the parameter is absent rather than present-and-ignored. Reflection
        // because the point of the test is the *absence* of a member no markup could exercise.
        var divider = typeof(AtomCardBody).GetProperty("Divider");

        Assert.Null(divider);
        // The two sections that do draw a rule still have it.
        Assert.NotNull(typeof(AtomCardHeader).GetProperty("Divider"));
        Assert.NotNull(typeof(AtomCardFooter).GetProperty("Divider"));
    }

    [Fact]
    public void BackgroundColor_becomes_a_section_custom_property()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardBody>(p => p.Add(x => x.BackgroundColor, "#fff8e1"));

        Assert.Contains("--card-section-bg:#fff8e1", cut.Find(".atom-card-body").GetAttribute("style"));
    }
}
