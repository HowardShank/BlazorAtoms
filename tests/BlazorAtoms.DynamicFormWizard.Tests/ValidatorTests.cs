using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BlazorAtoms.DynamicFormWizard.Files;

namespace BlazorAtoms.DynamicFormWizard.Tests;

public class ValidatorTests
{
    private class Dummy
    {
        public string? Text { get; set; }
        public DateTime Date { get; set; }
        public IReadOnlyList<WizardFileAttachment>? Files { get; set; }
    }

    private static ValidationContext ContextFor(string memberName) =>
        new(new Dummy()) { MemberName = memberName };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FormRegex_lets_Required_handle_empty_values(string? value)
    {
        var attr = new FormRegexAttribute(@"^\d+$", "Digits only.");
        var result = attr.GetValidationResult(value, ContextFor(nameof(Dummy.Text)));
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void FormRegex_passes_a_matching_value()
    {
        var attr = new FormRegexAttribute(@"^\d+$", "Digits only.");
        var result = attr.GetValidationResult("12345", ContextFor(nameof(Dummy.Text)));
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void FormRegex_fails_a_non_matching_value_with_the_given_message()
    {
        var attr = new FormRegexAttribute(@"^\d+$", "Digits only.");
        var result = attr.GetValidationResult("abc", ContextFor(nameof(Dummy.Text)));

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Equal("Digits only.", result!.ErrorMessage);
    }

    [Fact]
    public void DateRange_passes_a_date_inside_the_window()
    {
        var attr = new DateRangeAttribute(minDaysFromNow: 1, maxDaysFromNow: 90);
        var result = attr.GetValidationResult(DateTime.Now.AddDays(10), ContextFor(nameof(Dummy.Date)));
        Assert.Equal(ValidationResult.Success, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(200)]
    public void DateRange_fails_a_date_outside_the_window(int daysFromNow)
    {
        var attr = new DateRangeAttribute(minDaysFromNow: 1, maxDaysFromNow: 90);
        var result = attr.GetValidationResult(DateTime.Now.AddDays(daysFromNow), ContextFor(nameof(Dummy.Date)));
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void DateRange_fails_a_non_date_value_instead_of_throwing()
    {
        var attr = new DateRangeAttribute(minDaysFromNow: 1, maxDaysFromNow: 90);
        var result = attr.GetValidationResult("not a date", ContextFor(nameof(Dummy.Date)));
        Assert.NotEqual(ValidationResult.Success, result);
    }

    private static WizardFileAttachment MakeFile(string name, long size) => new(name, "text/plain", size, []);

    [Fact]
    public void MaxFileCount_passes_a_null_collection_zero_or_more_is_the_default()
    {
        var attr = new MaxFileCountAttribute(2);
        var result = attr.GetValidationResult(null, ContextFor(nameof(Dummy.Files)));
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void MaxFileCount_fails_once_the_limit_is_exceeded()
    {
        var attr = new MaxFileCountAttribute(1);
        var files = new List<WizardFileAttachment> { MakeFile("a.txt", 10), MakeFile("b.txt", 10) };

        var result = attr.GetValidationResult(files, ContextFor(nameof(Dummy.Files)));

        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void MaxFileSize_fails_when_any_single_file_exceeds_the_limit()
    {
        var attr = new MaxFileSizeAttribute(100);
        var files = new List<WizardFileAttachment> { MakeFile("small.txt", 50), MakeFile("big.txt", 200) };

        var result = attr.GetValidationResult(files, ContextFor(nameof(Dummy.Files)));

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("big.txt", result!.ErrorMessage);
    }

    [Fact]
    public void MaxFileSize_passes_when_every_file_is_within_the_limit()
    {
        var attr = new MaxFileSizeAttribute(100);
        var files = new List<WizardFileAttachment> { MakeFile("a.txt", 50), MakeFile("b.txt", 99) };

        var result = attr.GetValidationResult(files, ContextFor(nameof(Dummy.Files)));

        Assert.Equal(ValidationResult.Success, result);
    }

    [Theory]
    [InlineData("photo.png", true)]
    [InlineData("photo.PNG", true)]
    [InlineData("document.pdf", true)]
    [InlineData("script.exe", false)]
    public void AllowedExtensions_is_case_insensitive_and_rejects_anything_not_listed(string fileName, bool expectedValid)
    {
        var attr = new AllowedExtensionsAttribute("png", ".pdf");
        var files = new List<WizardFileAttachment> { MakeFile(fileName, 10) };

        var result = attr.GetValidationResult(files, ContextFor(nameof(Dummy.Files)));

        Assert.Equal(expectedValid, result == ValidationResult.Success);
    }
}
