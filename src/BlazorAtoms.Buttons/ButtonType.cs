namespace BlazorAtoms.Buttons;

/// <summary>
/// Native <c>type</c> attribute. Ignored when <c>Href</c> is set (an <c>&lt;a&gt;</c> has no type) — and
/// worth setting deliberately inside a form, where the HTML default is <c>submit</c>.
/// </summary>
public enum ButtonType
{
    /// <summary>Does nothing on its own; only fires <c>OnClick</c>. Default — deliberately not the
    /// HTML default, so a button inside a form never submits by accident.</summary>
    Button,

    Submit,
    Reset,
}
