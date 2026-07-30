namespace BlazorAtoms.Tabs;

/// <summary>What an arrow key does in the tab strip. Both modes are sanctioned by the ARIA authoring
/// practices; the choice is about how expensive it is to show a panel.</summary>
public enum TabsActivation
{
    /// <summary>Arrow keys move focus <i>and</i> select, so the panel follows the focused tab.
    /// Default, and the pattern ARIA recommends when panels are cheap to render.</summary>
    Automatic,

    /// <summary>Arrow keys only move focus; Enter or Space selects. Correct when activating a tab is
    /// expensive (a fetch, a heavy panel), since arrowing past a tab no longer triggers it.</summary>
    Manual,
}
