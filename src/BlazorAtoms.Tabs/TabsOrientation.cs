namespace BlazorAtoms.Tabs;

/// <summary>Axis the tab strip runs along. Also sets <c>aria-orientation</c> on the tablist, which is
/// what tells assistive tech (and this component's own key handler) whether Up/Down or Left/Right are
/// the navigation keys.</summary>
public enum TabsOrientation
{
    /// <summary>Strip runs left-to-right above the panels; Left/Right arrows navigate. Default.</summary>
    Horizontal,

    /// <summary>Strip runs top-to-bottom beside the panels; Up/Down arrows navigate.</summary>
    Vertical,
}
