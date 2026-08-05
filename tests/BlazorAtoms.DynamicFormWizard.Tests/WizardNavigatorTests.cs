using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using BlazorAtoms.DynamicFormWizard.Navigation;

namespace BlazorAtoms.DynamicFormWizard.Tests;

public class WizardNavigatorTests
{
    // Scenario 1 (DESIGN-DISCUSSION.md): A/B fork converging on a shared, unconditional step.
    public enum Choice { A, B }

    private class ForkRejoinModel
    {
        [FormStep(1)]
        public Choice Selection { get; set; } = Choice.A;

        [FormStep(2)]
        [DependsOn(nameof(Selection), Choice.A)]
        public string FieldA { get; set; } = string.Empty;

        [FormStep(3)]
        [DependsOn(nameof(Selection), Choice.B)]
        public string FieldB { get; set; } = string.Empty;

        [FormStep(4, "Review")]
        public string Shared { get; set; } = string.Empty;
    }

    [Fact]
    public void Choosing_A_walks_1_then_2_then_skips_3_and_lands_on_the_shared_step_4()
    {
        var model = new ForkRejoinModel { Selection = Choice.A };
        var nav = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), model);

        Assert.Equal(1, nav.CurrentStep);
        nav.GoNext();
        Assert.Equal(2, nav.CurrentStep);
        nav.GoNext();
        Assert.Equal(4, nav.CurrentStep);
    }

    // initialStep (README.md/#134, draft-save/resume) -- resumes at a saved step instead of
    // always starting at the first declared one.
    [Fact]
    public void InitialStep_resumes_at_the_given_step_when_it_is_a_real_declared_step()
    {
        var model = new ForkRejoinModel { Selection = Choice.A };
        var nav = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), model, initialStep: 4);

        Assert.Equal(4, nav.CurrentStep);
    }

    [Fact]
    public void InitialStep_falls_back_to_the_first_declared_step_when_it_is_not_a_real_one()
    {
        var model = new ForkRejoinModel { Selection = Choice.A };
        var nav = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), model, initialStep: 99);

        Assert.Equal(1, nav.CurrentStep);
    }

    [Fact]
    public void Choosing_B_walks_1_then_3_then_skips_2_and_lands_on_the_shared_step_4()
    {
        var model = new ForkRejoinModel { Selection = Choice.B };
        var nav = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), model);

        nav.GoNext();
        Assert.Equal(3, nav.CurrentStep);
        nav.GoNext();
        Assert.Equal(4, nav.CurrentStep);
    }

    [Fact]
    public void Back_navigation_returns_through_whichever_branch_was_actually_taken()
    {
        var model = new ForkRejoinModel { Selection = Choice.A };
        var nav = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), model);
        nav.GoNext();
        nav.GoNext();
        Assert.Equal(4, nav.CurrentStep);

        nav.GoPrevious();

        Assert.Equal(2, nav.CurrentStep); // skips the untaken step 3, not just step 4 - 1
    }

    [Fact]
    public void GoPrevious_at_the_first_reachable_step_is_a_no_op()
    {
        var model = new ForkRejoinModel();
        var nav = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), model);

        nav.GoPrevious();

        Assert.Equal(1, nav.CurrentStep);
    }

    [Fact]
    public void Both_branches_report_the_same_true_length_in_this_scenario()
    {
        var pathA = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), new ForkRejoinModel { Selection = Choice.A });
        var pathB = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), new ForkRejoinModel { Selection = Choice.B });

        Assert.Equal([1, 2, 4], pathA.EffectiveStepNumbers());
        Assert.Equal([1, 3, 4], pathB.EffectiveStepNumbers());
        Assert.Equal(pathA.EffectiveStepNumbers().Count, pathB.EffectiveStepNumbers().Count);
    }

    [Fact]
    public void Title_falls_back_to_a_computed_Step_N_when_no_title_is_declared()
    {
        var model = new ForkRejoinModel();
        var nav = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), model);

        Assert.Equal("Step 1", nav.DisplayTitle());
    }

    [Fact]
    public void Title_uses_the_declared_FormStep_title_when_present()
    {
        var model = new ForkRejoinModel();
        var nav = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), model);
        nav.GoNext();
        nav.GoNext();

        Assert.Equal(4, nav.CurrentStep);
        Assert.Equal("Review", nav.DisplayTitle());
    }

    [Fact]
    public void IsFinalStep_is_true_only_once_no_further_step_has_anything_visible()
    {
        var model = new ForkRejoinModel();
        var nav = new WizardNavigator(WizardModelSchema.For<ForkRejoinModel>(), model);

        Assert.False(nav.IsFinalStep()); // step 1: steps 2 and 4 still ahead
        nav.GoNext();
        Assert.False(nav.IsFinalStep()); // step 2: step 4 still ahead (even though 3 is empty)
        nav.GoNext();
        Assert.True(nav.IsFinalStep()); // step 4: nothing declared after it
    }

    // Scenario 2 (DESIGN-DISCUSSION.md): unconditional -> unconditional-or-conditional ->
    // unconditional, where the two paths have genuinely different true lengths (3 vs 4).
    public enum AccountType { Personal, Manager }

    private class AccountModel
    {
        [FormStep(1)]
        public AccountType Type { get; set; } = AccountType.Personal;

        [FormStep(2)]
        public string CustomerInfo { get; set; } = string.Empty;

        [FormStep(3)]
        [DependsOn(nameof(Type), AccountType.Manager)]
        public string ManagerFields { get; set; } = string.Empty;

        [FormStep(4)]
        public string ValidateSubmit { get; set; } = string.Empty;
    }

    [Fact]
    public void Personal_accounts_have_a_true_length_of_3_steps()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<AccountModel>(), new AccountModel { Type = AccountType.Personal });

        Assert.Equal([1, 2, 4], nav.EffectiveStepNumbers());
    }

    [Fact]
    public void Manager_accounts_have_a_true_length_of_4_steps()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<AccountModel>(), new AccountModel { Type = AccountType.Manager });

        Assert.Equal([1, 2, 3, 4], nav.EffectiveStepNumbers());
    }

    [Fact]
    public void Personal_path_never_shows_step_3_of_4_it_shows_step_2_of_3()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<AccountModel>(), new AccountModel { Type = AccountType.Personal });
        nav.GoNext();

        Assert.Equal(2, nav.CurrentStep);
        Assert.Equal((2, 3), nav.DisplayPosition());
    }

    [Fact]
    public void Manager_path_shows_step_3_of_4_correctly_on_the_manager_fields_step()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<AccountModel>(), new AccountModel { Type = AccountType.Manager });
        nav.GoNext();
        nav.GoNext();

        Assert.Equal(3, nav.CurrentStep);
        Assert.Equal((3, 4), nav.DisplayPosition());
    }

    // Partial per-step validation.
    private class ValidatedModel
    {
        [FormStep(1)]
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [FormStep(2)]
        public string Other { get; set; } = string.Empty;
    }

    [Fact]
    public void ValidateCurrentStep_fails_and_populates_the_message_store_for_an_invalid_value()
    {
        var model = new ValidatedModel { Name = string.Empty };
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<ValidatedModel>(), model);

        var isValid = nav.ValidateCurrentStep(store);

        Assert.False(isValid);
        var messages = editContext.GetValidationMessages(new FieldIdentifier(model, nameof(ValidatedModel.Name)));
        Assert.Contains("Name is required.", messages);
    }

    [Fact]
    public void ValidateCurrentStep_passes_and_clears_prior_messages_once_the_value_is_valid()
    {
        var model = new ValidatedModel { Name = string.Empty };
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<ValidatedModel>(), model);

        nav.ValidateCurrentStep(store);
        model.Name = "Alice";
        var isValid = nav.ValidateCurrentStep(store);

        Assert.True(isValid);
        Assert.Empty(editContext.GetValidationMessages(new FieldIdentifier(model, nameof(ValidatedModel.Name))));
    }

    // FormPathEnd (DESIGN-DISCUSSION.md G.29 addendum): the exact failure mode a consumer raised --
    // with only derived visibility, a later step meant for a *different* branch but missing its
    // own DependsOn (an authoring mistake) would silently become reachable from a branch that was
    // supposed to have ended earlier, because "no condition" already means "always visible." An
    // explicit marker on the earlier branch's true end stops navigation there regardless.
    public enum Branch { A, B }

    private class PathEndModel
    {
        [FormStep(1)]
        public Branch Selection { get; set; } = Branch.A;

        [FormStep(2)]
        [DependsOn(nameof(Selection), Branch.A)]
        [FormPathEnd(nameof(Selection), Branch.A)]
        public string BranchAField { get; set; } = string.Empty;

        [FormStep(3)]
        [DependsOn(nameof(Selection), Branch.B)]
        public string BranchBField { get; set; } = string.Empty;

        // Simulates the mistake: meant only for Branch B, but the DependsOn was forgotten.
        [FormStep(4)]
        public string AccidentallyUnconditional { get; set; } = string.Empty;
    }

    [Fact]
    public void A_path_end_marker_stops_GoNext_even_when_a_later_step_is_accidentally_unconditional()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<PathEndModel>(), new PathEndModel { Selection = Branch.A });

        nav.GoNext();
        Assert.Equal(2, nav.CurrentStep);

        nav.GoNext(); // must NOT advance to step 4, despite it having a visible property

        Assert.Equal(2, nav.CurrentStep);
    }

    [Fact]
    public void IsFinalStep_is_true_at_a_marked_step_even_though_a_later_step_has_content()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<PathEndModel>(), new PathEndModel { Selection = Branch.A });
        nav.GoNext();

        Assert.True(nav.IsFinalStep());
    }

    [Fact]
    public void EffectiveStepNumbers_excludes_everything_after_a_marked_step()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<PathEndModel>(), new PathEndModel { Selection = Branch.A });

        Assert.Equal([1, 2], nav.EffectiveStepNumbers()); // step 4 excluded despite being "visible"
    }

    [Fact]
    public void The_marker_only_protects_the_branch_that_declares_it()
    {
        // Branch B has no marker of its own, so the pre-existing mistake (step 4's missing
        // DependsOn) still surfaces for it -- the fix is targeted, not a blanket safety net.
        var nav = new WizardNavigator(WizardModelSchema.For<PathEndModel>(), new PathEndModel { Selection = Branch.B });

        Assert.Equal([1, 3, 4], nav.EffectiveStepNumbers());
    }

    // A skipped FormStep number (no property anywhere declares it) is inert -- navigation walks
    // the declared numbers directly, never assumes contiguity, and never shows the gap.
    private class SkippedStepModel
    {
        [FormStep(1)]
        public string First { get; set; } = string.Empty;

        [FormStep(3)]
        public string Third { get; set; } = string.Empty;
    }

    [Fact]
    public void GoNext_lands_directly_on_the_next_declared_step_skipping_the_gap()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<SkippedStepModel>(), new SkippedStepModel());

        Assert.Equal(1, nav.CurrentStep);
        nav.GoNext();

        Assert.Equal(3, nav.CurrentStep); // not 2 -- there is no step 2 declared anywhere
    }

    [Fact]
    public void Display_position_and_count_never_reference_the_skipped_number()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<SkippedStepModel>(), new SkippedStepModel());

        Assert.Equal((1, 2), nav.DisplayPosition());
        nav.GoNext();
        Assert.Equal((2, 2), nav.DisplayPosition()); // "Step 2 of 2," never "3 of 2"
    }

    // Two properties sharing one FormStep number is the normal mechanism for a multi-field step
    // (or a step whose visible content differs per branch, per DESIGN-DISCUSSION.md scenario 1's
    // own step 3/4). The one genuinely open question is conflicting *titles* declared on two
    // different properties in the same step -- resolved deterministically by the same (FormOrder,
    // encounter-order) tie-break already used for field render order, not left undefined.
    [Fact]
    public void A_title_conflict_across_two_properties_in_one_step_is_resolved_by_render_order()
    {
        var nav = new WizardNavigator(WizardModelSchema.For<TitleConflictModel>(), new TitleConflictModel());

        // "First Title" has FormOrder(1); "Second Title" has FormOrder(2) -- render order decides.
        Assert.Equal("First Title", nav.DisplayTitle());
    }

    private class TitleConflictModel
    {
        [FormStep(1, "First Title")]
        [FormOrder(1)]
        public string A { get; set; } = string.Empty;

        [FormStep(1, "Second Title")]
        [FormOrder(2)]
        public string B { get; set; } = string.Empty;
    }

    // Auto-expanded complex-typed properties validate recursively via TryValidateObject, not
    // TryValidateValue (DESIGN-DISCUSSION.md B.5) -- exercised directly here, not just inferred
    // from reading WizardNavigator's source.
    private class GroupWithRequiredField
    {
        [Required(ErrorMessage = "Street is required.")]
        public string Street { get; set; } = string.Empty;
    }

    private class NestedGroupModel
    {
        [FormStep(1)]
        public GroupWithRequiredField Group { get; set; } = new();
    }

    [Fact]
    public void ValidateCurrentStep_recurses_into_a_complex_typed_property_via_TryValidateObject()
    {
        var model = new NestedGroupModel();
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<NestedGroupModel>(), model);

        var isValid = nav.ValidateCurrentStep(store);

        Assert.False(isValid);
        var messages = editContext.GetValidationMessages(new FieldIdentifier(model, nameof(NestedGroupModel.Group)));
        Assert.Contains("Street is required.", messages);
    }

    [Fact]
    public void ValidateCurrentStep_passes_once_the_nested_required_field_is_filled()
    {
        var model = new NestedGroupModel();
        model.Group.Street = "Main St";
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<NestedGroupModel>(), model);

        Assert.True(nav.ValidateCurrentStep(store));
    }

    // ComparisonOperator (DESIGN-DISCUSSION.md G.28) -- range is expressed by stacking two
    // conditions on the same property (GreaterThanOrEqual + LessThanOrEqual), reusing the
    // existing AND-combine rule rather than a new construct.
    private class ComparisonOperatorModel
    {
        [FormStep(1)]
        public int Age { get; set; }

        [FormStep(1)]
        public string Status { get; set; } = "Active";

        [FormStep(2)]
        [DependsOn(nameof(Age), 18, ComparisonOperator.GreaterThanOrEqual)]
        [DependsOn(nameof(Age), 65, ComparisonOperator.LessThanOrEqual)]
        public string WorkingAgeField { get; set; } = string.Empty;

        [FormStep(2)]
        [DependsOn(nameof(Status), "Active", ComparisonOperator.NotEquals)]
        public string InactiveOnlyField { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData(17, false)]
    [InlineData(18, true)]
    [InlineData(65, true)]
    [InlineData(66, false)]
    public void GreaterThanOrEqual_and_LessThanOrEqual_stacked_express_a_range(int age, bool expectedVisible)
    {
        var model = new ComparisonOperatorModel { Age = age };
        var nav = new WizardNavigator(WizardModelSchema.For<ComparisonOperatorModel>(), model);

        var property = WizardModelSchema.For<ComparisonOperatorModel>().TryGetByName(nameof(ComparisonOperatorModel.WorkingAgeField))!;

        Assert.Equal(expectedVisible, nav.IsVisible(property));
    }

    [Theory]
    [InlineData("Active", false)]
    [InlineData("Inactive", true)]
    public void NotEquals_hides_the_field_when_the_target_currently_matches(string status, bool expectedVisible)
    {
        var model = new ComparisonOperatorModel { Status = status };
        var nav = new WizardNavigator(WizardModelSchema.For<ComparisonOperatorModel>(), model);

        var property = WizardModelSchema.For<ComparisonOperatorModel>().TryGetByName(nameof(ComparisonOperatorModel.InactiveOnlyField))!;

        Assert.Equal(expectedVisible, nav.IsVisible(property));
    }

    private class NonComparableTarget
    {
        public string Name { get; set; } = string.Empty;
    }

    private class NonComparableConditionModel
    {
        [FormStep(1)]
        public NonComparableTarget Target { get; set; } = new();

        [FormStep(2)]
        [DependsOn(nameof(Target), 5, ComparisonOperator.GreaterThan)]
        public string Gated { get; set; } = string.Empty;
    }

    [Fact]
    public void An_ordering_operator_against_a_non_IComparable_target_throws_a_clear_error()
    {
        var model = new NonComparableConditionModel();
        var nav = new WizardNavigator(WizardModelSchema.For<NonComparableConditionModel>(), model);
        var property = WizardModelSchema.For<NonComparableConditionModel>().TryGetByName(nameof(NonComparableConditionModel.Gated))!;

        var ex = Assert.Throws<InvalidOperationException>(() => nav.IsVisible(property));
        Assert.Contains("IComparable", ex.Message);
    }

    // [Compare] (a stock DataAnnotations attribute, not something this engine wrote) already
    // works with zero engine code: ValidateCurrentStep's scalar branch builds its ValidationContext
    // as `new ValidationContext(_model) { MemberName = ... }` -- ObjectInstance is the *whole
    // model*, not just the one value being checked -- so CompareAttribute's own reflection lookup
    // of the other property off ValidationContext.ObjectInstance/ObjectType finds it correctly.
    // Proven here rather than assumed, per the same "don't infer, verify" standard as every other
    // edge case in this suite.
    private class PasswordModel
    {
        [FormStep(1)]
        public string Password { get; set; } = string.Empty;

        [FormStep(1)]
        [Compare(nameof(Password), ErrorMessage = "Passwords must match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    [Fact]
    public void CompareAttribute_already_works_via_the_full_model_ValidationContext_no_engine_change_needed()
    {
        var model = new PasswordModel { Password = "secret", ConfirmPassword = "different" };
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<PasswordModel>(), model);

        Assert.False(nav.ValidateCurrentStep(store));
        var messages = editContext.GetValidationMessages(new FieldIdentifier(model, nameof(PasswordModel.ConfirmPassword)));
        Assert.Contains("Passwords must match.", messages);
    }

    [Fact]
    public void CompareAttribute_passes_once_the_two_properties_match()
    {
        var model = new PasswordModel { Password = "secret", ConfirmPassword = "secret" };
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<PasswordModel>(), model);

        Assert.True(nav.ValidateCurrentStep(store));
    }

    // A broad proof pass for stock DataAnnotations validators that were never special-cased by
    // this engine -- they run through the exact same Validator.TryValidateValue(value, context,
    // property.Validators) call as [Required]/[DateRange]/etc. always have, so "any ValidationAttribute
    // just works" (README.md) covers these too. Each attribute gets its own single-property model
    // (rather than one shared model with 12 properties on one step) -- ValidateCurrentStep
    // validates every *visible* property of the current step together, so a shared model's other
    // default-empty properties would fail their own unrelated attributes and corrupt the result.
    private static bool Validates<TModel>(TModel model) where TModel : class, new()
    {
        var editContext = new EditContext(model);
        var store = new ValidationMessageStore(editContext);
        var nav = new WizardNavigator(WizardModelSchema.For<TModel>(), model);
        return nav.ValidateCurrentStep(store);
    }

    private class CreditCardModel
    {
        [FormStep(1)]
        [CreditCard(ErrorMessage = "Not a valid card number.")]
        public string Card { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("not-a-card", false)]
    [InlineData("4111111111111111", true)]
    public void CreditCardAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new CreditCardModel { Card = value }));
    }

    private class PhoneModel
    {
        [FormStep(1)]
        [Phone(ErrorMessage = "Not a valid phone number.")]
        public string Number { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("not-a-phone!!!", false)]
    [InlineData("+1 555-123-4567", true)]
    public void PhoneAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new PhoneModel { Number = value }));
    }

    private class UrlModel
    {
        [FormStep(1)]
        [Url(ErrorMessage = "Not a valid URL.")]
        public string Website { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("not a url", false)]
    [InlineData("https://example.com", true)]
    public void UrlAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new UrlModel { Website = value }));
    }

    private class RegularExpressionModel
    {
        [FormStep(1)]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Must be 5 digits.")]
        public string ZipCode { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("abc", false)]
    [InlineData("12345", true)]
    public void RegularExpressionAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new RegularExpressionModel { ZipCode = value }));
    }

    private class StringLengthModel
    {
        [FormStep(1)]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "Must be 2-5 characters.")]
        public string ShortCode { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("x", false)]
    [InlineData("toolongvalue", false)]
    [InlineData("ok", true)]
    public void StringLengthAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new StringLengthModel { ShortCode = value }));
    }

    private class LengthModel
    {
        [FormStep(1)]
        [Length(1, 3, ErrorMessage = "Must have 1-3 items.")]
        public List<string> LimitedTags { get; set; } = new();
    }

    [Fact]
    public void LengthAttribute_on_a_ListT_property_validates_through_the_existing_engine_path()
    {
        Assert.False(Validates(new LengthModel { LimitedTags = new List<string> { "a", "b", "c", "d" } }));
        Assert.True(Validates(new LengthModel { LimitedTags = new List<string> { "a", "b" } }));
    }

    private class MinLengthModel
    {
        [FormStep(1)]
        [MinLength(2, ErrorMessage = "Too short.")]
        public string AtLeastTwo { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("a", false)]
    [InlineData("ab", true)]
    public void MinLengthAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new MinLengthModel { AtLeastTwo = value }));
    }

    private class MaxLengthModel
    {
        [FormStep(1)]
        [MaxLength(3, ErrorMessage = "Too long.")]
        public string AtMostThree { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("toolong", false)]
    [InlineData("ok", true)]
    public void MaxLengthAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new MaxLengthModel { AtMostThree = value }));
    }

    private class AllowedValuesModel
    {
        [FormStep(1)]
        [AllowedValues("Red", "Green", "Blue", ErrorMessage = "Not an allowed color.")]
        public string Color { get; set; } = "Red";
    }

    [Theory]
    [InlineData("Purple", false)]
    [InlineData("Green", true)]
    public void AllowedValuesAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new AllowedValuesModel { Color = value }));
    }

    private class DeniedValuesModel
    {
        [FormStep(1)]
        [DeniedValues("Admin", ErrorMessage = "That value is reserved.")]
        public string Name { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("Admin", false)]
    [InlineData("Alice", true)]
    public void DeniedValuesAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new DeniedValuesModel { Name = value }));
    }

    private class EnumDataTypeModel
    {
        [FormStep(1)]
        [EnumDataType(typeof(Choice), ErrorMessage = "Not a valid choice.")]
        public string Value { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("NotAMember", false)]
    [InlineData("A", true)]
    public void EnumDataTypeAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new EnumDataTypeModel { Value = value }));
    }

    private class Base64StringModel
    {
        [FormStep(1)]
        [Base64String(ErrorMessage = "Not valid Base64.")]
        public string Encoded { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("not valid base64!", false)]
    [InlineData("aGVsbG8=", true)]
    public void Base64StringAttribute_validates_through_the_existing_engine_path(string value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new Base64StringModel { Encoded = value }));
    }

    // [CustomValidation] -- the extension point README.md points to for any rule with no
    // off-the-shelf attribute; proven here rather than just asserted to work. Must be a *public*
    // nested class -- CustomValidationAttribute reflects the validator type by name and throws
    // "must be public" for a private one, regardless of InternalsVisibleTo.
    public class CustomValidationModel
    {
        [FormStep(1)]
        [CustomValidation(typeof(CustomValidationModel), nameof(ValidateEven))]
        public int EvenNumber { get; set; }

        public static ValidationResult? ValidateEven(int value, ValidationContext context) =>
            value % 2 == 0 ? ValidationResult.Success : new ValidationResult("Must be even.");
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public void CustomValidationAttribute_validates_through_the_existing_engine_path(int value, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validates(new CustomValidationModel { EvenNumber = value }));
    }

    // Nested-target DependsOn (DESIGN-DISCUSSION.md M.1) -- a top-level property's [DependsOn]
    // reaching INTO a single-instance nested group's own field via a dotted path, resolved from
    // Model. CustomerInfo auto-expands (tier 3); StateTaxId only cares about its Country member.
    public class NestedTargetCustomerInfo
    {
        public string Country { get; set; } = "USA";
    }

    private class NestedTargetModel
    {
        [FormStep(1)]
        public NestedTargetCustomerInfo CustomerInfo { get; set; } = new();

        [FormStep(1)]
        [DependsOn("CustomerInfo.Country", "USA")]
        public string StateTaxId { get; set; } = string.Empty;
    }

    [Fact]
    public void Dotted_path_DependsOn_is_visible_when_the_nested_field_matches()
    {
        var model = new NestedTargetModel { CustomerInfo = new() { Country = "USA" } };
        var nav = new WizardNavigator(WizardModelSchema.For<NestedTargetModel>(), model);

        var property = WizardModelSchema.For<NestedTargetModel>().Steps[0].Properties
            .Single(p => p.Property.Name == nameof(NestedTargetModel.StateTaxId));

        Assert.True(nav.IsVisible(property));
    }

    [Fact]
    public void Dotted_path_DependsOn_is_hidden_when_the_nested_field_does_not_match()
    {
        var model = new NestedTargetModel { CustomerInfo = new() { Country = "Canada" } };
        var nav = new WizardNavigator(WizardModelSchema.For<NestedTargetModel>(), model);

        var property = WizardModelSchema.For<NestedTargetModel>().Steps[0].Properties
            .Single(p => p.Property.Name == nameof(NestedTargetModel.StateTaxId));

        Assert.False(nav.IsVisible(property));
    }

    [Fact]
    public void Dotted_path_DependsOn_treats_a_null_nested_segment_as_not_matching()
    {
        var model = new NestedTargetModel { CustomerInfo = null! };
        var nav = new WizardNavigator(WizardModelSchema.For<NestedTargetModel>(), model);

        var property = WizardModelSchema.For<NestedTargetModel>().Steps[0].Properties
            .Single(p => p.Property.Name == nameof(NestedTargetModel.StateTaxId));

        Assert.False(nav.IsVisible(property)); // no exception -- null along the path, not a match
    }
}
