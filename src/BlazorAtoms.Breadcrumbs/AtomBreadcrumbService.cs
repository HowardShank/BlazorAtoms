using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace BlazorAtoms.Breadcrumbs;

/// <summary>
/// Owns one breadcrumb trail: a static head resolved from the current page's
/// <see cref="AtomBreadcrumbAttribute"/> parent chain, plus a dynamic tail. Unattributed pages get
/// a URL-derived entry appended to the tail automatically (no attribute → no static-chain
/// membership, but still visible in the trail); call <see cref="Push"/>/<see cref="PushAsync"/>
/// from such a page to replace that auto-generated entry with a better title once real data is
/// available. One instance per <c>AtomBreadcrumbProvider</c> — not DI-registered, cascaded instead
/// (repo convention: no <c>services.Add…()</c>).
/// </summary>
public sealed partial class AtomBreadcrumbService
{
    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex TitleTokenPattern();

    [GeneratedRegex(@"\{\*?(\w+)(?::[^}?]+)?\??\}")]
    private static partial Regex RouteParamPattern();

    [GeneratedRegex(@"^\{\*?(\w+)(?::[^}?]+)?\??\}$")]
    private static partial Regex FullRouteParamSegmentPattern();

    private readonly Dictionary<string, string> _tokenValues = new();
    private List<AtomBreadcrumbEntry> _staticHead = new();
    private readonly List<AtomBreadcrumbEntry> _dynamicTail = new();
    private string? _currentTitleTemplate;
    private int _generation;
    private string? _lastUri;
    private Type? _lastPageType;

    /// <summary>The current trail: static head (root...current attributed page), then the dynamic
    /// tail — an auto-generated URL-derived entry for unattributed pages, any explicit
    /// <see cref="Push"/> entries, or both (the auto entry, then replaced by a later Push).</summary>
    public IReadOnlyList<AtomBreadcrumbEntry> Trail => _staticHead.Concat(_dynamicTail).ToList();

    /// <summary>Fires whenever the trail changes, for any consumer — not just
    /// <c>AtomBreadcrumbBar</c> — to react (e.g. sync <c>&lt;title&gt;</c>, log analytics).</summary>
    public event EventHandler? Changed;

    /// <summary>Called by <c>AtomBreadcrumbProvider</c> when the cascaded <c>RouteData</c> changes.
    /// Not meant for app code to call directly.</summary>
    //internal void OnNavigated(Type? pageType, IReadOnlyDictionary<string, object?> routeValues, string currentUri, bool isRoot)
    internal void OnNavigated(RouteData? routeData, string currentUri, bool isRoot)
    {
        var pageType = routeData?.PageType;
        var routeValues = routeData?.RouteValues ?? new Dictionary<string, object?>();
        var routeTemplate = pageType is not null && AtomBreadcrumbGraph.Instance.NodesByType.TryGetValue(pageType, out var node)
            ? node.OwnRouteTemplate
            : null;

        //if (routeTemplate == null)

        if (currentUri == _lastUri && pageType == _lastPageType) return;
        _lastUri = currentUri;
        _lastPageType = pageType;

        _generation++;
        _tokenValues.Clear();

        if (isRoot) _dynamicTail.Clear();

        if (pageType is not null && AtomBreadcrumbGraph.Instance.NodesByType.ContainsKey(pageType))
        {
            _staticHead = BuildStaticHead(pageType, routeValues, currentUri);
            _dynamicTail.Clear();
        }
        else
        {
            ApplyPush(new AtomBreadcrumbEntry
            {
                Title = ResolveUnattributedTitle(pageType, currentUri),
                Key = NormalizeKey(currentUri),
                Href = StripQuery(currentUri),
            });
        }

        Notify();
    }

    /// <summary>Sets a title-token value synchronously (e.g. data already loaded) and refreshes the
    /// current entry's title.</summary>
    public void SetData(string key, string value)
    {
        _tokenValues[key] = value;
        RefreshCurrentTitle();
        Notify();
    }

