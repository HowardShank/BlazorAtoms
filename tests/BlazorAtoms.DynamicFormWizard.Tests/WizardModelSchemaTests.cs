using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BlazorAtoms.DynamicFormWizard.Tests;

public class WizardModelSchemaTests
{
    private class Probe
    {
        // No FormStep -> defaults to step 1. No Display -> label falls back to property name.
        public string Untagged { get; set; } = string.Empty;

        [FormStep(2)]
        [FormOrder(1)]
        [Display(Name = "First Field")]
        public string First { get; set; } = string.Empty;

        [FormStep(2, "Contact Info")]
        [FormOrder(2)]
        [FormLayout(6)]
        public string Second { get; set; } = string.Empty;

        // No FormOrder -> sorts after First/Second within step 2, by encounter order.
        [FormStep(2)]
        [DependsOn(nameof(Untagged), "x")]
        [Required]
        public string Dependent { get; set; } = string.Empty;
    }

    [Fact]
    public void An_untagged_property_defaults_to_step_1()
    {
        var schema = WizardModelSchema.For<Probe>();
        var step1 = schema.Steps.Single(s => s.StepNumber == 1);

        Assert.Contains(step1.Properties, p => p.Property.Name == nameof(Probe.Untagged));
    }

    [Fact]
    public void Label_falls_back_to_the_property_name_when_no_Display_is_set()
    {
        var schema = WizardModelSchema.For<Probe>();
        var untagged = schema.TryGetByName(nameof(Probe.Untagged))!;

        Assert.Equal(nameof(Probe.Untagged), untagged.Label);
    }

    [Fact]
    public void Display_Name_overrides_the_label_when_set()
    {
        var schema = WizardModelSchema.For<Probe>();
        var first = schema.TryGetByName(nameof(Probe.First))!;

        Assert.Equal("First Field", first.Label);
    }

    [Fact]
    public void Steps_are_ordered_by_step_number()
    {
        var schema = WizardModelSchema.For<Probe>();

        Assert.Equal([1, 2], schema.Steps.Select(s => s.StepNumber));
    }

    [Fact]
    public void Properties_within_a_step_sort_by_FormOrder_then_by_encounter_order()
    {
        var schema = WizardModelSchema.For<Probe>();
        var step2 = schema.Steps.Single(s => s.StepNumber == 2);

        Assert.Equal(
            [nameof(Probe.First), nameof(Probe.Second), nameof(Probe.Dependent)],
            step2.Properties.Select(p => p.Property.Name));
    }

    [Fact]
    public void FormStep_title_is_captured_only_on_the_property_that_declared_it()
    {
        var schema = WizardModelSchema.For<Probe>();

        Assert.Null(schema.TryGetByName(nameof(Probe.First))!.StepTitle);
        Assert.Equal("Contact Info", schema.TryGetByName(nameof(Probe.Second))!.StepTitle);
    }

    [Fact]
    public void FormLayout_span_is_captured_and_clamped_into_1_to_TotalColumns()
    {
        var schema = WizardModelSchema.For<Probe>();

        Assert.Null(schema.TryGetByName(nameof(Probe.First))!.Layout);
        var layout = schema.TryGetByName(nameof(Probe.Second))!.Layout!;
        Assert.Equal(6, layout.Span);
        Assert.Equal(12, layout.TotalColumns);
    }

    [Fact]
    public void DependsOn_attributes_are_captured_on_the_dependent_property()
    {
        var schema = WizardModelSchema.For<Probe>();
        var dependent = schema.TryGetByName(nameof(Probe.Dependent))!;

        var dep = Assert.Single(dependent.Dependencies);
        Assert.Equal(nameof(Probe.Untagged), dep.TargetProperty);
        Assert.Equal("x", dep.ExpectedValue);
    }

    [Fact]
    public void ValidationAttributes_are_captured_for_reuse_without_re_reflecting()
    {
        var schema = WizardModelSchema.For<Probe>();
        var dependent = schema.TryGetByName(nameof(Probe.Dependent))!;

        Assert.Contains(dependent.Validators, v => v is RequiredAttribute);
    }

    [Fact]
    public void The_schema_is_built_once_and_cached_per_type()
    {
        var first = WizardModelSchema.For<Probe>();
        var second = WizardModelSchema.For<Probe>();

        Assert.Same(first, second);
    }

    [Fact]
    public void TryGetByName_returns_null_for_an_unknown_property()
    {
        var schema = WizardModelSchema.For<Probe>();

        Assert.Null(schema.TryGetByName("DoesNotExist"));
    }

    private class SkippedStepModel
    {
        [FormStep(1)]
        public string First { get; set; } = string.Empty;

        // No property anywhere declares FormStep(2).
        [FormStep(3)]
        public string Third { get; set; } = string.Empty;
    }

    [Fact]
    public void A_skipped_step_number_never_appears_in_Steps()
    {
        var schema = WizardModelSchema.For<SkippedStepModel>();

        Assert.Equal([1, 3], schema.Steps.Select(s => s.StepNumber));
    }
}
