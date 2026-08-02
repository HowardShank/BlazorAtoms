using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using BlazorAtoms.DynamicFormWizard.Files;
using BlazorAtoms.DynamicFormWizard.Services;

namespace BlazorAtoms.DynamicFormWizard.Tests;

public class DynamicWizardTests : BunitContext
{
    public enum Priority { Low, High }

    private class ContactInfo
    {
        [Required(ErrorMessage = "Street is required.")]
        public string Street { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
    }

    private class WizardTestModel
    {
        [FormStep(1)]
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [FormStep(1)]
        public Priority Level { get; set; } = Priority.Low;

        [FormStep(2)]
        public ContactInfo Contact { get; set; } = new();

        [FormStep(2)]
        public int Age { get; set; }

        [FormStep(3)]
        public bool AcceptTerms { get; set; }
    }

    [Fact]
    public void Step_1_renders_only_its_own_fields()
    {
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p.Add(c => c.Model, new WizardTestModel()));

        Assert.NotNull(cut.Find("input.wizard-field--text"));
        Assert.NotNull(cut.Find("select.wizard-field--select"));
        Assert.Empty(cut.FindAll("input.wizard-field--number"));
        Assert.Empty(cut.FindAll("fieldset.wizard-field-group"));
        Assert.Contains("Step 1 of 3", cut.Markup);
    }

    [Fact]
    public void Next_is_blocked_and_shows_the_error_when_a_required_field_is_empty()
    {
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p.Add(c => c.Model, new WizardTestModel()));

        cut.Find("button.wizard__button--next").Click();

