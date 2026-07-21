# BlazorAtoms.Breadcrumbs

Breadcrumb trails for Blazor, combining two mechanisms rather than picking one:

- **Static** — `[AtomBreadcrumb]` on an `@page` component declares its title and parent route.
  The chain from root to current page is resolved from that metadata alone, so it's correct even
  on a hard refresh or a direct deep link — no click-history required.
- **Dynamic** — an unattributed page automatically gets a URL-derived entry in the trail (no setup
  needed); call `AtomBreadcrumbService.Push`/`PushAsync` from it to replace that placeholder with a
  better title once real data (e.g. an entity name) is available.

No `<script>` tag, no DI registration (`AtomBreadcrumbProvider` cascades the service, matching the
rest of BlazorAtoms — no `services.Add…()`), no third-party dependency.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Breadcrumbs\BlazorAtoms.Breadcrumbs.csproj" />
```

```razor
@using BlazorAtoms.Breadcrumbs
```

## Required one-time app setup

`AtomBreadcrumbProvider` resolves the current page from the real, already-route-matched `RouteData`
your `<Router>` produces — not by re-implementing route matching — so your `Routes.razor` (or
equivalent) needs to cascade it once:

```razor
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <CascadingValue Value="routeData">
            <AtomBreadcrumbProvider>
                <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
            </AtomBreadcrumbProvider>
        </CascadingValue>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

Nest another `<AtomBreadcrumbProvider>` deeper in the tree (e.g. inside a section layout) to give
that subtree its own independent trail — providers scope by render-tree position, not config.

## Declaring the static hierarchy

```razor
@page "/customers"
@attribute [AtomBreadcrumb("Customers")]
```

```razor
@page "/customers/{id}"
@attribute [AtomBreadcrumb("Customer: {name}", ParentRoute = "/customers")]
```

`Title` supports `{token}` placeholders resolved at runtime — see below. `ParentRoute` must match
another attributed page's own route template; a typo there, or a cyclic chain, throws
`InvalidOperationException` the first time that specific page is visited (not at app startup, so
one broken page doesn't take down breadcrumbs everywhere else). An optional `Tooltip` renders as
the entry's native `title` attribute (hover text) when set:

```razor
@attribute [AtomBreadcrumb("Customers", Tooltip = "Browse all customer accounts")]
```

## Filling in a title token

```razor
@code {
    [CascadingParameter] private AtomBreadcrumbService? Breadcrumbs { get; set; }
    [Parameter] public string Id { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        var customer = await CustomerService.GetAsync(Id);
        Breadcrumbs?.SetData("name", customer.Name);
        // or, if the value itself is async:
        // await Breadcrumbs!.SetDataAsync("name", CustomerService.GetNameAsync(Id));
    }
}
```

While a token is unresolved, `AtomBreadcrumbBar` shows `LoadingPlaceholder` (default `"…"`) instead
of the raw template. If navigation moves on before an async value resolves, the stale result is
discarded automatically.

## Dynamic entries for unattributed pages

A page with no `[AtomBreadcrumb]` attribute at all still shows up in the trail — it gets an
automatic entry titled from its last URL segment (`/customers/482/my-draft` → "My Draft"). To
upgrade that placeholder once you have real data, `Push`/`PushAsync` with the **same `Key`**
(sans query string) and it replaces the auto-generated entry in place rather than adding a duplicate:

```razor
@code {
    [CascadingParameter] private AtomBreadcrumbService? Breadcrumbs { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        Breadcrumbs?.Push(new AtomBreadcrumbEntry
        {
            Title = "Draft #482",
            Key = new Uri(NavigationManager.Uri).AbsolutePath, // matches the auto entry's Key so this replaces it
        });
    }
}
```

Revisiting an entry already in the trail truncates back to it (A→B→C→A→D collapses to A→D) rather
than duplicating it — and refreshes its content from whatever was just pushed. Mixed nav works too:
an attributed page's static chain is always the head, and dynamic entries (auto or pushed) append
after it as the tail.

## Rendering the trail

```razor
<AtomBreadcrumbBar />

@* Custom separator / loading text *@
<AtomBreadcrumbBar Separator=">" LoadingPlaceholder="Loading…" CssClass="my-breadcrumbs" />
```

Renders a `<nav aria-label="breadcrumb">` / `<ol>` with `aria-current="page"` on the last entry
(never a link) and separators marked `aria-hidden`.

## Resetting the trail's dynamic tail

Some routes should always start fresh — e.g. a dashboard root the user can jump to from anywhere.

```razor
<AtomBreadcrumbProvider IsRootRoute="@(uri => uri.EndsWith("/dashboard"))">
```

Without an explicit predicate, a page's own `[AtomBreadcrumb(..., IsRoot = true)]` is used instead.

## External title lookup for unattributed pages

If your titles live in data you own (a CSV, a database table) rather than in `[AtomBreadcrumb]`
attributes, wire a lookup in via `TitleResolver` at the same `Routes.razor` mount point as
`IsRootRoute`. It's called with **two** views of the same navigation — use whichever the lookup needs:

- `routeTemplate` — the matched `@page` template (e.g. `"/products/{id}"`), or `null` if none
  matched — for keying a title on the page's *shape*, one row per page.
- `path` — the normalized URI with actual segment values intact (e.g. `"/products/482"`) — for
  pulling a specific value (an id, a slug) back out to drive an *entity* lookup. Only the consumer
  knows how that value is structured, so parsing it is left entirely to the resolver.

Generic per-page-shape title, keyed on the template, `IsRootRoute` a plain inline predicate:

```razor
@* Routes.razor *@
@inject IPageTitleStore PageTitleStore

<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <CascadingValue Value="routeData">
            <AtomBreadcrumbProvider TitleResolver="@ResolveTitle" IsRootRoute="@(uri => uri.EndsWith("/dashboard"))">
                <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
            </AtomBreadcrumbProvider>
        </CascadingValue>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>

@code {
    // one row per page shape, e.g. "/products/{id}" -> "Product" — same title regardless of which product
    private string? ResolveTitle(string? routeTemplate, string path) =>
        routeTemplate is not null && PageTitleStore.TryGetTitle(routeTemplate, out var title) ? title : null;
}
```

Entity-specific title: parse the id out of `path` and look up the actual record:

```razor
@* Routes.razor *@
@inject IPageTitleStore PageTitleStore

<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <CascadingValue Value="routeData">
            <AtomBreadcrumbProvider TitleResolver="@ResolveTitle" IsRootRoute="@ResolveIsRoot">
                <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
            </AtomBreadcrumbProvider>
        </CascadingValue>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>

@code {
    // "/products/482" -> pull "482" out, look up that specific product's name
    private string? ResolveTitle(string? routeTemplate, string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return routeTemplate == "/products/{id}" && segments.Length == 2 && int.TryParse(segments[1], out var id)
            ? PageTitleStore.GetProductName(id)
            : null;
    }

    // IsRootRoute gets the raw NavigationManager.Uri too — same store, a different column/key
    // (e.g. an "IsRoot" flag per row) driving the same data-owned decision.
    private bool ResolveIsRoot(string uri) => PageTitleStore.IsRootRoute(uri);
}
```

```csharp
public interface IPageTitleStore
{
    bool TryGetTitle(string routeTemplate, out string title);
    string? GetProductName(int id);
    bool IsRootRoute(string uri);
}
```

Returning `null` (no row for that key) falls through to the built-in humanized-segment title, so a
partially-populated store is safe — only the pages you've actually keyed get overridden.