    /// <summary>Sets a title-token value from an async source. Guarded by the navigation generation
    /// counter (decision 7) — if navigation moves on before <paramref name="valueTask"/> completes,
    /// the stale result is discarded.</summary>
    public async Task SetDataAsync(string key, Task<string> valueTask)
    {
        var generation = _generation;
        var value = await valueTask.ConfigureAwait(false);
        if (generation != _generation) return;
        _tokenValues[key] = value;
        RefreshCurrentTitle();
        Notify();
    }

    /// <summary>Appends a dynamic entry for an unattributed page — or, if that page already has an
    /// auto-generated (or previously pushed) entry for the same <see cref="AtomBreadcrumbEntry.Key"/>,
    /// replaces it in place. Revisiting an entry further back truncates to it (decision 2). Call
    /// from <c>OnInitializedAsync</c> once real data (e.g. an entity name) is available, to upgrade
    /// past the automatic URL-derived title.</summary>
    public void Push(AtomBreadcrumbEntry entry) => ApplyPush(entry);

    /// <summary>Async variant of <see cref="Push"/>, generation-guarded the same way as
    /// <see cref="SetDataAsync"/>.</summary>
    public async Task PushAsync(Func<Task<AtomBreadcrumbEntry>> entryFactory)
    {
        var generation = _generation;
        var entry = await entryFactory().ConfigureAwait(false);
        if (generation != _generation) return;
        ApplyPush(entry);
    }

    private void ApplyPush(AtomBreadcrumbEntry entry)
    {
        var combined = _staticHead.Concat(_dynamicTail).ToList();
        var existingIndex = combined.FindIndex(e => e.Key == entry.Key);

        if (existingIndex >= 0 && existingIndex < _staticHead.Count)
        {
            _dynamicTail.Clear();
        }
        else if (existingIndex >= 0)
        {
            var tailIndex = existingIndex - _staticHead.Count;
            _dynamicTail.RemoveRange(tailIndex + 1, _dynamicTail.Count - tailIndex - 1);
            _dynamicTail[tailIndex] = entry; // refresh content — e.g. an explicit Push replacing the auto-fallback entry for the same Key.
        }
        else
        {
            _dynamicTail.Add(entry);
        }

        Notify();
    }

    private List<AtomBreadcrumbEntry> BuildStaticHead(Type currentType, IReadOnlyDictionary<string, object?> routeValues, string currentUri)
    {
        var graph = AtomBreadcrumbGraph.Instance;
        var chain = new List<AtomBreadcrumbNode>();
        var visited = new HashSet<Type>();

        var type = currentType;
        while (type is not null)
        {
            if (!graph.NodesByType.TryGetValue(type, out var node)) break;
            if (!visited.Add(type))
                throw new InvalidOperationException(
                    $"[AtomBreadcrumb] parent chain starting at '{currentType}' contains a cycle at '{type}'.");

            chain.Add(node);

            if (node.Attribute.ParentRoute is null)
            {
                type = null;
            }
            else if (graph.TypeByTemplate.TryGetValue(node.Attribute.ParentRoute, out var parentType))
            {
                type = parentType;
            }
            else
            {
                throw new InvalidOperationException(
                    $"[AtomBreadcrumb] on '{node.PageType}' references ParentRoute \"{node.Attribute.ParentRoute}\", " +
                    "which does not match any known [AtomBreadcrumb]-attributed page's route template.");
            }
        }

        chain.Reverse();

        _currentTitleTemplate = chain.Count > 0 ? chain[^1].Attribute.Title : null;

        var entries = new List<AtomBreadcrumbEntry>(chain.Count);
        for (var i = 0; i < chain.Count; i++)
        {
            var node = chain[i];
            var isCurrent = i == chain.Count - 1;
            var (title, pending) = ResolveTitle(node.Attribute.Title);
            var href = isCurrent ? currentUri : TryBuildHref(node.OwnRouteTemplate, routeValues);
            var key = NormalizeKey(isCurrent ? currentUri : href ?? node.OwnRouteTemplate);

            entries.Add(new AtomBreadcrumbEntry { Title = title, IsTitlePending = pending, Href = href, Key = key, Tooltip = node.Attribute.Tooltip });
        }

        return entries;
    }

