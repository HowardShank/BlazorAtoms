using System.Linq;
using System.Reflection;

namespace BlazorAtoms.DynamicFormWizard.Tests;

public class AttributeTests
{
    private class Probe
    {
        [FormStep(1)]
        public string Untitled { get; set; } = string.Empty;

        [FormStep(2, "Contact Info")]
        public string Titled { get; set; } = string.Empty;

        [FormOrder(3)]
        public string Ordered { get; set; } = string.Empty;

        [DependsOn(nameof(Untitled), "A")]
        [DependsOn(nameof(Titled), "B")]
        public string Stacked { get; set; } = string.Empty;

        [FormSelect("One", "Two", "Three")]
        public string Choices { get; set; } = string.Empty;

        [FormSelect]
        public string NoChoices { get; set; } = string.Empty;

        [FormDynamicSelect("api/departments")]
        public string Dynamic { get; set; } = string.Empty;

        [FormPathEnd(nameof(Untitled), "A")]
        [FormPathEnd(nameof(Titled), "B")]
        public string EndsThePath { get; set; } = string.Empty;
    }

    private static PropertyInfo Prop(string name) => typeof(Probe).GetProperty(name)!;

    [Fact]
    public void FormStep_stores_step_number_and_defaults_title_to_null()
    {
        var attr = Prop(nameof(Probe.Untitled)).GetCustomAttribute<FormStepAttribute>()!;
        Assert.Equal(1, attr.StepNumber);
        Assert.Null(attr.Title);
    }

    [Fact]
    public void FormStep_stores_an_explicit_title()
    {
        var attr = Prop(nameof(Probe.Titled)).GetCustomAttribute<FormStepAttribute>()!;
        Assert.Equal(2, attr.StepNumber);
        Assert.Equal("Contact Info", attr.Title);
    }

    [Fact]
    public void FormOrder_stores_the_order_value()
    {
        var attr = Prop(nameof(Probe.Ordered)).GetCustomAttribute<FormOrderAttribute>()!;
        Assert.Equal(3, attr.Order);
    }

    [Fact]
    public void DependsOn_allows_multiple_and_ANDs_them_together()
    {
        var attrs = Prop(nameof(Probe.Stacked)).GetCustomAttributes<DependsOnAttribute>().ToList();

        Assert.Equal(2, attrs.Count);
        Assert.Contains(attrs, a => a.TargetProperty == nameof(Probe.Untitled) && a.ExpectedValue.Equals("A"));
        Assert.Contains(attrs, a => a.TargetProperty == nameof(Probe.Titled) && a.ExpectedValue.Equals("B"));
    }

    [Fact]
    public void FormSelect_stores_the_given_options()
    {
        var attr = Prop(nameof(Probe.Choices)).GetCustomAttribute<FormSelectAttribute>()!;
        Assert.Equal(["One", "Two", "Three"], attr.Options);
    }

    [Fact]
    public void FormSelect_with_no_options_is_an_empty_array_not_null()
    {
        var attr = Prop(nameof(Probe.NoChoices)).GetCustomAttribute<FormSelectAttribute>()!;
        Assert.NotNull(attr.Options);
        Assert.Empty(attr.Options);
    }

    [Fact]
    public void FormDynamicSelect_stores_the_provider_key()
    {
        var attr = Prop(nameof(Probe.Dynamic)).GetCustomAttribute<FormDynamicSelectAttribute>()!;
        Assert.Equal("api/departments", attr.ProviderKey);
    }

    [Fact]
    public void FormPathEnd_allows_multiple_and_ANDs_them_together()
    {
        var attrs = Prop(nameof(Probe.EndsThePath)).GetCustomAttributes<FormPathEndAttribute>().ToList();

        Assert.Equal(2, attrs.Count);
        Assert.Contains(attrs, a => a.TargetProperty == nameof(Probe.Untitled) && a.ExpectedValue.Equals("A"));
        Assert.Contains(attrs, a => a.TargetProperty == nameof(Probe.Titled) && a.ExpectedValue.Equals("B"));
    }

    [Theory]
    [InlineData(6, 12, 6)]
    [InlineData(0, 12, 1)]   // clamps up to the minimum
    [InlineData(99, 12, 12)] // clamps down to TotalColumns
    public void FormLayout_clamps_span_into_1_to_TotalColumns(int span, int totalColumns, int expectedSpan)
    {
        var attr = new FormLayoutAttribute(span, totalColumns);

        Assert.Equal(expectedSpan, attr.Span);
        Assert.Equal(totalColumns, attr.TotalColumns);
    }
}
