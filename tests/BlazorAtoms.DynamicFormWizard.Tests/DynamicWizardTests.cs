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
using BlazorAtoms.DynamicFormWizard.Navigation;
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

    // A nested group type with a public settable indexer (mirrors System.Text.StringBuilder's
    // Chars[int]) -- GetProperties() reports indexers as CanRead/CanWrite too, but GetValue()
    // with no index arguments throws TargetParameterCountException. WizardTypeInspection and the
    // auto-expand enumeration must both exclude indexers, or this crashes instead of rendering
    // the type's ordinary properties.
    private class GroupWithIndexer
    {
        private readonly Dictionary<int, string> _byIndex = new();

        public string Label { get; set; } = string.Empty;

        public string this[int index]
        {
            get => _byIndex.TryGetValue(index, out var v) ? v : string.Empty;
            set => _byIndex[index] = value;
        }
    }

    private class ModelWithIndexerGroup
    {
        [FormStep(1)]
        public GroupWithIndexer Group { get; set; } = new();
    }

    [Fact]
    public void A_nested_type_with_a_public_indexer_renders_its_ordinary_properties_without_crashing()
    {
        var model = new ModelWithIndexerGroup();

        var cut = Render<DynamicWizard<ModelWithIndexerGroup>>(p => p.Add(c => c.Model, model));

        cut.Find("fieldset.wizard-field-group input.wizard-field--text").Change("hello");

        Assert.Equal("hello", model.Group.Label);
        Assert.Empty(cut.FindAll("span.wizard-field--unhandled"));
    }

    // List<T> repeating support (DESIGN-DISCUSSION.md G.25). List<T>'s only public read/write,
    // non-indexer property is Capacity (an int) -- WizardTypeInspection.IsComplexType excludes
    // ALL collection types (so it never misdetects List<T> as a "Capacity" auto-expand group),
    // and DynamicWizard's own tier 1b gives List<T> real handling instead of falling to the tier-4
    // fallback: a repeating row of scalar inputs when the item type is a scalar, tested here.
    private class ModelWithScalarListProperty
    {
        [FormStep(1)]
        public List<string> Tags { get; set; } = new() { "a", "b" };
    }

    [Fact]
    public void A_ListT_of_scalar_renders_a_repeating_row_not_a_fallback_or_a_Capacity_field()
    {
        var model = new ModelWithScalarListProperty();
        var cut = Render<DynamicWizard<ModelWithScalarListProperty>>(p => p.Add(c => c.Model, model));

        Assert.Empty(cut.FindAll("span.wizard-field--unhandled"));
        Assert.Empty(cut.FindAll("fieldset.wizard-field-group"));
        Assert.Equal(2, cut.FindAll("div.wizard-list-repeater__row input").Count);
    }

    [Fact]
    public void Editing_a_scalar_list_row_writes_into_the_correct_list_index()
    {
        var model = new ModelWithScalarListProperty();
        var cut = Render<DynamicWizard<ModelWithScalarListProperty>>(p => p.Add(c => c.Model, model));

        cut.FindAll("div.wizard-list-repeater__row input")[1].Change("bravo");

        Assert.Equal(new[] { "a", "bravo" }, model.Tags);
    }

    [Fact]
    public void Clicking_Add_appends_a_new_scalar_list_row()
    {
        var model = new ModelWithScalarListProperty();
        var cut = Render<DynamicWizard<ModelWithScalarListProperty>>(p => p.Add(c => c.Model, model));

        cut.Find("button.wizard-list-repeater__add").Click();

        Assert.Equal(new[] { "a", "b", "" }, model.Tags);
        Assert.Equal(3, cut.FindAll("div.wizard-list-repeater__row input").Count);
    }

    [Fact]
    public void Clicking_Remove_deletes_that_scalar_list_row()
    {
        var model = new ModelWithScalarListProperty();
        var cut = Render<DynamicWizard<ModelWithScalarListProperty>>(p => p.Add(c => c.Model, model));

        cut.FindAll("button.wizard-list-repeater__remove")[0].Click();

        Assert.Equal(new[] { "b" }, model.Tags);
        Assert.Single(cut.FindAll("div.wizard-list-repeater__row input"));
    }

    private class Beneficiary
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        public int SharePercent { get; set; }
    }

    private class ModelWithComplexListProperty
    {
        [FormStep(1, "Beneficiaries")]
        [MinItemCount(1)]
        [MaxItemCount(3)]
        public List<Beneficiary> Beneficiaries { get; set; } = new() { new Beneficiary { Name = "Alice", SharePercent = 100 } };
    }

    [Fact]
    public void A_ListT_of_complex_type_renders_one_fieldset_group_per_item()
    {
        var model = new ModelWithComplexListProperty();
        var cut = Render<DynamicWizard<ModelWithComplexListProperty>>(p => p.Add(c => c.Model, model));

        var groups = cut.FindAll("fieldset.wizard-list-repeater__item");
        Assert.Single(groups);
        Assert.Equal("Alice", groups[0].QuerySelector("input.wizard-field--text")!.GetAttribute("value"));
    }

    [Fact]
    public void Editing_a_complex_list_items_own_field_writes_into_that_item_not_a_sibling()
    {
        var model = new ModelWithComplexListProperty();
        model.Beneficiaries.Add(new Beneficiary { Name = "Bob", SharePercent = 0 });
        var cut = Render<DynamicWizard<ModelWithComplexListProperty>>(p => p.Add(c => c.Model, model));

        cut.FindAll("fieldset.wizard-list-repeater__item input.wizard-field--text")[1].Change("Robert");

        Assert.Equal("Alice", model.Beneficiaries[0].Name);
        Assert.Equal("Robert", model.Beneficiaries[1].Name);
    }

    [Fact]
    public void Clicking_Add_on_a_complex_list_appends_a_blank_item()
    {
        var model = new ModelWithComplexListProperty();
        var cut = Render<DynamicWizard<ModelWithComplexListProperty>>(p => p.Add(c => c.Model, model));

        cut.Find("button.wizard-list-repeater__add").Click();

        Assert.Equal(2, model.Beneficiaries.Count);
        Assert.Equal(string.Empty, model.Beneficiaries[1].Name);
    }

    [Fact]
    public void Clicking_Remove_on_a_complex_list_deletes_that_item()
    {
        var model = new ModelWithComplexListProperty();
        model.Beneficiaries.Add(new Beneficiary { Name = "Bob" });
        var cut = Render<DynamicWizard<ModelWithComplexListProperty>>(p => p.Add(c => c.Model, model));

        cut.FindAll("button.wizard-list-repeater__remove")[0].Click();

        Assert.Single(model.Beneficiaries);
        Assert.Equal("Bob", model.Beneficiaries[0].Name);
    }

    [Fact]
    public void MaxItemCount_and_MinItemCount_validate_the_whole_list_property()
    {
        var model = new ModelWithComplexListProperty();
        model.Beneficiaries.Clear(); // violates MinItemCount(1)
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<ModelWithComplexListProperty>(), model);

        Assert.False(nav.ValidateCurrentStep(store));
        var messages = editContext.GetValidationMessages(new FieldIdentifier(model, nameof(ModelWithComplexListProperty.Beneficiaries)));
        Assert.Contains("At least 1 item(s) required.", messages);
    }

    [Fact]
    public void Each_complex_list_items_own_Required_field_is_validated_individually()
    {
        var model = new ModelWithComplexListProperty();
        model.Beneficiaries[0].Name = string.Empty; // violates [Required] on Beneficiary.Name
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<ModelWithComplexListProperty>(), model);

        Assert.False(nav.ValidateCurrentStep(store));
        var messages = editContext.GetValidationMessages(new FieldIdentifier(model.Beneficiaries[0], nameof(Beneficiary.Name)));
        Assert.Contains("Name is required.", messages);
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

    // Tier 4 (fallback) -- a type that is neither a known scalar, IParsable<T> (tier 2b), nor a
    // complex class (a bare struct with a public field but no properties and no IParsable
    // implementation is genuinely unhandled).
    private struct UnparsableStruct
    {
        public int Value;
    }

    private class FallbackModel
    {
        [FormStep(1)]
        public UnparsableStruct Id { get; set; }
    }

    [Fact]
    public void An_unhandled_type_renders_a_read_only_fallback_instead_of_disappearing()
    {
        var value = new UnparsableStruct { Value = 7 };
        var cut = Render<DynamicWizard<FallbackModel>>(p => p.Add(c => c.Model, new FallbackModel { Id = value }));

        var span = cut.Find("span.wizard-field--unhandled");
        Assert.Equal(value.ToString(), span.TextContent);
    }

    // Tier 2b -- any IParsable<T> not already covered by a native Blazor Input* component.
    private class ParsableTypesModel
    {
        [FormStep(1)]
        public byte SmallCount { get; set; }

        [FormStep(1)]
        public Guid ExternalId { get; set; } = Guid.Empty;

        [FormStep(1)]
        public TimeSpan Duration { get; set; }
    }

    [Fact]
    public void A_byte_property_renders_via_WizardParsableInput_and_round_trips_edits()
    {
        var model = new ParsableTypesModel();
        var cut = Render<DynamicWizard<ParsableTypesModel>>(p => p.Add(c => c.Model, model));

        cut.Find("input.wizard-field--parsable").Change("200");

        Assert.Equal((byte)200, model.SmallCount);
    }

    [Fact]
    public void A_Guid_property_renders_via_WizardParsableInput_and_round_trips_edits()
    {
        var model = new ParsableTypesModel();
        var newId = Guid.NewGuid();
        var cut = Render<DynamicWizard<ParsableTypesModel>>(p => p.Add(c => c.Model, model));

        cut.FindAll("input.wizard-field--parsable")[1].Change(newId.ToString());

        Assert.Equal(newId, model.ExternalId);
    }

    [Fact]
    public void An_invalid_value_for_a_parsable_type_surfaces_a_validation_error_instead_of_throwing()
    {
        var model = new ParsableTypesModel();
        var cut = Render<DynamicWizard<ParsableTypesModel>>(p => p.Add(c => c.Model, model));

        cut.Find("input.wizard-field--parsable").Change("not-a-byte");

        Assert.Equal((byte)0, model.SmallCount);
        cut.Find("input.wizard-field--parsable.wizard-field--invalid");
    }

    // Exhaustive proof for every C# native/BCL scalar type claimed as "handled" -- every property
    // here must render as an editable input (native Input* or WizardParsableInput), never fall
    // through to the tier-4 read-only fallback, and every edit must round-trip into the model.
    private class EveryNativeTypeModel
    {
        [FormStep(1)] public bool BoolValue { get; set; }
        [FormStep(1)] public Priority EnumValue { get; set; } = Priority.Low;
        [FormStep(1)] public string StringValue { get; set; } = string.Empty;
        [FormStep(1)] public sbyte SByteValue { get; set; }
        [FormStep(1)] public byte ByteValue { get; set; }
        [FormStep(1)] public short ShortValue { get; set; }
        [FormStep(1)] public ushort UShortValue { get; set; }
        [FormStep(1)] public int IntValue { get; set; }
        [FormStep(1)] public uint UIntValue { get; set; }
        [FormStep(1)] public long LongValue { get; set; }
        [FormStep(1)] public ulong ULongValue { get; set; }
        [FormStep(1)] public nint NIntValue { get; set; }
        [FormStep(1)] public nuint NUIntValue { get; set; }
        [FormStep(1)] public char CharValue { get; set; }
        [FormStep(1)] public float FloatValue { get; set; }
        [FormStep(1)] public double DoubleValue { get; set; }
        [FormStep(1)] public decimal DecimalValue { get; set; }
        [FormStep(1)] public DateTime DateTimeValue { get; set; }
        [FormStep(1)] public DateOnly DateOnlyValue { get; set; }
        [FormStep(1)] public TimeOnly TimeOnlyValue { get; set; }
        [FormStep(1)] public TimeSpan TimeSpanValue { get; set; }
        [FormStep(1)] public Guid GuidValue { get; set; }
        [FormStep(1)] public DateTimeOffset DateTimeOffsetValue { get; set; }
    }

    [Fact]
    public void Every_native_CSharp_scalar_type_renders_as_an_editable_input_not_a_fallback()
    {
        var cut = Render<DynamicWizard<EveryNativeTypeModel>>(p => p.Add(c => c.Model, new EveryNativeTypeModel()));

        Assert.Empty(cut.FindAll("span.wizard-field--unhandled"));

        var propertyCount = typeof(EveryNativeTypeModel).GetProperties().Length;
        var renderedFieldCount = cut.FindAll("input, select").Count;
        Assert.Equal(propertyCount, renderedFieldCount);
    }

    [Fact]
    public void A_ulong_and_a_char_property_both_round_trip_edits_via_WizardParsableInput()
    {
        var model = new EveryNativeTypeModel();
        var cut = Render<DynamicWizard<EveryNativeTypeModel>>(p => p.Add(c => c.Model, model));

        // Only non-native-Blazor-Input types land in this list, in declaration order: SByte(0),
        // Byte(1), UShort(2), UInt(3), ULong(4), NInt(5), NUInt(6), Char(7), TimeSpan(8), Guid(9),
        // DateTimeOffset(10) -- Short/Int/Long/Float/Double/Decimal/DateTime/DateOnly/TimeOnly all
        // have native components instead and don't appear here. Re-query after each edit -- a
        // change re-renders the tree and invalidates prior element/event-handler references.
        cut.FindAll("input.wizard-field--parsable")[3].Change("4000000000");
        cut.FindAll("input.wizard-field--parsable")[7].Change("Z");

        Assert.Equal(4000000000u, model.UIntValue);
        Assert.Equal('Z', model.CharValue);
    }

    // Nullable<T> proof (DESIGN-DISCUSSION.md A.4 tier 2b, nullable form) -- Nullable<T> can never
    // itself satisfy an IParsable<T> constraint (a C# language rule), so every nullable branch of
    // the dispatch checks Nullable.GetUnderlyingType instead. Covers every nullable native/BCL
    // scalar: the native Input*<TValue> types already support T? directly, bool? and every tier-2b
    // IParsable<T> type route through WizardNullableParsableInput<T>, and a nullable enum gets its
    // own "-- none --" option instead of defaulting to the first member.
    private class NullableTypesModel
    {
        [FormStep(1)] public bool? BoolValue { get; set; }
        [FormStep(1)] public Priority? EnumValue { get; set; }
        [FormStep(1)] public sbyte? SByteValue { get; set; }
        [FormStep(1)] public byte? ByteValue { get; set; }
        [FormStep(1)] public short? ShortValue { get; set; }
        [FormStep(1)] public ushort? UShortValue { get; set; }
        [FormStep(1)] public int? IntValue { get; set; }
        [FormStep(1)] public uint? UIntValue { get; set; }
        [FormStep(1)] public long? LongValue { get; set; }
        [FormStep(1)] public ulong? ULongValue { get; set; }
        [FormStep(1)] public nint? NIntValue { get; set; }
        [FormStep(1)] public nuint? NUIntValue { get; set; }
        [FormStep(1)] public char? CharValue { get; set; }
        [FormStep(1)] public float? FloatValue { get; set; }
        [FormStep(1)] public double? DoubleValue { get; set; }
        [FormStep(1)] public decimal? DecimalValue { get; set; }
        [FormStep(1)] public DateTime? DateTimeValue { get; set; }
        [FormStep(1)] public DateOnly? DateOnlyValue { get; set; }
        [FormStep(1)] public TimeOnly? TimeOnlyValue { get; set; }
        [FormStep(1)] public TimeSpan? TimeSpanValue { get; set; }
        [FormStep(1)] public Guid? GuidValue { get; set; }
        [FormStep(1)] public DateTimeOffset? DateTimeOffsetValue { get; set; }
    }

    [Fact]
    public void Every_nullable_CSharp_scalar_type_renders_as_an_editable_input_not_a_fallback()
    {
        var cut = Render<DynamicWizard<NullableTypesModel>>(p => p.Add(c => c.Model, new NullableTypesModel()));

        Assert.Empty(cut.FindAll("span.wizard-field--unhandled"));

        var propertyCount = typeof(NullableTypesModel).GetProperties().Length;
        var renderedFieldCount = cut.FindAll("input, select").Count;
        Assert.Equal(propertyCount, renderedFieldCount);
    }

    [Fact]
    public void A_nullable_byte_property_starts_empty_accepts_a_value_and_clears_back_to_null()
    {
        var model = new NullableTypesModel();
        var cut = Render<DynamicWizard<NullableTypesModel>>(p => p.Add(c => c.Model, model));

        // Parsable-tier declaration order: BoolValue(0), SByteValue(1), ByteValue(2), UShortValue(3),
        // UIntValue(4), ULongValue(5), NIntValue(6), NUIntValue(7), CharValue(8), TimeSpanValue(9),
        // GuidValue(10), DateTimeOffsetValue(11) -- Enum uses <select>; Short/Int/Long/Float/Double/
        // Decimal/DateTime/DateOnly/TimeOnly all have native nullable-aware components instead.
        Assert.Null(model.ByteValue);

        cut.FindAll("input.wizard-field--parsable")[2].Change("9");
        Assert.Equal((byte)9, model.ByteValue);

        cut.FindAll("input.wizard-field--parsable")[2].Change("");
        Assert.Null(model.ByteValue);
    }

    [Fact]
    public void An_invalid_value_for_a_nullable_parsable_type_surfaces_a_validation_error()
    {
        var model = new NullableTypesModel();
        var cut = Render<DynamicWizard<NullableTypesModel>>(p => p.Add(c => c.Model, model));

        cut.FindAll("input.wizard-field--parsable")[2].Change("not-a-byte");

        Assert.Null(model.ByteValue);
        cut.Find("input.wizard-field--parsable.wizard-field--invalid");
    }

    [Fact]
    public void A_nullable_enum_offers_a_none_option_and_round_trips_between_null_and_a_value()
    {
        var model = new NullableTypesModel();
        var cut = Render<DynamicWizard<NullableTypesModel>>(p => p.Add(c => c.Model, model));

        var select = cut.Find("select.wizard-field--select");
        var options = select.QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToArray();
        Assert.Equal(new string?[] { "", "Low", "High" }, options);

        select.Change("High");
        Assert.Equal(Priority.High, model.EnumValue);

        cut.Find("select.wizard-field--select").Change("");
        Assert.Null(model.EnumValue);
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

    // [DataType] rendering (README.md/#141) -- a handful of well-known string shapes get a real
    // HTML5 input type or a <textarea> instead of native InputText's hardcoded type="text".
    private class DataTypeModel
    {
        [FormStep(1)]
        [DataType(DataType.Password)]
        public string Secret { get; set; } = string.Empty;

        [FormStep(1)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [FormStep(1)]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; } = string.Empty;
    }

    [Fact]
    public void DataType_Password_renders_an_input_type_password()
    {
        var cut = Render<DynamicWizard<DataTypeModel>>(p => p.Add(c => c.Model, new DataTypeModel()));

        Assert.NotEmpty(cut.FindAll("input[type=password]"));
    }

    [Fact]
    public void DataType_EmailAddress_renders_an_input_type_email_and_round_trips_edits()
    {
        var model = new DataTypeModel();
        var cut = Render<DynamicWizard<DataTypeModel>>(p => p.Add(c => c.Model, model));

        cut.Find("input[type=email]").Change("a@b.com");

        Assert.Equal("a@b.com", model.Email);
    }

    [Fact]
    public void DataType_MultilineText_renders_a_textarea_and_round_trips_edits()
    {
        var model = new DataTypeModel();
        var cut = Render<DynamicWizard<DataTypeModel>>(p => p.Add(c => c.Model, model));

        cut.Find("textarea.wizard-field--textarea").Change("line one");

        Assert.Equal("line one", model.Notes);
    }

    // [Editable(false)] renders a read-only span regardless of what tier the type would otherwise
    // dispatch to -- proven here on a plain string that would normally render an editable InputText.
    private class EditableModel
    {
        [FormStep(1)]
        [Editable(false)]
        public string ComputedId { get; set; } = "ABC-123";

        [FormStep(1)]
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void EditableFalse_renders_a_read_only_span_not_an_editable_input()
    {
        var cut = Render<DynamicWizard<EditableModel>>(p => p.Add(c => c.Model, new EditableModel()));

        var span = cut.Find("span.wizard-field--readonly");
        Assert.Equal("ABC-123", span.TextContent);
        Assert.Single(cut.FindAll("input.wizard-field--text")); // only Name renders one; ComputedId doesn't
    }

    // [Editable(false)] + [DisplayFormat(DataFormatString=...)] together -- the read-only span
    // formats the value instead of a bare ToString().
    private class EditableWithFormatModel
    {
        [FormStep(1)]
        [Editable(false)]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Amount { get; set; } = 1234.5m;
    }

    [Fact]
    public void EditableFalse_combined_with_DisplayFormat_formats_the_read_only_value()
    {
        var cut = Render<DynamicWizard<EditableWithFormatModel>>(p => p.Add(c => c.Model, new EditableWithFormatModel()));

        var span = cut.Find("span.wizard-field--readonly");
        Assert.Equal(string.Format("{0:C}", 1234.5m), span.TextContent);
    }

    // [DisplayFormat] on the tier-4 fallback (an unhandled type, reusing UnparsableStruct/Nullable
    // form of it) -- NullDisplayText for a null value, DataFormatString for a non-null one.
    private class FallbackWithNullDisplayTextModel
    {
        [FormStep(1)]
        [DisplayFormat(NullDisplayText = "(none)")]
        public UnparsableStruct? Id { get; set; }
    }

    [Fact]
    public void RenderFallback_honors_DisplayFormat_NullDisplayText_for_a_null_unhandled_value()
    {
        var cut = Render<DynamicWizard<FallbackWithNullDisplayTextModel>>(p => p.Add(c => c.Model, new FallbackWithNullDisplayTextModel()));

        var span = cut.Find("span.wizard-field--unhandled");
        Assert.Equal("(none)", span.TextContent);
    }

    private class FallbackWithFormatStringModel
    {
        [FormStep(1)]
        [DisplayFormat(DataFormatString = "ID:{0}")]
        public UnparsableStruct Id { get; set; } = new UnparsableStruct { Value = 42 };
    }

    [Fact]
    public void RenderFallback_honors_DisplayFormat_DataFormatString_for_a_non_null_unhandled_value()
    {
        var value = new UnparsableStruct { Value = 42 };
        var cut = Render<DynamicWizard<FallbackWithFormatStringModel>>(p => p.Add(c => c.Model, new FallbackWithFormatStringModel()));

        var span = cut.Find("span.wizard-field--unhandled");
        Assert.Equal($"ID:{value}", span.TextContent);
    }

    // [ScaffoldColumn(false)] excludes a property entirely -- never rendered, never validated,
    // never counted toward step visibility (WizardModelSchema.Build filters it out before it ever
    // becomes a WizardPropertySchema).
    private class ScaffoldColumnModel
    {
        [FormStep(1)]
        public string Visible { get; set; } = string.Empty;

        [FormStep(1)]
        [ScaffoldColumn(false)]
        [Required(ErrorMessage = "Should never be checked.")]
        public string Hidden { get; set; } = string.Empty;
    }

    [Fact]
    public void ScaffoldColumnFalse_excludes_the_property_from_rendering_entirely()
    {
        var cut = Render<DynamicWizard<ScaffoldColumnModel>>(p => p.Add(c => c.Model, new ScaffoldColumnModel()));

        Assert.Single(cut.FindAll("input.wizard-field--text")); // only Visible, not Hidden
    }

    [Fact]
    public void ScaffoldColumnFalse_excludes_the_property_from_validation_too()
    {
        var model = new ScaffoldColumnModel(); // Hidden's [Required] would fail if it were still validated
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<ScaffoldColumnModel>(), model);

        Assert.True(nav.ValidateCurrentStep(store));
    }

    // [FormLabel(LabelPosition)] / DynamicWizard.DefaultLabelPosition (README.md/#142) -- Above
    // (the default) and Left both keep a real, visible <label>; Inline/Hidden render none at all
    // and move the text onto the input itself instead (placeholder/aria-label respectively).
    private class LabelPositionModel
    {
        [FormStep(1)]
        public string Above { get; set; } = string.Empty;

        [FormStep(1)]
        [FormLabel(LabelPosition.Left)]
        public string Left { get; set; } = string.Empty;

        [FormStep(1)]
        [FormLabel(LabelPosition.Inline)]
        public string Inline { get; set; } = string.Empty;

        [FormStep(1)]
        [FormLabel(LabelPosition.Hidden)]
        public string Hidden { get; set; } = string.Empty;
    }

    [Fact]
    public void LabelPosition_Above_is_the_default_and_renders_a_visible_label()
    {
        var cut = Render<DynamicWizard<LabelPositionModel>>(p => p.Add(c => c.Model, new LabelPositionModel()));

        Assert.Contains(cut.FindAll("label.wizard__label"), l => l.TextContent == nameof(LabelPositionModel.Above));
    }

    [Fact]
    public void LabelPosition_Left_still_renders_a_visible_label_with_the_left_layout_class()
    {
        var cut = Render<DynamicWizard<LabelPositionModel>>(p => p.Add(c => c.Model, new LabelPositionModel()));

        Assert.Contains(cut.FindAll("label.wizard__label"), l => l.TextContent == nameof(LabelPositionModel.Left));
        Assert.NotEmpty(cut.FindAll("div.wizard__field-row--label-left"));
    }

    [Fact]
    public void LabelPosition_Inline_renders_no_visible_label_and_sets_placeholder_instead()
    {
        var cut = Render<DynamicWizard<LabelPositionModel>>(p => p.Add(c => c.Model, new LabelPositionModel()));

        Assert.DoesNotContain(cut.FindAll("label.wizard__label"), l => l.TextContent == nameof(LabelPositionModel.Inline));
        var inputs = cut.FindAll("input.wizard-field--text")
            .Where(i => i.GetAttribute("placeholder") == nameof(LabelPositionModel.Inline));
        Assert.Single(inputs);
    }

    [Fact]
    public void LabelPosition_Hidden_renders_no_visible_label_and_sets_aria_label_instead()
    {
        var cut = Render<DynamicWizard<LabelPositionModel>>(p => p.Add(c => c.Model, new LabelPositionModel()));

        Assert.DoesNotContain(cut.FindAll("label.wizard__label"), l => l.TextContent == nameof(LabelPositionModel.Hidden));
        var inputs = cut.FindAll("input.wizard-field--text")
            .Where(i => i.GetAttribute("aria-label") == nameof(LabelPositionModel.Hidden));
        Assert.Single(inputs);
    }

    private class PlainTextModel
    {
        [FormStep(1)]
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void DefaultLabelPosition_applies_to_every_property_with_no_FormLabel_override()
    {
        var cut = Render<DynamicWizard<PlainTextModel>>(p => p
            .Add(c => c.Model, new PlainTextModel())
            .Add(c => c.DefaultLabelPosition, LabelPosition.Hidden));

        Assert.Empty(cut.FindAll("label.wizard__label"));
        Assert.Equal(nameof(PlainTextModel.Name), cut.Find("input.wizard-field--text").GetAttribute("aria-label"));
    }

    // [FieldAttributes] (README.md/#143) splats arbitrary HTML onto one named field's rendered
    // input -- and, combined with Hidden, proves a consumer's own explicit aria-label wins over
    // the one Hidden would otherwise synthesize (same "explicit beats engine default" precedence
    // as every other override in this engine).
    [Fact]
    public void FieldAttributes_splats_extra_html_attributes_onto_the_named_field()
    {
        var attrs = new Dictionary<string, IReadOnlyDictionary<string, object>>
        {
            [nameof(PlainTextModel.Name)] = new Dictionary<string, object> { ["data-testid"] = "name-field" },
        };
        var cut = Render<DynamicWizard<PlainTextModel>>(p => p
            .Add(c => c.Model, new PlainTextModel())
            .Add(c => c.FieldAttributes, attrs));

        Assert.Equal("name-field", cut.Find("input.wizard-field--text").GetAttribute("data-testid"));
    }

    [Fact]
    public void FieldAttributes_own_aria_label_wins_over_the_one_Hidden_would_synthesize()
    {
        var attrs = new Dictionary<string, IReadOnlyDictionary<string, object>>
        {
            [nameof(PlainTextModel.Name)] = new Dictionary<string, object> { ["aria-label"] = "Custom label" },
        };
        var cut = Render<DynamicWizard<PlainTextModel>>(p => p
            .Add(c => c.Model, new PlainTextModel())
            .Add(c => c.DefaultLabelPosition, LabelPosition.Hidden)
            .Add(c => c.FieldAttributes, attrs));

        Assert.Equal("Custom label", cut.Find("input.wizard-field--text").GetAttribute("aria-label"));
    }

    // [Display(Prompt=...)] (README.md/#142 follow-up) -- stock DataAnnotations' own placeholder
    // field, reused rather than inventing a new attribute. Applies regardless of LabelPosition: a
    // visible label above the field and a placeholder hint inside it aren't mutually exclusive.
    private class DisplayPromptModel
    {
        [FormStep(1)]
        [Display(Prompt = "e.g. jane@example.com")]
        public string Email { get; set; } = string.Empty;

        [FormStep(1)]
        [FormLabel(LabelPosition.Inline)]
        [Display(Prompt = "Explicit prompt wins")]
        public string InlineWithPrompt { get; set; } = string.Empty;
    }

    [Fact]
    public void DisplayPrompt_sets_the_placeholder_even_with_the_default_Above_label_position()
    {
        var cut = Render<DynamicWizard<DisplayPromptModel>>(p => p.Add(c => c.Model, new DisplayPromptModel()));

        var input = cut.FindAll("input.wizard-field--text")
            .First(i => i.GetAttribute("placeholder") == "e.g. jane@example.com");
        Assert.NotNull(input);
        Assert.Contains(cut.FindAll("label.wizard__label"), l => l.TextContent == nameof(DisplayPromptModel.Email));
    }

    [Fact]
    public void DisplayPrompt_wins_over_the_LabelPosition_Inline_label_text_fallback()
    {
        var cut = Render<DynamicWizard<DisplayPromptModel>>(p => p.Add(c => c.Model, new DisplayPromptModel()));

        var input = cut.FindAll("input.wizard-field--text")
            .First(i => i.GetAttribute("placeholder") == "Explicit prompt wins");
        Assert.NotNull(input);
    }

    // [Display(Order=N)] as a fallback for field order (README.md follow-up) -- reuses stock
    // DataAnnotations rather than requiring [FormOrder] for every model. Read via GetOrder(),
    // never the Order property getter directly: DisplayAttribute.Order throws
    // InvalidOperationException when never explicitly set.
    private class DisplayOrderModel
    {
        [FormStep(1)]
        [Display(Order = 2)]
        public string Second { get; set; } = string.Empty;

        [FormStep(1)]
        [Display(Order = 1)]
        public string First { get; set; } = string.Empty;

        [FormStep(1)]
        [Display(Order = 3)]
        public string Third { get; set; } = string.Empty;
    }

    [Fact]
    public void DisplayOrder_alone_orders_fields_with_no_FormOrder_attribute_present()
    {
        var cut = Render<DynamicWizard<DisplayOrderModel>>(p => p.Add(c => c.Model, new DisplayOrderModel()));

        var labels = cut.FindAll("label.wizard__label").Select(l => l.TextContent).ToArray();
        Assert.Equal([nameof(DisplayOrderModel.First), nameof(DisplayOrderModel.Second), nameof(DisplayOrderModel.Third)], labels);
    }

    private class FormOrderWinsOverDisplayOrderModel
    {
        [FormStep(1)]
        [FormOrder(1)]
        [Display(Order = 99)] // would sort last if Display.Order were used instead of FormOrder
        public string A { get; set; } = string.Empty;

        [FormStep(1)]
        [FormOrder(2)]
        [Display(Order = 1)] // would sort first if Display.Order were used instead of FormOrder
        public string B { get; set; } = string.Empty;
    }

    [Fact]
    public void FormOrder_wins_over_DisplayOrder_when_both_are_present()
    {
        var cut = Render<DynamicWizard<FormOrderWinsOverDisplayOrderModel>>(p => p.Add(c => c.Model, new FormOrderWinsOverDisplayOrderModel()));

        var labels = cut.FindAll("label.wizard__label").Select(l => l.TextContent).ToArray();
        Assert.Equal([nameof(FormOrderWinsOverDisplayOrderModel.A), nameof(FormOrderWinsOverDisplayOrderModel.B)], labels);
    }

    // Cancel/close affordance (README.md/#137, DESIGN-DISCUSSION.md G.26) -- opt-in, no validation.
    [Fact]
    public void ShowCancelButton_false_by_default_renders_no_cancel_button()
    {
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p.Add(c => c.Model, new WizardTestModel()));

        Assert.Empty(cut.FindAll("button.wizard__button--cancel"));
    }

    [Fact]
    public void ShowCancelButton_true_renders_a_cancel_button()
    {
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p
            .Add(c => c.Model, new WizardTestModel())
            .Add(c => c.ShowCancelButton, true));

        Assert.Single(cut.FindAll("button.wizard__button--cancel"));
    }

    [Fact]
    public void Cancel_invokes_OnWizardCancel_with_the_current_model_without_validating()
    {
        var model = new WizardTestModel(); // Name left empty -- [Required] would normally block Next
        WizardTestModel? cancelled = null;
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.ShowCancelButton, true)
            .Add(c => c.OnWizardCancel, EventCallback.Factory.Create<WizardTestModel>(this, m => cancelled = m)));

        cut.Find("button.wizard__button--cancel").Click();

        Assert.Same(model, cancelled);
        Assert.Empty(cut.FindAll("span.wizard__error")); // no validation ran
        Assert.Contains("Step 1 of 3", cut.Markup); // no step/state mutation either
    }

    // Draft-save/resume (README.md/#134, DESIGN-DISCUSSION.md G.23) -- InitialStep resumes
    // straight at a saved step; CurrentStep + OnStepChanged let a consumer build their own
    // save snapshot without this engine owning any storage itself.
    [Fact]
    public void InitialStep_resumes_the_wizard_at_the_given_step_on_first_render()
    {
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p
            .Add(c => c.Model, new WizardTestModel())
            .Add(c => c.InitialStep, 2));

        Assert.Contains("Step 2 of 3", cut.Markup);
        Assert.NotNull(cut.Find("fieldset.wizard-field-group"));
        Assert.Equal(2, cut.Instance.CurrentStep);
    }

    [Fact]
    public void CurrentStep_reflects_the_navigators_position_after_advancing()
    {
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p.Add(c => c.Model, new WizardTestModel()));
        Assert.Equal(1, cut.Instance.CurrentStep);

        cut.Find("input.wizard-field--text").Change("Alice");
        cut.Find("button.wizard__button--next").Click();

        Assert.Equal(2, cut.Instance.CurrentStep);
    }

    [Fact]
    public void OnStepChanged_fires_with_the_new_step_number_after_Next_and_Back()
    {
        var stepChanges = new List<int>();
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p
            .Add(c => c.Model, new WizardTestModel())
            .Add(c => c.OnStepChanged, EventCallback.Factory.Create<int>(this, s => stepChanges.Add(s))));

        cut.Find("input.wizard-field--text").Change("Alice");
        cut.Find("button.wizard__button--next").Click();
        cut.Find("button.wizard__button--back").Click();

        Assert.Equal([2, 1], stepChanges);
    }

    [Fact]
    public void OnStepChanged_does_not_fire_when_Next_is_blocked_by_validation()
    {
        var stepChanges = new List<int>();
        var cut = Render<DynamicWizard<WizardTestModel>>(p => p
            .Add(c => c.Model, new WizardTestModel())
            .Add(c => c.OnStepChanged, EventCallback.Factory.Create<int>(this, s => stepChanges.Add(s))));

        cut.Find("button.wizard__button--next").Click(); // Name left empty -- blocked

        Assert.Empty(stepChanges);
    }
}
