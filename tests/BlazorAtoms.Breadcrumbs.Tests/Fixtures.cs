using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Breadcrumbs.Tests;

// Marker page types carrying [Route] + [AtomBreadcrumb] so AtomBreadcrumbGraph's reflection scan
// (which walks every loaded assembly, including this test assembly) discovers them. Route
// templates are namespaced per scenario to avoid colliding with fixtures from other test classes,
// since the graph is a single process-lifetime cache shared across the whole test run.
//
// They derive from ComponentBase because .NET 10's RouteData(Type pageType, ...) constructor now
// validates that pageType implements IComponent (net9 did not) — a bare marker class throws
// ArgumentException. ComponentBase is the lightest way to satisfy that; no rendering happens here.

[Route("/test/svc/root")]
[AtomBreadcrumb("Root")]
public sealed class ServiceRootPage : ComponentBase;

[Route("/test/svc/child")]
[AtomBreadcrumb("Child", ParentRoute = "/test/svc/root")]
public sealed class ServiceChildPage : ComponentBase;

[Route("/test/svc/grandchild/{id}")]
[AtomBreadcrumb("Item: {name}", ParentRoute = "/test/svc/child")]
public sealed class ServiceGrandchildPage : ComponentBase;

[Route("/test/svc/rooted-child")]
[AtomBreadcrumb("Rooted child", ParentRoute = "/test/svc/root", IsRoot = true)]
public sealed class ServiceRootedChildPage : ComponentBase;

[Route("/test/svc/cycle-a")]
[AtomBreadcrumb("Cycle A", ParentRoute = "/test/svc/cycle-b")]
public sealed class ServiceCycleAPage : ComponentBase;

[Route("/test/svc/cycle-b")]
[AtomBreadcrumb("Cycle B", ParentRoute = "/test/svc/cycle-a")]
public sealed class ServiceCycleBPage : ComponentBase;

[Route("/test/svc/orphan")]
[AtomBreadcrumb("Orphan", ParentRoute = "/test/svc/does-not-exist")]
public sealed class ServiceOrphanPage : ComponentBase;

[Route("/test/svc/tooltip")]
[AtomBreadcrumb("Tooltipped", Tooltip = "Extra context")]
public sealed class ServiceTooltipPage : ComponentBase;

public sealed class ServiceUnattributedPage : ComponentBase;