    private void RefreshCurrentTitle()
    {
        if (_staticHead.Count == 0 || _currentTitleTemplate is null) return;
        var (title, pending) = ResolveTitle(_currentTitleTemplate);
        var last = _staticHead[^1];
        last.Title = title;
        last.IsTitlePending = pending;
    }

    private (string title, bool pending) ResolveTitle(string template)
    {
        var pending = false;
        var title = TitleTokenPattern().Replace(template, m =>
        {
            if (_tokenValues.TryGetValue(m.Groups[1].Value, out var value)) return value;
            pending = true;
            return m.Value;
        });
        return (title, pending);
    }

    private static string? TryBuildHref(string template, IReadOnlyDictionary<string, object?> routeValues)
    {
        var resolved = true;
        var href = RouteParamPattern().Replace(template, m =>
        {
            if (routeValues.TryGetValue(m.Groups[1].Value, out var value) && value is not null) return value.ToString() ?? "";
            resolved = false;
            return "";
        });
        return resolved ? href : null;
    }

    /// <summary>Title for an unattributed page. The last URL segment is often a page parameter
    /// (e.g. an id), not a title-worthy word, so this matches <paramref name="currentUri"/> against
    /// the page's own <c>@page</c> template(s) (via reflection, independent of the breadcrumb graph)
    /// and humanizes the template's last literal segment instead — falling back to the raw last
    /// URL segment only if no template match is found.</summary>
    private static string ResolveUnattributedTitle(Type? pageType, string currentUri)
    {
        var path = NormalizeKey(currentUri);
        if (path is "" or "/") return "Home";

        var templates = pageType?
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Select(a => a.Template)
            .ToArray() ?? Array.Empty<string>();

        var matched = MatchRouteTemplate(templates, path);
        return matched is not null ? TitleFromRouteTemplate(matched) : HumanizeLastSegment(currentUri);
    }

    private static string? MatchRouteTemplate(IReadOnlyList<string> templates, string path)
    {
        var uriSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var template in templates)
        {
            var segments = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != uriSegments.Length) continue;

            var isMatch = true;
            for (var i = 0; i < segments.Length; i++)
            {
                if (FullRouteParamSegmentPattern().IsMatch(segments[i])) continue;
                if (!string.Equals(segments[i], uriSegments[i], StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch) return template;
        }

        return null;
    }

    private static string TitleFromRouteTemplate(string template)
    {
        var segments = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = segments.Length - 1; i >= 0; i--)
        {
            if (FullRouteParamSegmentPattern().IsMatch(segments[i])) continue;
            return Humanize(segments[i]);
        }

        return "Home";
    }

    private static string HumanizeLastSegment(string uri)
    {
        var segments = StripQuery(uri).Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? "Home" : Humanize(segments[^1]);
    }

    private static string Humanize(string segment)
    {
        var words = segment.Replace('-', ' ').Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0
            ? "Home"
            : string.Join(' ', words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    private static string StripQuery(string uri)
    {
        var queryIndex = uri.IndexOf('?');
        return queryIndex < 0 ? uri : uri[..queryIndex];
    }

    /// <summary>Identity form for <see cref="AtomBreadcrumbEntry.Key"/>: path only, query stripped.
    /// <paramref name="uriOrPath"/> may be an absolute URI (scheme+host, e.g. from
    /// <c>NavigationManager.Uri</c>) or already a bare path — callers (this service, and app code
    /// via <see cref="Push"/>) must land on the same form or revisit/replace matching (decision 2)
    /// silently fails and duplicate entries pile up instead of one being replaced.</summary>
    private static string NormalizeKey(string uriOrPath)
    {
        var path = Uri.TryCreate(uriOrPath, UriKind.Absolute, out var absolute) ? absolute.AbsolutePath : uriOrPath;
        return StripQuery(path);
    }

    private void Notify() => Changed?.Invoke(this, EventArgs.Empty);
}
