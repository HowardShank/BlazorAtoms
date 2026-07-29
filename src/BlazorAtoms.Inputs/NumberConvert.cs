using System.Globalization;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Converts between <see cref="AtomNumberField{TValue}"/>'s generic <c>TValue</c> and the strings a
/// native <c>&lt;input type="number"&gt;</c> speaks. Separate from <see cref="RangeConvert"/> for one
/// reason: a number field can legitimately be <b>empty</b>, which a slider can't, so parsing has to
/// distinguish "cleared" from "unparseable" instead of collapsing both to <c>0</c>.
/// </summary>
/// <remarks>
/// Culture is always invariant: per the HTML spec a number input's <c>value</c> is a
/// floating-point literal with a <c>.</c> decimal separator regardless of the user's locale, so
/// parsing it with the ambient culture would break on e.g. <c>de-DE</c>.
/// </remarks>
internal static class NumberConvert
{
    /// <summary>Whether <c>TValue</c> can hold "no value" — a nullable value type
    /// (<c>int?</c>) or a reference type.</summary>
    public static bool IsNullable<TValue>() =>
        Nullable.GetUnderlyingType(typeof(TValue)) is not null || !typeof(TValue).IsValueType;

    /// <summary>Value → the input's <c>value</c> attribute. Null renders as an empty box.</summary>
    public static string Format<TValue>(TValue value) =>
        value is null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

    /// <summary>Formats a <c>Min</c>/<c>Max</c>/<c>Step</c> bound; null omits the attribute.</summary>
    public static string? FormatBound(double? value) =>
        value?.ToString(CultureInfo.InvariantCulture);

    /// <summary>Parses what the browser reported. Returns false — meaning "leave the current value
    /// alone" — for input the target type can't represent: an unparseable string, an out-of-range
    /// number, or a cleared box when <c>TValue</c> is non-nullable.</summary>
    public static bool TryParse<TValue>(string? raw, out TValue result)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            result = default!;
            return IsNullable<TValue>();
        }

        var target = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        try
        {
            result = (TValue)Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException)
        {
            // e.g. "3.7" into an int field, or 1e400 into a double. The browser may hold text the
            // type can't take; rejecting it keeps Value valid instead of throwing mid-render.
            result = default!;
            return false;
        }
    }
}
