namespace BlazorAtoms.Tabs;

/// <summary>Look of the tab strip → <c>data-variant</c> on the <c>AtomTabs</c> root.</summary>
/// <remarks>
/// Prefixed <c>Tabs*</c> per the repo convention that a cross-package enum name carries its package's
/// noun — <c>BadgeVariant</c>, <c>ButtonVariant</c>, <c>InputVariant</c>, <c>ProgressVariant</c>,
/// <c>CardVariant</c> and this one would otherwise all be a bare <c>Variant</c>, leaving no
/// unambiguous name for a page that <c>@using</c>s more than one. The parameter is still called
/// <c>Variant</c>.
/// </remarks>
public enum TabsVariant
{
    /// <summary>Text tabs with a moving accent rule along the active edge. Default.</summary>
    Line,

    /// <summary>Folder tabs: the active tab is a bordered box joined to the panel below it.</summary>
    Enclosed,

    /// <summary>Rounded pills, the active one filled with the accent.</summary>
    Pill,

    /// <summary>Segmented control — one bordered track with the active segment filled.</summary>
    Bar,
}
