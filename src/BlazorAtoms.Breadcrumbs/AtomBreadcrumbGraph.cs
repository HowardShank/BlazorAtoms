using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Breadcrumbs;

/// <summary>One <c>[AtomBreadcrumb]</c>-attributed page: its metadata plus the route template it
/// was discovered under, for <see cref="AtomBreadcrumbAttribute.ParentRoute"/> lookups.</summary>
internal sealed class AtomBreadcrumbNode
{
    public required Type PageType { get; init; }
    public required AtomBreadcrumbAttribute Attribute { get; init; }
    public required string OwnRouteTemplate { get; init; }
}

/// <summary>Process-lifetime cache of every <c>[AtomBreadcrumb]</c>-attributed page found across
/// loaded assemblies, keyed by page type and by route template. Built once, lazily, on first
/// access (decision 8). <see cref="AtomBreadcrumbAttribute.ParentRoute"/> resolution and cycle
/// detection happen per-chain-walk in <see cref="AtomBreadcrumbService"/>, not here — validating
/// the whole graph eagerly would let one misconfigured page anywhere in an app break breadcrumbs
/// for every other page.</summary>
internal sealed class AtomBreadcrumbGraph
{
    private static readonly Lazy<AtomBreadcrumbGraph> LazyInstance = new(Build);

    public static AtomBreadcrumbGraph Instance => LazyInstance.Value;

    public required IReadOnlyDictionary<Type, AtomBreadcrumbNode> NodesByType { get; init; }
    public required IReadOnlyDictionary<string, Type> TypeByTemplate { get; init; }

    private static AtomBreadcrumbGraph Build()
    {
        var nodesByType = new Dictionary<Type, AtomBreadcrumbNode>();
        var typeByTemplate = new Dictionary<string, Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).ToArray()!;
            }

            foreach (var type in types)
            {
                var breadcrumbAttribute = type.GetCustomAttribute<AtomBreadcrumbAttribute>();
                if (breadcrumbAttribute is null) continue;

                var routeAttributes = type.GetCustomAttributes<RouteAttribute>().ToArray();
                if (routeAttributes.Length == 0) continue;

                nodesByType[type] = new AtomBreadcrumbNode
                {
                    PageType = type,
                    Attribute = breadcrumbAttribute,
                    OwnRouteTemplate = routeAttributes[0].Template,
                };
                foreach (var routeAttribute in routeAttributes)
                {
                    typeByTemplate[routeAttribute.Template] = type;
                }
            }
        }

        return new AtomBreadcrumbGraph { NodesByType = nodesByType, TypeByTemplate = typeByTemplate };
    }
}
