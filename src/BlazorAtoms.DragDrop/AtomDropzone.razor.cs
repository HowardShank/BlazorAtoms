using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.DragDrop;

/// <summary>
/// Generic drag-and-drop zone. Wraps a bound <see cref="Items"/> list and renders every item via
/// <see cref="ChildContent"/>, with native HTML5 drag handles on each row plus spacer targets
/// between rows for precise insertion. When multiple zones share a common <see cref="AtomDropzoneGroup{TItem}"/>
/// ancestor (or share a <see cref="Group"/> key), items flow between them; standalone zones
/// only reorder in place. No JavaScript.
/// </summary>
/// <typeparam name="TItem">Item type. Reference-only so equality is well-defined and item lookups
/// don't collide across duplicates.</typeparam>
public partial class AtomDropzone<TItem> : AtomComponentBase, IAsyncDisposable where TItem : class
{
    private DragDropContext<TItem> _ownedContext = new();
    private DragDropContext<TItem>? _subscribedContext;
    private int _activeIndexCache = -1;

    private ElementReference _rootRef;
    private IJSObjectReference? _jsModule;
    private bool _autoScrollWired;
    private readonly CancellationTokenSource _cts = new();

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter] private DragDropContext<TItem>? CascadedContext { get; set; }

    private DragDropContext<TItem> Context => CascadedContext ?? _ownedContext;

    // ---- required inputs --------------------------------------------------------------------

    /// <summary>Bound list. Mutated in-place on drop, so callers who need immutability should
    /// bind a copy.</summary>
    [Parameter, EditorRequired] public IList<TItem> Items { get; set; } = default!;

    /// <summary>Per-item template. Receives the item as <c>context</c>.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> ChildContent { get; set; } = default!;

    // ---- optional slots ---------------------------------------------------------------------

    /// <summary>Optional content shown after the last item — a footer strip, an "add" button, etc.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Optional content shown when <see cref="Items"/> is empty. When null, a default
    /// "Drop here" label renders so an empty zone still accepts drops.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    // ---- drop rules -------------------------------------------------------------------------

    /// <summary>Group key. When set, only zones sharing this key can exchange items — even when
    /// they live under the same <see cref="AtomDropzoneGroup{TItem}"/>.</summary>
    [Parameter] public string? Group { get; set; }

    /// <summary>Predicate that decides whether the active item may drop onto the target item
    /// (or into this zone when the target is null — e.g. dropping on the empty area). Return
    /// false to reject.</summary>
    [Parameter] public Func<TItem, TItem?, bool>? Accepts { get; set; }

    /// <summary>Predicate that decides whether an item can be picked up at all. False = no drag
    /// handle on that row.</summary>
    [Parameter] public Func<TItem, bool>? AllowsDrag { get; set; }

    /// <summary>When set, cross-zone drops clone the item instead of moving it — the source list
    /// keeps its copy. Same-zone drops still just reorder.</summary>
    [Parameter] public Func<TItem, TItem>? CopyItem { get; set; }

    /// <summary>Maximum number of items this zone will hold. Cross-zone drops beyond the cap
    /// fire <see cref="OnItemDropRejectedByMaxItems"/>.</summary>
    [Parameter] public int? MaxItems { get; set; }

    /// <summary>When true, hovered items swap places immediately instead of waiting for the drop.
    /// WYSIWYG kanban feel; disable for a more traditional "commit on release" model.</summary>
    [Parameter] public bool InstantReplace { get; set; }

    // ---- layout / styling -------------------------------------------------------------------

    /// <summary>Zone layout direction. Drives the scoped CSS grid/flex mode.</summary>
    [Parameter] public DropzoneOrientation Orientation { get; set; } = DropzoneOrientation.Vertical;

    /// <summary>Delegate returning extra class(es) for a specific item's wrapper.</summary>
    [Parameter] public Func<TItem, string>? ItemWrapperClass { get; set; }

    /// <summary>Optional gap between items — maps to CSS custom property <c>--dropzone-gap</c>.</summary>
    [Parameter] public string? Gap { get; set; }

    /// <summary>Highlight color used on the hovered target / active spacer. Maps to
    /// <c>--dropzone-highlight-color</c>.</summary>
    [Parameter] public string? HighlightColor { get; set; }

    /// <summary>Highlight color used when the target rejects the drop. Maps to
    /// <c>--dropzone-deny-color</c>.</summary>
    [Parameter] public string? DenyColor { get; set; }

    // ---- state ------------------------------------------------------------------------------

    /// <summary>When false, the zone is hidden via CSS <c>display:none</c>. Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>When true, blocks drag pickup (rows lose their handle) and blocks incoming drops.</summary>
    [Parameter] public bool Disabled { get; set; }

    // ---- auto-scroll --------------------------------------------------------------------------

    /// <summary>When true (default), dragging the pointer near the zone's edge scrolls it (or its
    /// nearest scrollable ancestor). Uses a small lazy-loaded JS module — invisible to consumers,
    /// no setup required. Set to false to opt out.</summary>
    [Parameter] public bool AutoScroll { get; set; } = true;

    /// <summary>Distance in pixels from the container edge that triggers auto-scroll. Default 60.</summary>
    [Parameter] public int AutoScrollEdgeSize { get; set; } = 60;

    /// <summary>Auto-scroll speed in pixels per animation frame. Default 10 (~600 px/s at 60 fps).</summary>
    [Parameter] public int AutoScrollSpeed { get; set; } = 10;

    // ---- virtualization ---------------------------------------------------------------------

    /// <summary>Wrap items in <see cref="Virtualize{TItem}"/> — only render rows in the viewport
    /// plus overscan. Set for very long lists.</summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>Options forwarded to the underlying <see cref="Virtualize{TItem}"/>.</summary>
    [Parameter] public VirtualizeOptions<TItem>? VirtualizeOptions { get; set; }

    // ---- events -----------------------------------------------------------------------------

    /// <summary>Fires with the item after a successful drop (any zone).</summary>
    [Parameter] public EventCallback<TItem> OnItemDrop { get; set; }

    /// <summary>Fires with the swapped-out target item when <see cref="InstantReplace"/> is on.</summary>
    [Parameter] public EventCallback<TItem> OnReplacedItemDrop { get; set; }

    /// <summary>Fires with the active item when a drop was refused by <see cref="Accepts"/>.</summary>
    [Parameter] public EventCallback<TItem> OnItemDropRejected { get; set; }

    /// <summary>Fires with the active item when a drop was refused by <see cref="MaxItems"/>.</summary>
    [Parameter] public EventCallback<TItem> OnItemDropRejectedByMaxItems { get; set; }

    /// <summary>Fires with the active item when its drag operation ends (either through a drop or
    /// through cancellation).</summary>
    [Parameter] public EventCallback<TItem> OnDragEnd { get; set; }

    // ---- lifecycle --------------------------------------------------------------------------

    protected override void OnParametersSet()
    {
        if (Items is null) throw new InvalidOperationException("AtomDropzone requires an Items list.");
        if (Items.IsReadOnly) throw new InvalidOperationException(
            "AtomDropzone.Items must be a mutable IList<TItem>. Arrays and read-only lists cannot be reordered — pass List<T> (or another mutable IList) instead.");

        var current = Context;
        if (!ReferenceEquals(_subscribedContext, current))
        {
            if (_subscribedContext is not null)
                _subscribedContext.StateChanged -= HandleContextStateChanged;
            current.StateChanged += HandleContextStateChanged;
            _subscribedContext = current;
        }

        _activeIndexCache = Context.ActiveItem is null ? -1 : Items.IndexOf(Context.ActiveItem);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !AutoScroll || _autoScrollWired) return;
        try
        {
            _jsModule ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", _cts.Token, "./_content/BlazorAtoms.DragDrop/atom-dropzone.js");
            await _jsModule.InvokeVoidAsync("enableAutoScroll", _cts.Token,
                _rootRef, AutoScrollEdgeSize, AutoScrollSpeed);
            _autoScrollWired = true;
        }
        catch (OperationCanceledException) { /* disposed mid-load */ }
        catch (JSDisconnectedException) { /* circuit gone */ }
        catch (InvalidOperationException) { /* SSR / prerender — JS not available */ }
    }

    private void HandleContextStateChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    public async ValueTask DisposeAsync()
    {
        if (!_cts.IsCancellationRequested) _cts.Cancel();

        if (_subscribedContext is not null)
        {
            _subscribedContext.StateChanged -= HandleContextStateChanged;
            _subscribedContext = null;
        }

        if (_jsModule is not null)
        {
            try
            {
                if (_autoScrollWired)
                    await _jsModule.InvokeVoidAsync("disableAutoScroll", _rootRef);
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (OperationCanceledException) { /* cancellation racing dispose */ }
            catch (ObjectDisposedException) { /* module already gone */ }
            _jsModule = null;
        }

        _cts.Dispose();
    }

    // ---- render helpers ---------------------------------------------------------------------

    private string RootClass => "atom-dropzone";

    private string OrientationAttr => Orientation switch
    {
        DropzoneOrientation.Horizontal => "horizontal",
        DropzoneOrientation.Grid => "grid",
        _ => "vertical",
    };

    private string? InTransitAttr => Context.ActiveItem is not null ? "true" : null;

    private string? RootStyle
    {
        get
        {
            var sb = new StringBuilder();
            if (!Visible) sb.Append("display:none;");
            if (!string.IsNullOrEmpty(Gap)) sb.Append($"--dropzone-gap:{Gap};");
            if (!string.IsNullOrEmpty(HighlightColor)) sb.Append($"--dropzone-highlight-color:{HighlightColor};");
            if (!string.IsNullOrEmpty(DenyColor)) sb.Append($"--dropzone-deny-color:{DenyColor};");
            return sb.Length == 0 ? null : sb.ToString();
        }
    }

    private string DraggableAttr(TItem item)
    {
        if (Disabled) return "false";
        if (AllowsDrag is null) return "true";
        return AllowsDrag(item) ? "true" : "false";
    }

    private string DraggableClass(TItem item)
    {
        var sb = new StringBuilder("atom-dropzone-item");
        if (AllowsDrag is not null && !AllowsDrag(item)) sb.Append(" atom-dropzone-nodrag");
        if (IsActive(item)) sb.Append(" atom-dropzone-active");
        if (ItemWrapperClass is not null)
        {
            var extra = ItemWrapperClass(item);
            if (!string.IsNullOrEmpty(extra))
            {
                sb.Append(' ');
                sb.Append(extra);
            }
        }
        return sb.ToString();
    }

    private string SpacerClass(int spacerId)
    {
        var sb = new StringBuilder("atom-dropzone-spacer");
        var active = Context.ActiveItem;
        var activeIndex = active is not null ? Items.IndexOf(active) : -1;

        if (Context.ActiveSpacerId == spacerId)
        {
            if (activeIndex == -1)
            {
                sb.Append(" atom-dropzone-spacer-hot");
            }
            else if (spacerId != activeIndex && spacerId != activeIndex + 1)
            {
                sb.Append(" atom-dropzone-spacer-hot");
            }
        }

        return sb.ToString();
    }

    private bool IsActive(TItem item) => ReferenceEquals(item, Context.ActiveItem);

    private string? TargetState(TItem item)
    {
        var active = Context.ActiveItem;
        if (active is null || ReferenceEquals(item, active)) return null;
        if (!ReferenceEquals(item, Context.DragTargetItem)) return null;
        return DropzoneEngine.ShouldAccept(active, item, Accepts) ? "accept" : "deny";
    }

    // ---- drag handlers ----------------------------------------------------------------------

    private void OnDragStart(TItem item)
    {
        if (Disabled) return;
        if (AllowsDrag is not null && !AllowsDrag(item)) return;

        Context.ActiveItem = item;
        Context.SourceItems = Items;
        Context.Group = Group;
        Context.Notify();
    }

    private async Task OnDragEnd_Internal()
    {
        var active = Context.ActiveItem;
        Context.Reset();
        if (active is not null && OnDragEnd.HasDelegate)
            await OnDragEnd.InvokeAsync(active);
    }

    // Blazor won't invoke a private method exposed as `OnDragEnd` on the razor side because
    // that name collides with the public EventCallback parameter — keep the two apart by using
    // an internal method name in markup.
    private Task OnDragEndInternal() => OnDragEnd_Internal();

    private void OnDragEnter(TItem item)
    {
        var active = Context.ActiveItem;
        if (active is null || ReferenceEquals(item, active)) return;
        if (!IsGroupCompatible()) return;
        if (DropzoneEngine.IsAtCapacity(Items, active, MaxItems)) return;
        if (!DropzoneEngine.ShouldAccept(active, item, Accepts)) return;

        Context.DragTargetItem = item;
        if (InstantReplace)
        {
            DropzoneEngine.Swap(Context.SourceItems ?? Items, Items, active, item, CopyItem);
        }
        Context.Notify();
    }

    private void OnDragLeave()
    {
        Context.DragTargetItem = null;
        Context.Notify();
    }

    private void SetActiveSpacer(int id)
    {
        Context.ActiveSpacerId = id;
        Context.Notify();
    }

    private void ClearActiveSpacer()
    {
        Context.ActiveSpacerId = null;
        Context.Notify();
    }

    private async Task OnDrop()
    {
        var active = Context.ActiveItem;
        if (active is null) { Context.Reset(); return; }
        if (Disabled) { Context.Reset(); return; }
        if (!IsGroupCompatible()) { Context.Reset(); return; }

        if (DropzoneEngine.IsAtCapacity(Items, active, MaxItems))
        {
            await OnItemDropRejectedByMaxItems.InvokeAsync(active);
            Context.Reset();
            return;
        }

        var target = Context.DragTargetItem;
        if (!DropzoneEngine.ShouldAccept(active, target, Accepts))
        {
            await OnItemDropRejected.InvokeAsync(active);
            Context.Reset();
            return;
        }

        var source = Context.SourceItems ?? Items;

        if (target is null)
        {
            if (!Items.Contains(active))
            {
                DropzoneEngine.InsertAt(source, Items, active, Items.Count, CopyItem);
            }
        }
        else
        {
            if (!Items.Contains(active))
            {
                if (!InstantReplace)
                    DropzoneEngine.Swap(source, Items, active, target, CopyItem);
                // InstantReplace already ran on drag-enter.
            }
            else if (!InstantReplace)
            {
                DropzoneEngine.Swap(source, Items, active, target, CopyItem);
            }
        }

        Context.Reset();
        if (OnItemDrop.HasDelegate) await OnItemDrop.InvokeAsync(active);
        if (InstantReplace && target is not null && OnReplacedItemDrop.HasDelegate)
            await OnReplacedItemDrop.InvokeAsync(target);
    }

    private async Task OnDropOnSpacer(int spacerId)
    {
        var active = Context.ActiveItem;
        if (active is null) { Context.Reset(); return; }
        if (Disabled) { Context.Reset(); return; }
        if (!IsGroupCompatible()) { Context.Reset(); return; }

        if (DropzoneEngine.IsAtCapacity(Items, active, MaxItems))
        {
            await OnItemDropRejectedByMaxItems.InvokeAsync(active);
            Context.Reset();
            return;
        }

        if (!DropzoneEngine.ShouldAccept(active, null, Accepts))
        {
            await OnItemDropRejected.InvokeAsync(active);
            Context.Reset();
            return;
        }

        var source = Context.SourceItems ?? Items;
        DropzoneEngine.InsertAt(source, Items, active, spacerId, CopyItem);

        Context.Reset();
        if (OnItemDrop.HasDelegate) await OnItemDrop.InvokeAsync(active);
    }

    private bool IsGroupCompatible()
    {
        // If either side declares a group, both sides must match — this fixes the reference's
        // "everyone-of-the-same-TItem-interoperates" quirk.
        if (Group is null && Context.Group is null) return true;
        return string.Equals(Group, Context.Group, StringComparison.Ordinal);
    }
}
