using System.Globalization;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Converts between <see cref="AtomRangeInput{TValue}"/>'s generic <c>TValue</c>
/// (<c>int</c>/<c>long</c>/<c>short</c>/<c>float</c>/<c>double</c>/<c>decimal</c> and their nullable
/// variants) and the <see cref="double"/> the native <c>&lt;input type="range"&gt;</c> speaks in its
/// string-based <c>value</c>/<c>min</c>/<c>max</c>/<c>step</c> attributes. No generic constraint on
/// <c>TValue</c> — nullable value types don't satisfy <c>INumber&lt;T&gt;</c> — same shape as
/// Blazor's own <c>InputNumber&lt;TValue&gt;</c>. Precision loss between <c>decimal</c> and
/// <c>double</c> is a non-issue here: this drives a UI slider, not a financial calculation.
/// </summary>
internal static class RangeConvert
{
    public static double ToDouble<TValue>(TValue value) =>
        value is null ? 0 : Convert.ToDouble(value, CultureInfo.InvariantCulture);

    public static TValue FromDouble<TValue>(double value)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        return (TValue)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    public static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
