using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.ActivityIndicators;

/// <summary>
/// Wrapper that renders one of the <c>Busy*</c> SVG indicators living in the
/// <c>BlazorAtoms.ActivityIndicators.Indicators</c> namespace.
/// <para>
/// Set <see cref="Name"/> to render a specific indicator; leave it null/empty to render a
/// random one. The candidate set is discovered by reflection (namespace + <c>Busy</c> prefix),
/// so adding or removing a <c>Busy*.razor</c> in the Indicators folder requires no change here.
/// </para>
/// </summary>
public partial class ActivityIndicator : ComponentBase
{
    // The indicators live in the wrapper's namespace + ".Indicators".
    private static readonly string IndicatorsNamespace = typeof(ActivityIndicator).Namespace + ".Indicators";

    /// <summary>
    /// All discoverable indicator component types, computed once per process. Filtered to
    /// non-abstract <see cref="ComponentBase"/> types in the Indicators namespace whose name
    /// starts with "Busy". The wrapper is in the parent namespace and is not named Busy*, so it
    /// cannot match itself. Trimming-safe via ILLink.Descriptors.xml (roots the Indicators namespace).
    /// </summary>
    private static readonly Type[] Candidates =
        typeof(ActivityIndicator).Assembly.GetTypes()
            .Where(t => typeof(ComponentBase).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && t.Namespace == IndicatorsNamespace
                     && t.Name.StartsWith("Busy", StringComparison.Ordinal))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

    // Per-type cache of declared [Parameter] property names, so we never forward a parameter
    // the target component doesn't declare (DynamicComponent throws if we do).
    private static readonly ConcurrentDictionary<Type, HashSet<string>> ParamNames = new();

    /// <summary>Indicator to render. Null/empty selects a random indicator. Matched
    /// case-insensitively against the component type name, accepting both "BusyGears" and "Gears".</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>Rendered width/height in pixels. Forwarded to every indicator.</summary>
    [Parameter] public int Size { get; set; } = 48;

    /// <summary>Primary moving element color. Forwarded only if the chosen indicator declares it.</summary>
    [Parameter] public string? Blip { get; set; }

    /// <summary>Highlight / accent color. Forwarded only if the chosen indicator declares it.</summary>
    [Parameter] public string? Glow { get; set; }

    /// <summary>Structural stroke color. Forwarded only if the chosen indicator declares it.</summary>
    [Parameter] public string? Line { get; set; }

    /// <summary>Faint body fill color. Forwarded only if the chosen indicator declares it.</summary>
    [Parameter] public string? Fill { get; set; }

    /// <summary>Extra CSS class(es) applied to the chosen indicator's root svg element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Invoked with the requested <see cref="Name"/> when no indicator matches it,
    /// just before falling back to a random indicator. No-op if unbound.</summary>
    [Parameter] public EventCallback<string> OnUnknownName { get; set; }

    private Type? ResolvedType;
    private Type? _randomPick;

    protected override async Task OnParametersSetAsync()
    {
        if (Candidates.Length == 0) { ResolvedType = null; return; }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            var hit = Candidates.FirstOrDefault(t =>
                string.Equals(t.Name, Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.Name, "Busy" + Name, StringComparison.OrdinalIgnoreCase));
            if (hit is not null) { ResolvedType = hit; return; }

            // Unknown name: notify the host, then fall back to random. Never throw.
            await OnUnknownName.InvokeAsync(Name);
        }

        // Pick once and remember, so re-renders don't flicker to a different indicator.
        _randomPick ??= Candidates[Random.Shared.Next(Candidates.Length)];
        ResolvedType = _randomPick;
    }

    private static HashSet<string> DeclaredParams(Type t) =>
        ParamNames.GetOrAdd(t, ty => ty
            .GetProperties()
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal));

    private IDictionary<string, object> BuildParameters()
    {
        var declared = DeclaredParams(ResolvedType!);
        var dict = new Dictionary<string, object>();

        void Add(string key, object? val)
        {
            if (val is not null && declared.Contains(key)) dict[key] = val;
        }

        if (declared.Contains("Size")) dict["Size"] = Size;   // non-null int
        Add("Blip", Blip);
        Add("Glow", Glow);
        Add("Line", Line);
        Add("Fill", Fill);
        Add("Class", Class);
        return dict;
    }
}
