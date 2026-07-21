

namespace BlazorAtoms.Breadcrumbs.Tests;

/// <summary>
/// Unit tests for <see cref="AtomBreadcrumbService"/> — headless, no bUnit rendering needed.
/// Covers the static-chain build, token resolution + generation-guarded async staleness, the
/// dynamic-tail Push/truncate-to-ancestor algorithm (including that a matching Push replaces an
/// entry's content, not just its position), the automatic URL-derived fallback entry for
/// unattributed pages, root reset, and the per-chain-walk cycle / orphan-route failures.
/// </summary>
public class AtomBreadcrumbServiceTests
{
    private static readonly IReadOnlyDictionary<string, object?> NoRouteValues = new Dictionary<string, object?>();

    [Fact]
    public void OnNavigated_BuildsStaticChainRootToCurrent()
    {
        var service = new AtomBreadcrumbService();

        service.OnNavigated(new RouteData(typeof(ServiceChildPage), NoRouteValues), "/test/svc/child", false);

        Assert.Collection(service.Trail,
            e => Assert.Equal("Root", e.Title),
            e => Assert.Equal("Child", e.Title));
        Assert.Equal("/test/svc/child", service.Trail[^1].Href);
        Assert.Equal("/test/svc/root", service.Trail[0].Href);
    }

    [Fact]
    public void OnNavigated_AttributeTooltip_FlowsToEntry()
    {
        var service = new AtomBreadcrumbService();

        service.OnNavigated(new RouteData(typeof(ServiceTooltipPage), NoRouteValues), "/test/svc/tooltip", false);

        Assert.Equal("Extra context", service.Trail[^1].Tooltip);
    }

    [Fact]
    public void OnNavigated_NoTooltipOnAttribute_EntryTooltipIsNull()
    {
        var service = new AtomBreadcrumbService();

        service.OnNavigated(new RouteData(typeof(ServiceChildPage), NoRouteValues), "/test/svc/child", false);

        Assert.Null(service.Trail[^1].Tooltip);
    }

    [Fact]
    public void OnNavigated_UnresolvedTitleTokenIsPending_ThenResolvedViaSetData()
    {
        var service = new AtomBreadcrumbService();
        var routeValues = new Dictionary<string, object?> { ["id"] = "42" };

        service.OnNavigated(new RouteData(typeof(ServiceGrandchildPage), routeValues), "/test/svc/grandchild/42", false);

        var current = service.Trail[^1];
        Assert.True(current.IsTitlePending);

        service.SetData("name", "Widget");

        current = service.Trail[^1];
        Assert.False(current.IsTitlePending);
        Assert.Equal("Item: Widget", current.Title);
    }

    [Fact]
    public async Task SetDataAsync_StaleResultAfterNewNavigationIsDiscarded()
    {
        var service = new AtomBreadcrumbService();
        var routeValues = new Dictionary<string, object?> { ["id"] = "42" };
        service.OnNavigated(new RouteData(typeof(ServiceGrandchildPage), routeValues), "/test/svc/grandchild/42", false);

        var tcs = new TaskCompletionSource<string>();
        var pending = service.SetDataAsync("name", tcs.Task);

        service.OnNavigated(new RouteData(typeof(ServiceChildPage), NoRouteValues), "/test/svc/child", false);
        tcs.SetResult("Stale value");
        await pending;

        Assert.Equal("Child", service.Trail[^1].Title);
        Assert.False(service.Trail[^1].IsTitlePending);
    }

    [Fact]
    public void Push_AppendsDynamicTailAfterStaticHead()
    {
        var service = new AtomBreadcrumbService();
        service.OnNavigated(new RouteData(typeof(ServiceChildPage), NoRouteValues), "/test/svc/child", false);

        service.Push(new AtomBreadcrumbEntry { Title = "Widget A", Key = "/widgets/a" });

        Assert.Collection(service.Trail,
            e => Assert.Equal("Root", e.Title),
            e => Assert.Equal("Child", e.Title),
            e => Assert.Equal("Widget A", e.Title));
    }

