namespace BlazorAtoms.Breadcrumbs;

/// <summary>One resolved node in a breadcrumb trail — a static-chain ancestor/current page, or a
/// dynamic entry appended via <see cref="AtomBreadcrumbService.Push"/>.</summary>
public sealed class AtomBreadcrumbEntry
{
    /// <summary>Resolved display text. Ignored by renderers while <see cref="IsTitlePending"/> is true.</summary>
    public required string Title { get; set; }

    /// <summary>Identity for truncate-on-revisit matching (decision 2) — the entry's route path with
    /// the query string stripped. Two entries with the same path but different query strings are
    /// the same node; same display title with a different path is not.</summary>
    public required string Key { get; init; }

    /// <summary>Resolved, navigable URL, or null to render this entry as non-clickable text. The
    /// last (current) entry in a trail is never rendered as a link regardless of this value.</summary>
    public string? Href { get; init; }

    /// <summary>True while one or more <c>{token}</c> placeholders in <see cref="Title"/> are still
    /// awaiting an async value — renderers should show a loading placeholder instead.</summary>
    public bool IsTitlePending { get; set; }

    /// <summary>Optional hover tooltip text, from <see cref="AtomBreadcrumbAttribute.Tooltip"/> for a
    /// static-chain entry, or set directly for a <see cref="AtomBreadcrumbService.Push"/> entry.
    /// Null renders no tooltip.</summary>
    public string? Tooltip { get; init; }
}
