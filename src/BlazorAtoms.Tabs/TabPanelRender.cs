namespace BlazorAtoms.Tabs;

/// <summary>When a panel's content is in the DOM. The distinction matters as soon as a panel holds
/// state the browser owns rather than your model — a half-filled form, a scroll position, a playing
/// video, a focused field.</summary>
public enum TabPanelRender
{
    /// <summary>Only the selected panel is rendered; switching away destroys the others' content and
    /// any browser-owned state in it. Lightest, and the default.</summary>
    Active,

    /// <summary>Every panel is rendered up front, inactive ones carrying the HTML <c>hidden</c>
    /// attribute. Costs the full render on first paint, but scroll positions, uncommitted input and
    /// media playback all survive a switch.</summary>
    Always,

    /// <summary>A panel renders the first time it is selected and stays in the DOM afterwards
    /// (<c>hidden</c> when inactive). The compromise: nothing is paid for a panel the user never
    /// opens, and nothing is lost once they have.</summary>
    Lazy,
}
