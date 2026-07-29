namespace BlazorAtoms.Inputs;

/// <summary>
/// Which text-like <c>&lt;input type&gt;</c> <see cref="AtomTextField"/> renders. All of these keep
/// a string value and support the native <c>readonly</c>/<c>maxlength</c> attributes; the type only
/// changes browser affordances (mobile keyboard, autofill hints, masking, the search clear button).
/// </summary>
public enum TextFieldType
{
    Text,
    Email,
    Url,
    Tel,
    Search,
    Password,
}