        Assert.Contains("Name is required.", cut.Markup);
        Assert.Contains("Step 1 of 3", cut.Markup);
    }

    [Fact]
    public void Filling_the_required_field_allows_advancing_to_the_group_and_number_step()
    {
        var model = new WizardTestModel();
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p.Add(c => c.Model, model));

        cut.Find("input.wizard-field--text").Change("Alice");
        cut.Find("button.wizard__button--next").Click();

        Assert.NotNull(cut.Find("fieldset.wizard-field-group"));
        Assert.NotNull(cut.Find("input.wizard-field--number"));
        Assert.Contains("Step 2 of 3", cut.Markup);
    }

    [Fact]
    public void Next_is_blocked_by_a_nested_required_field_left_empty()
    {
        var model = new WizardTestModel();
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p.Add(c => c.Model, model));
        cut.Find("input.wizard-field--text").Change("Alice");
        cut.Find("button.wizard__button--next").Click();
        Assert.Contains("Step 2 of 3", cut.Markup);

        cut.Find("button.wizard__button--next").Click(); // Contact.Street left empty

        Assert.Contains("Street is required.", cut.Markup);
        Assert.Contains("Step 2 of 3", cut.Markup); // still on step 2, not advanced
    }

    [Fact]
    public void Editing_a_nested_group_field_writes_into_the_nested_object_not_the_top_level_model()
    {
        var model = new WizardTestModel();
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p.Add(c => c.Model, model));
        cut.Find("input.wizard-field--text").Change("Alice");
        cut.Find("button.wizard__button--next").Click();

        cut.Find("fieldset.wizard-field-group input.wizard-field--text").Change("Main St");

        Assert.Equal("Main St", model.Contact.Street);
        Assert.Equal("Alice", model.Name); // untouched by the nested edit -- a different owning object
    }

    [Fact]
    public void Final_step_shows_Submit_and_invokes_OnWizardComplete()
    {
        var model = new WizardTestModel();
        WizardTestModel? completed = null;

        var cut = Render<DynamicWizard<WizardTestModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.OnWizardComplete, m => completed = m));

        cut.Find("input.wizard-field--text").Change("Alice");
        cut.Find("button.wizard__button--next").Click();
        cut.Find("fieldset.wizard-field-group input.wizard-field--text").Change("Main St");
        cut.Find("button.wizard__button--next").Click();

        Assert.Contains("Step 3 of 3", cut.Markup);
        Assert.Empty(cut.FindAll("button.wizard__button--next"));
        cut.Find("button.wizard__button--submit").Click();

        Assert.Same(model, completed);
    }

    // Tier 1 (consumer type-registry) worked example, mirroring EXTENSIBILITY.md's Money.
    public record Money(decimal Amount);

    private class MoneyModel
    {
        [FormStep(1)]
        public Money Amount { get; set; } = new(0m);
    }

    private class MoneyStub : ComponentBase
    {
        [Parameter] public Money Value { get; set; } = new(0m);
        [Parameter] public EventCallback<Money> ValueChanged { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "class", "money-stub");
            builder.AddAttribute(2, "value", Value.Amount);
            builder.AddAttribute(3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, async e =>
            {
                if (decimal.TryParse(e.Value?.ToString(), out var amount))
                {
                    await ValueChanged.InvokeAsync(new Money(amount));
                }
            }));
            builder.CloseElement();
        }
    }

    [Fact]
    public void FieldRenderers_registry_uses_the_registered_component_for_its_exact_type()
    {
        var model = new MoneyModel();
        var renderers = new Dictionary<Type, Type> { [typeof(Money)] = typeof(MoneyStub) };

        var cut = Render<DynamicWizard<MoneyModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.FieldRenderers, renderers));

        cut.Find("input.money-stub").Change("42.50");

        Assert.Equal(42.50m, model.Amount.Amount);
    }

    // Tier 4 (fallback) -- a type that is neither a known scalar nor a complex class (Guid is a
    // struct, so WizardTypeInspection.IsComplexType's IsClass check correctly excludes it too).
    private class FallbackModel
    {
        [FormStep(1)]
        public Guid Id { get; set; } = Guid.Empty;
    }

    [Fact]
    public void An_unhandled_type_renders_a_read_only_fallback_instead_of_disappearing()
    {
        var cut = Render<DynamicWizard<FallbackModel>>(p => p.Add(c => c.Model, new FallbackModel { Id = Guid.Empty }));

        var span = cut.Find("span.wizard-field--unhandled");
        Assert.Equal(Guid.Empty.ToString(), span.TextContent);
    }

    // File uploads (DESIGN-DISCUSSION.md E.14-16): own render branch, bytes copied into a
    // wizard-owned WizardFileAttachment immediately on selection, not a raw IBrowserFile handle.
    private class FileModel
    {
        [FormStep(1)]
        [MaxFileCount(2)]
        public IReadOnlyList<WizardFileAttachment>? Attachments { get; set; }
    }

    [Fact]
    public void Selecting_a_file_reads_its_bytes_into_a_WizardFileAttachment_immediately()
    {
        var model = new FileModel();
        var cut = Render<DynamicWizard<FileModel>>(p => p.Add(c => c.Model, model));

        var inputFile = cut.FindComponent<InputFile>();
        var content = InputFileContent.CreateFromText("hello world", "test.txt");
        inputFile.UploadFiles(content);

        Assert.NotNull(model.Attachments);
        var file = Assert.Single(model.Attachments);
        Assert.Equal("test.txt", file.FileName);
        Assert.Equal("hello world", Encoding.UTF8.GetString(file.Content));
    }

    // FormSelect / FormDynamicSelect dropdowns.
    private class SelectModel
    {
        [FormStep(1)]
        [FormSelect("Red", "Green", "Blue")]
        public string Color { get; set; } = string.Empty;

        [FormStep(1)]
        [FormDynamicSelect("departments")]
        public string Department { get; set; } = string.Empty;
    }

    private class FakeLookupService : IWizardLookupService
    {
        public Task<IReadOnlyDictionary<string, string>> GetOptionsAsync(string providerKey)
        {
            IReadOnlyDictionary<string, string> result = new Dictionary<string, string>
            {
                ["eng"] = "Engineering",
                ["sales"] = "Sales",
            };
            return Task.FromResult(result);
        }
    }

    [Fact]
    public void FormSelect_renders_its_declared_options_and_updates_the_model_on_change()
    {
        var model = new SelectModel();
        var cut = Render<DynamicWizard<SelectModel>>(p => p.Add(c => c.Model, model));

        var select = cut.FindAll("select.wizard-field--select")[0];
        var optionValues = select.QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToList();

        Assert.Contains("Red", optionValues);
        Assert.Contains("Green", optionValues);
        Assert.Contains("Blue", optionValues);

        select.Change("Green");
        Assert.Equal("Green", model.Color);
    }

    [Fact]
    public void FormDynamicSelect_shows_a_disabled_placeholder_when_no_lookup_service_is_registered()
    {
        var cut = Render<DynamicWizard<SelectModel>>(p => p.Add(c => c.Model, new SelectModel()));

        var selects = cut.FindAll("select.wizard-field--select");
        var placeholder = selects.Single(s => s.HasAttribute("disabled"));

        Assert.Contains("Loading", placeholder.TextContent);
    }

    [Fact]
    public void FormDynamicSelect_populates_from_the_registered_lookup_service_and_updates_the_model()
    {
        Services.AddSingleton<IWizardLookupService>(new FakeLookupService());
        var model = new SelectModel();
        var cut = Render<DynamicWizard<SelectModel>>(p => p.Add(c => c.Model, model));

        var selects = cut.FindAll("select.wizard-field--select");
        var departmentSelect = selects.Single(s => !s.HasAttribute("disabled") && s != selects[0]);
        var optionValues = departmentSelect.QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToList();

        Assert.Contains("eng", optionValues);
        Assert.Contains("sales", optionValues);

        departmentSelect.Change("eng");
        Assert.Equal("eng", model.Department);
    }

    // FormLayout bare-CSS grid (DESIGN-DISCUSSION.md F.21).
    private class LayoutModel
    {
        [FormStep(1)]
        public string Untagged { get; set; } = string.Empty;

        [FormStep(1)]
        [FormLayout(6)]
        public string HalfWidth { get; set; } = string.Empty;
    }

    [Fact]
    public void An_untagged_field_gets_no_inline_column_span_style()
    {
        var cut = Render<DynamicWizard<LayoutModel>>(p => p.Add(c => c.Model, new LayoutModel()));

        var rows = cut.FindAll("div.wizard__field-row");
        Assert.Contains(rows, r => string.IsNullOrEmpty(r.GetAttribute("style")));
    }

    [Fact]
    public void A_FormLayout_field_gets_the_column_span_custom_property_inline()
    {
        var cut = Render<DynamicWizard<LayoutModel>>(p => p.Add(c => c.Model, new LayoutModel()));

        var laidOutRow = cut.FindAll("div.wizard__field-row").Single(r => (r.GetAttribute("style") ?? "").Contains("--wizard-column-span"));

        Assert.Contains("--wizard-column-span: 6;", laidOutRow.GetAttribute("style"));
    }
}