    [Fact]
    public void Push_RevisitingDynamicTailEntry_TruncatesToAncestor()
    {
        var service = new AtomBreadcrumbService();
        service.OnNavigated(new RouteData(typeof(ServiceChildPage), NoRouteValues), "/test/svc/child", false);
        service.Push(new AtomBreadcrumbEntry { Title = "A", Key = "/a" });
        service.Push(new AtomBreadcrumbEntry { Title = "B", Key = "/b" });
        service.Push(new AtomBreadcrumbEntry { Title = "C", Key = "/c" });

        service.Push(new AtomBreadcrumbEntry { Title = "A again", Key = "/a" });

        Assert.Collection(service.Trail,
            e => Assert.Equal("Root", e.Title),
            e => Assert.Equal("Child", e.Title),
            e => Assert.Equal("A again", e.Title)); // truncate-to-ancestor also refreshes the matched entry's content

        service.Push(new AtomBreadcrumbEntry { Title = "D", Key = "/d" });

        Assert.Collection(service.Trail,
            e => Assert.Equal("Root", e.Title),
            e => Assert.Equal("Child", e.Title),
            e => Assert.Equal("A again", e.Title),
            e => Assert.Equal("D", e.Title));
    }

    [Fact]
    public void Push_RevisitingStaticHeadEntry_ClearsDynamicTailEntirely()
    {
        var service = new AtomBreadcrumbService();
        service.OnNavigated(new RouteData(typeof(ServiceChildPage), NoRouteValues), "/test/svc/child", false);
        service.Push(new AtomBreadcrumbEntry { Title = "X", Key = "/x" });

        service.Push(new AtomBreadcrumbEntry { Title = "Child again", Key = "/test/svc/child" });

        Assert.Collection(service.Trail,
            e => Assert.Equal("Root", e.Title),
            e => Assert.Equal("Child", e.Title));
    }

    [Fact]
    public void OnNavigated_UnattributedPage_AutoAddsUrlDerivedFallbackEntry()
    {
        var service = new AtomBreadcrumbService();

        service.OnNavigated(new RouteData(typeof(ServiceUnattributedPage), NoRouteValues), "/test/svc/my-draft", false);

        Assert.Collection(service.Trail, e => Assert.Equal("My Draft", e.Title));
    }

    [Fact]
    public void Push_ReplacesAutoFallbackEntry_ForSameKey()
    {
        var service = new AtomBreadcrumbService();
        service.OnNavigated(new RouteData(typeof(ServiceUnattributedPage), NoRouteValues), "/test/svc/my-draft", false);

        service.Push(new AtomBreadcrumbEntry { Title = "Draft #482", Key = "/test/svc/my-draft" });

        Assert.Collection(service.Trail, e => Assert.Equal("Draft #482", e.Title));
    }

    [Fact]
    public void OnNavigated_RootReset_ClearsDynamicTail_IncludingAutoFallback()
    {
        var service = new AtomBreadcrumbService();
        service.OnNavigated(new RouteData(typeof(ServiceUnattributedPage), NoRouteValues), "/test/svc/unattributed-1", false);
        service.Push(new AtomBreadcrumbEntry { Title = "Pushed", Key = "/pushed" });
        Assert.Equal(2, service.Trail.Count); // auto-fallback for unattributed-1, plus the explicit push

        service.OnNavigated(new RouteData(typeof(ServiceUnattributedPage), NoRouteValues), "/test/svc/unattributed-2", true);

        // root-reset clears the old tail before the new auto-fallback entry is appended for the new uri.
        Assert.Collection(service.Trail, e => Assert.Equal("Unattributed 2", e.Title));
    }

    [Fact]
    public void OnNavigated_AttributeIsRootHonoredByProvider_ClearsDynamicTail()
    {
        var service = new AtomBreadcrumbService();
        service.OnNavigated(new RouteData(typeof(ServiceChildPage), NoRouteValues), "/test/svc/child", false);
        service.Push(new AtomBreadcrumbEntry { Title = "Pushed", Key = "/pushed" });

        // Provider evaluates IsRoot from the attribute when no predicate is supplied; simulated
        // here by passing isRoot: true directly, as AtomBreadcrumbProvider would for this page.
        service.OnNavigated(new RouteData(typeof(ServiceRootedChildPage), NoRouteValues), "/test/svc/rooted-child", true);

        Assert.Collection(service.Trail,
            e => Assert.Equal("Root", e.Title),
            e => Assert.Equal("Rooted child", e.Title));
    }

    [Fact]
    public void OnNavigated_CyclicParentChain_Throws()
    {
        var service = new AtomBreadcrumbService();

        Assert.Throws<InvalidOperationException>(() =>
            service.OnNavigated(new RouteData(typeof(ServiceCycleAPage), NoRouteValues), "/test/svc/cycle-a", false));
    }

    [Fact]
    public void OnNavigated_OrphanParentRoute_Throws()
    {
        var service = new AtomBreadcrumbService();

        Assert.Throws<InvalidOperationException>(() =>
            service.OnNavigated(new RouteData(typeof(ServiceOrphanPage), NoRouteValues), "/test/svc/orphan", false));
    }
}
