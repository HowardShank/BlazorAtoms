using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Breadcrumbs;

/// <summary>
/// Scopes a breadcrumb trail to its position in the render tree (decision 5) — nest this anywhere
/// (e.g. inside a section layout) to give that subtree its own independent trail. Requires the
/// app's <c>&lt;Router&gt;&lt;Found&gt;</c> content to cascade <c>RouteData</c> (decision 9):
/// <code>
/// &lt;Found Context="routeData"&gt;
///     &lt;CascadingValue Value="routeData"&gt;
///         &lt;RouteView RouteData="routeData" ... /&gt;
///     &lt;/CascadingValue&gt;
/// &lt;/Found&gt;
/// </code>
/// </summary>
public partial class AtomBreadcrumbProvider : ComponentBase
{
    [CascadingParameter] private RouteData? RouteData { get; set; }

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Decides whether a route resets the trail's dynamic tail (decision 3). When
    /// supplied, this takes precedence over each page's own <see cref="AtomBreadcrumbAttribute.IsRoot"/>.</summary>
    [Parameter] public Func<string, bool>? IsRootRoute { get; set; }

    /// <summary>External title lookup for unattributed pages — see <see cref="AtomBreadcrumbService.TitleResolver"/>.
    /// Wired through here so it can be set declaratively at the same mount point as <see cref="IsRootRoute"/>,
    /// instead of the consumer having to reach the cascaded service instance directly.</summary>
    [Parameter] public Func<string?, string, string?>? TitleResolver { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private readonly AtomBreadcrumbService _atomsBreadcrumbService = new();

    protected override void OnParametersSet()
    {
        if (RouteData is null) return;

        _atomsBreadcrumbService.TitleResolver = TitleResolver;

        var uri = NavigationManager.Uri;
        var isRoot = IsRootRoute?.Invoke(uri) ?? EvaluateAttributeIsRoot();
        //_atomsBreadcrumbService.OnNavigated(RouteData.PageType, RouteData.RouteValues, uri, isRoot);
        _atomsBreadcrumbService.OnNavigated(RouteData, uri, isRoot);

    }

    private bool EvaluateAttributeIsRoot() =>
        RouteData?.PageType is not null &&
        AtomBreadcrumbGraph.Instance.NodesByType.TryGetValue(RouteData.PageType, out var node) &&
        node.Attribute.IsRoot;
}
