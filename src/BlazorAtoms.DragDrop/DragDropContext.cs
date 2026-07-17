namespace BlazorAtoms.DragDrop;

/// <summary>
/// Shared mutable state for a set of <see cref="AtomDropzone{TItem}"/> instances that participate
/// in the same drag operation — typically all zones sharing a <c>Group</c> value. Replaces the
/// reference library's DI-scoped service so no <c>Services.Add…()</c> registration is needed.
/// Instances are cascaded implicitly by the first zone in a group.
/// </summary>
internal sealed class DragDropContext<TItem> where TItem : class
{
    /// <summary>Item currently being dragged; null when no drag is in progress.</summary>
    public TItem? ActiveItem { get; set; }

    /// <summary>Item the cursor is currently hovering over inside a drop zone.</summary>
    public TItem? DragTargetItem { get; set; }

    /// <summary>The <see cref="AtomDropzone{TItem}.Items"/> list the active item was picked from.
    /// Needed so cross-zone drops can remove it from the source.</summary>
    public IList<TItem>? SourceItems { get; set; }

    /// <summary>Index of the spacer slot the cursor is over (0..Items.Count). Null when not over
    /// a spacer.</summary>
    public int? ActiveSpacerId { get; set; }

    /// <summary>Group key for the current drag — zones only accept drops from matching groups.</summary>
    public string? Group { get; set; }

    /// <summary>Guard flag: some renders are suppressed during rapid drag events.</summary>
    public bool ShouldRender { get; set; } = true;

    /// <summary>Fires whenever a subscriber (dropzone) should refresh. All zones in the group
    /// listen so cross-zone transitions redraw consistently.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Clear all drag state and notify subscribers to re-render.</summary>
    public void Reset()
    {
        ShouldRender = true;
        ActiveItem = null;
        DragTargetItem = null;
        SourceItems = null;
        ActiveSpacerId = null;
        Group = null;
        Notify();
    }

    public void Notify() => StateChanged?.Invoke(this, EventArgs.Empty);
}
