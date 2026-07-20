# BlazorAtoms.Breadcrumbs

Breadcrumb navigation components for Blazor. Ships **`AtomBreadcrumbs`** — a customizable breadcrumb trail component with semantic HTML, automatic styling, and zero dependencies.

Supports nested routes, custom separators, and accessible navigation patterns. No `<script>` tag, no DI registration, no setup.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Breadcrumbs\BlazorAtoms.Breadcrumbs.csproj" />
```

```razor
@using BlazorAtoms.Breadcrumbs
```

## AtomBreadcrumbs

```razor
@* Basic breadcrumbs with default styling *@
<AtomBreadcrumbs Items="breadcrumbItems" />

@* Custom separator *@
<AtomBreadcrumbs Items="breadcrumbItems" Separator="/" />

@* With custom styling *@
<AtomBreadcrumbs Items="breadcrumbItems" CssClass="custom-breadcrumb" />
```

## Parameters

- **Items**: Collection of breadcrumb items to display
- **Separator**: Custom separator between breadcrumb items (default: `/`)
- **CssClass**: Custom CSS class
- **Style**: Inline styles
