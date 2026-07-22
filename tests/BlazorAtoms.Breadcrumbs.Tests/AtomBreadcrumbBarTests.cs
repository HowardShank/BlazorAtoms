namespace BlazorAtoms.Breadcrumbs.Tests;

/// <summary>
/// bUnit-level tests for <see cref="AtomBreadcrumbBar"/>. Locks in the rendered DOM shape: nav/ol
/// semantics, link vs. non-clickable text per entry, <c>aria-current</c> on the last entry only,
/// separator count, and the loading placeholder for a pending title.
/// </summary>
public class AtomBreadcrumbBarTests
{
    [Fact]
    public void Renders_nav_landmark_with_ordered_list()
    {
        using var ctx = new BunitContext();
        var service = new AtomBreadcrumbService();
        service.Push(new AtomBreadcrumbEntry { Title = "Home", Key = "/", Href = "/" });

        var cut = ctx.Render<AtomBreadcrumbBar>(p => p.AddCascadingValue(service));

        var nav = cut.Find("nav");
        Assert.Equal("breadcrumb", nav.GetAttribute("aria-label"));
        Assert.NotNull(cut.Find("ol"));
    }

    [Fact]
    public void Ancestor_entries_with_href_render_as_links_current_entry_does_not()
    {
        using var ctx = new BunitContext();
        var service = new AtomBreadcrumbService();
        service.Push(new AtomBreadcrumbEntry { Title = "Customers", Key = "/customers", Href = "/customers" });
        service.Push(new AtomBreadcrumbEntry { Title = "Acme Corp", Key = "/customers/1", Href = "/customers/1" });

        var cut = ctx.Render<AtomBreadcrumbBar>(p => p.AddCascadingValue(service));

        var links = cut.FindAll("a");
        Assert.Single(links);
        Assert.Equal("/customers", links[0].GetAttribute("href"));

        var current = cut.Find("span[aria-current='page']");
        Assert.Equal("Acme Corp", current.TextContent);
    }

    [Fact]
    public void Entry_without_href_renders_as_non_clickable_text()
    {
        using var ctx = new BunitContext();
        var service = new AtomBreadcrumbService();
        service.Push(new AtomBreadcrumbEntry { Title = "Unresolved parent", Key = "/parent", Href = null });
        service.Push(new AtomBreadcrumbEntry { Title = "Current", Key = "/current", Href = "/current" });

        var cut = ctx.Render<AtomBreadcrumbBar>(p => p.AddCascadingValue(service));

        Assert.Empty(cut.FindAll("a"));
    }

    [Fact]
    public void Separator_renders_between_entries_but_not_after_the_last()
    {
        using var ctx = new BunitContext();
        var service = new AtomBreadcrumbService();
        service.Push(new AtomBreadcrumbEntry { Title = "A", Key = "/a", Href = "/a" });
        service.Push(new AtomBreadcrumbEntry { Title = "B", Key = "/b", Href = "/b" });
        service.Push(new AtomBreadcrumbEntry { Title = "C", Key = "/c", Href = "/c" });

        var cut = ctx.Render<AtomBreadcrumbBar>(p => p.AddCascadingValue(service));

        var separators = cut.FindAll(".atom-breadcrumb-bar__separator");
        Assert.Equal(2, separators.Count);
    }

    [Fact]
    public void Pending_title_renders_loading_placeholder_instead_of_raw_template()
    {
        using var ctx = new BunitContext();
        var service = new AtomBreadcrumbService();
        service.Push(new AtomBreadcrumbEntry { Title = "Item: {name}", Key = "/item/1", IsTitlePending = true });

        var cut = ctx.Render<AtomBreadcrumbBar>(p => p
            .AddCascadingValue(service)
            .Add(x => x.LoadingPlaceholder, "loading…"));

        var current = cut.Find("span[aria-current='page']");
        Assert.Equal("loading…", current.TextContent);
    }

    [Fact]
    public void Entry_with_tooltip_renders_title_attribute_on_link_and_on_current_text()
    {
        using var ctx = new BunitContext();
        var service = new AtomBreadcrumbService();
        service.Push(new AtomBreadcrumbEntry { Title = "Customers", Key = "/customers", Href = "/customers", Tooltip = "Browse customers" });
        service.Push(new AtomBreadcrumbEntry { Title = "Acme Corp", Key = "/customers/1", Href = "/customers/1", Tooltip = "Account #1" });

        var cut = ctx.Render<AtomBreadcrumbBar>(p => p.AddCascadingValue(service));

        Assert.Equal("Browse customers", cut.Find("a").GetAttribute("title"));
        Assert.Equal("Account #1", cut.Find("span[aria-current='page']").GetAttribute("title"));
    }

    [Fact]
    public void Entry_without_tooltip_renders_no_title_attribute()
    {
        using var ctx = new BunitContext();
        var service = new AtomBreadcrumbService();
        service.Push(new AtomBreadcrumbEntry { Title = "Home", Key = "/", Href = "/" });

        var cut = ctx.Render<AtomBreadcrumbBar>(p => p.AddCascadingValue(service));

        Assert.Null(cut.Find("span[aria-current='page']").GetAttribute("title"));
    }

    [Fact]
    public void Renders_nothing_when_no_provider_is_present()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomBreadcrumbBar>();

        Assert.Empty(cut.Markup.Trim());
    }
}
