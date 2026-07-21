using BlazorAtoms.Shared;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Breadcrumbs;

/// <summary>
/// Renders the trail of the nearest ancestor <c>AtomBreadcrumbProvider</c>. Semantic <c>&lt;nav&gt;</c>/<c>&lt;ol&gt;</c>
/// markup, <c>aria-current="page"</c> on the current (last, never a link) entry, separators marked
/// <c>aria-hidden</c>.
/// </summary>
public partial class AtomBreadcrumbBar : AtomComponentBase, IDisposable
{
    [CascadingParameter] private AtomBreadcrumbService? Service { get; set; }

    /// <summary>Text rendered between entries. Marked <c>aria-hidden</c>.</summary>
    [Parameter] public string Separator { get; set; } = "/";

    /// <summary>Shown in place of an entry's title while one of its <c>{token}</c> placeholders is
    /// still awaiting an async value.</summary>
    [Parameter] public string LoadingPlaceholder { get; set; } = "…";

    private AtomBreadcrumbService? _subscribed;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Service)) return;

        if (_subscribed is not null) _subscribed.Changed -= HandleChanged;
        _subscribed = Service;
        if (_subscribed is not null) _subscribed.Changed += HandleChanged;
    }

    private void HandleChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null) _subscribed.Changed -= HandleChanged;
    }
}
