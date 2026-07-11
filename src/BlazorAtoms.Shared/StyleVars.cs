using System.Globalization;
using System.Text;

namespace BlazorAtoms.Shared;

/// <summary>
/// Fluent builder for a component's inline CSS-custom-property string. Emits
/// <c>--{prefix}-{name}:{value};</c> only for non-null values, so callers can chain every optional
/// token and get a minimal <c>style</c> string with no empty/blank declarations. The <see cref="double"/>
/// overload appends <c>px</c> and formats invariant-culture (no locale decimal commas).
/// </summary>
public sealed class StyleVars(string prefix)
{
    private readonly StringBuilder _sb = new();

    /// <summary>Append <c>--{prefix}-{name}:{v};</c> when <paramref name="v"/> is non-empty.</summary>
    public StyleVars Add(string name, string? v)
    {
        if (!string.IsNullOrEmpty(v)) _sb.Append($"--{prefix}-{name}:{v};");
        return this;
    }

    /// <summary>Append <c>--{prefix}-{name}:{v}px;</c> when <paramref name="v"/> has a value.</summary>
    public StyleVars Add(string name, double? v)
    {
        if (v is not null) _sb.Append($"--{prefix}-{name}:{v.Value.ToString(CultureInfo.InvariantCulture)}px;");
        return this;
    }

    public override string ToString() => _sb.ToString();
}
