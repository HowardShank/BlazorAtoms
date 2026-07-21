using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Breadcrumbs.Tests;

// Plain marker types carrying [Route] + [AtomBreadcrumb] so AtomBreadcrumbGraph's reflection scan
// (which walks every loaded assembly, including this test assembly) discovers them. Route
// templates are namespaced per scenario to avoid colliding with fixtures from other test classes,
// since the graph is a single process-lifetime cache shared across the whole test run.

[Route("/test/svc/root")]
[AtomBreadcrumb("Root")]
public sealed class ServiceRootPage;

[Route("/test/svc/child")]
[AtomBreadcrumb("Child", ParentRoute = "/test/svc/root")]
public sealed class ServiceChildPage;

[Route("/test/svc/grandchild/{id}")]
[AtomBreadcrumb("Item: {name}", ParentRoute = "/test/svc/child")]
public sealed class ServiceGrandchildPage;

[Route("/test/svc/rooted-child")]
[AtomBreadcrumb("Rooted child", ParentRoute = "/test/svc/root", IsRoot = true)]
public sealed class ServiceRootedChildPage;

[Route("/test/svc/cycle-a")]
[AtomBreadcrumb("Cycle A", ParentRoute = "/test/svc/cycle-b")]
public sealed class ServiceCycleAPage;

[Route("/test/svc/cycle-b")]
[AtomBreadcrumb("Cycle B", ParentRoute = "/test/svc/cycle-a")]
public sealed class ServiceCycleBPage;

[Route("/test/svc/orphan")]
[AtomBreadcrumb("Orphan", ParentRoute = "/test/svc/does-not-exist")]
public sealed class ServiceOrphanPage;

[Route("/test/svc/tooltip")]
[AtomBreadcrumb("Tooltipped", Tooltip = "Extra context")]
public sealed class ServiceTooltipPage;

public sealed class ServiceUnattributedPage;
