namespace BlazorAtoms.Tabs.Tests;

/// <summary>
/// Builds the nested <see cref="AtomTab"/>/<see cref="AtomTabPanel"/> fragments the family needs.
/// Hand-rolling <c>RenderTreeBuilder</c> calls in every test would bury the assertions, and the three
/// components can only be exercised together — a tab with no parent does almost nothing.
/// </summary>
internal static class TabsHarness
{
    internal const string ModulePath = "./_content/BlazorAtoms.Tabs/atom-tabs.js";

    internal record Tab(string Value, string Title, bool Disabled = false);

    /// <summary>
    /// Plans the <c>atom-tabs.js</c> import and its two calls, so a test running under bUnit's default
    /// strict JSInterop mode doesn't fail on the key-guard module every <c>AtomTabs</c> loads on first
    /// render. Returns the handle, so a test that cares can verify the invocations.
    /// </summary>
    internal static BunitJSModuleInterop PlanKeyGuardModule(BunitContext ctx)
    {
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("attach", _ => true).SetVoidResult();
        module.SetupVoid("detach", _ => true).SetVoidResult();
        return module;
    }

    internal static RenderFragment TabList(params Tab[] tabs) => builder =>
    {
        var seq = 0;
        foreach (var tab in tabs)
        {
            builder.OpenComponent<AtomTab>(seq++);
            builder.AddAttribute(seq++, nameof(AtomTab.Value), tab.Value);
            builder.AddAttribute(seq++, nameof(AtomTab.Title), tab.Title);
            if (tab.Disabled) builder.AddAttribute(seq++, nameof(AtomTab.Disabled), true);
            builder.CloseComponent();
        }
    };

    internal static RenderFragment Panels(params (string Value, string Html)[] panels) => builder =>
    {
        var seq = 0;
        foreach (var (value, html) in panels)
        {
            builder.OpenComponent<AtomTabPanel>(seq++);
            builder.AddAttribute(seq++, nameof(AtomTabPanel.Value), value);
            builder.AddAttribute(seq++, nameof(AtomTabPanel.ChildContent),
                (RenderFragment)(b => b.AddMarkupContent(0, html)));
            builder.CloseComponent();
        }
    };

    /// <summary>The three-tab set most tests use: a, b, c with matching panels.</summary>
    internal static RenderFragment DefaultTabList => TabList(
        new Tab("a", "Alpha"), new Tab("b", "Bravo"), new Tab("c", "Charlie"));

    internal static RenderFragment DefaultPanels => Panels(
        ("a", "<p>panel a</p>"), ("b", "<p>panel b</p>"), ("c", "<p>panel c</p>"));
}
