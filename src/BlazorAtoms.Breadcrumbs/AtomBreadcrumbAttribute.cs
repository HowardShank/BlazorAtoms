namespace BlazorAtoms.Breadcrumbs;

/// <summary>
/// Declares an <c>@page</c> component's place in the static breadcrumb hierarchy. Discovered once
/// via reflection and cached for the process lifetime — see <see cref="AtomBreadcrumbGraph"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AtomBreadcrumbAttribute : Attribute
{
    public AtomBreadcrumbAttribute(string title) => Title = title;

    /// <summary>Display title template, e.g. <c>"Customer: {name}"</c>. Tokens are resolved via
    /// <see cref="AtomBreadcrumbService.SetData"/>/<see cref="AtomBreadcrumbService.SetDataAsync"/>.</summary>
    public string Title { get; }

    /// <summary>Route template of this page's parent in the static chain (e.g. <c>"/customers"</c>),
    /// or null if this page has no static parent.</summary>
    public string? ParentRoute { get; init; }

    /// <summary>When true, this page always resets the trail's dynamic tail on visit. Only takes
    /// effect when the provider has no <c>IsRootRoute</c> predicate supplied.</summary>
    public bool IsRoot { get; init; }

    /// <summary>
    /// Tooltip value when hover over breadcrumb.
    /// </summary>
    public string? Tooltip { get; init; }
}
