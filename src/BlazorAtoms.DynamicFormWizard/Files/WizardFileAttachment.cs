namespace BlazorAtoms.DynamicFormWizard.Files;

/// <summary>
/// A file selected in the wizard, with its bytes already read into memory by the engine at
/// selection time (DESIGN-DISCUSSION.md E.15) -- not a raw <c>IBrowserFile</c> handle, whose
/// underlying stream is tied to the current circuit/render and can't be held indefinitely.
/// A property of type <c>IReadOnlyList&lt;WizardFileAttachment&gt;</c> is "0 or more files" --
/// an empty list is the default, valid state; pair with a <c>MinFileCount</c>-style validator if
/// at least one file is required.
/// </summary>
public sealed record WizardFileAttachment(string FileName, string ContentType, long Size, byte[] Content);
