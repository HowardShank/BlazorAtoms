using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.ActivityIndicators;

/// <summary>
/// Wrapper that renders one of the <c>AtomActivity*</c> SVG indicators — the non-abstract subclasses
/// of <see cref="AtomActivityIndicatorBase"/> living in the
/// <c>BlazorAtoms.ActivityIndicators.Indicators</c> namespace.
/// <para>
/// Set <see cref="Name"/> to render a specific indicator; leave it null/empty to render a
/// random one. The candidate set is discovered by reflection (every non-abstract
/// <see cref="AtomActivityIndicatorBase"/>), so adding or removing an <c>AtomActivity*.razor</c> in the
/// Indicators folder requires no change here.
/// </para>
/// </summary>
public partial class AtomActivityIndicator : AtomComponentBase
{
    /// <summary>
    /// All discoverable indicator component types, computed once per process: non-abstract
    /// subclasses of <see cref="AtomActivityIndicatorBase"/>. The wrapper is not one, so it cannot
    /// match itself. Trimming-safe via ILLink.Descriptors.xml (roots the Indicators namespace).
    /// </summary>
    private static readonly Type[] Candidates =
        typeof(AtomActivityIndicator).Assembly.GetTypes()
            .Where(t => typeof(AtomActivityIndicatorBase).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

    // Per-type cache of declared [Parameter] property names, so we never forward a parameter
    // the target component doesn't declare (DynamicComponent throws if we do).
    private static readonly ConcurrentDictionary<Type, HashSet<string>> ParamNames = new();

    /// <summary>Indicator to render. Null/empty selects a random indicator. Matched
    /// case-insensitively against the component type name, accepting "AtomActivityGears",
    /// "ActivityGears" and "Gears".</summary>
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

    /// <summary>Invoked with the requested <see cref="Name"/> when no indicator matches it,
    /// just before falling back to a random indicator. No-op if unbound.</summary>
    [Parameter] public EventCallback<string> OnUnknownName { get; set; }

    private Type? ResolvedType;
    private Type? _randomPick;

    protected override async Task OnParametersSetAsync()
    {
        // Honor cancellation up front: resolve nothing so the render path emits no indicator.
        if (CancellationToken.IsCancellationRequested) { ResolvedType = null; return; }

        if (Candidates.Length == 0) { ResolvedType = null; return; }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            var hit = Candidates.FirstOrDefault(t => NameMatches(t.Name, Name));
            if (hit is not null) { ResolvedType = hit; return; }

            // Unknown name: notify the host, then fall back to random. Never throw.
            await OnUnknownName.InvokeAsync(Name);

            // The host callback may have been slow; re-check before continuing.
            if (CancellationToken.IsCancellationRequested) { ResolvedType = null; return; }
        }

        // Pick once and remember, so re-renders don't flicker to a different indicator.
        _randomPick ??= Candidates[Random.Shared.Next(Candidates.Length)];
        ResolvedType = _randomPick;
    }

    // Accepts the full type name ("AtomActivityGears"), the name without the Atom prefix
    // ("ActivityGears"), and the bare form ("Gears") — all case-insensitive.
    private static bool NameMatches(string typeName, string requested) =>
        string.Equals(typeName, requested, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(typeName, "Atom" + requested, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(typeName, "AtomActivity" + requested, StringComparison.OrdinalIgnoreCase);

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
        // Non-null struct: forward like Size so the indicator honors the same token.
        if (declared.Contains("CancellationToken")) dict["CancellationToken"] = CancellationToken;
        Add("Blip", Blip);
        Add("Glow", Glow);
        Add("Line", Line);
        Add("Fill", Fill);
        Add("CssClass", CssClass);
        Add("Style", Style);
        Add("AdditionalAttributes", AdditionalAttributes);
        return dict;
    }
}
