using System.Globalization;
using BlazorAtoms.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorAtoms.RadialMenus;

// Interaction, focus, interop and the CSS-custom-property plumbing. Split from the parameter
// surface and ring construction in AtomRadialMenu.razor.cs purely for file size — the component is
// one partial class and the markup reads members from both halves.
public partial class AtomRadialMenu
{
    private const string CenterKey = "center";

    // ---- open state ---------------------------------------------------------------------------

    /// <summary>Whether the ring is on screen. <see cref="RadialMenuTrigger.Always"/> pins it open.</summary>
    private bool EffectiveOpen => Trigger == RadialMenuTrigger.Always || _rootOpen;

    /// <summary>True when Drill mode has somewhere to go back to.</summary>
    private bool CanGoBack => ExpandMode == RadialMenuExpandMode.Drill && _openPaths.Count > 0;

    private async Task SetOpenAsync(bool open)
    {
        if (Trigger == RadialMenuTrigger.Always || _rootOpen == open) return;

        _rootOpen = open;
        _lastOpenParam = open;
        if (!open)
        {
            _openPaths.Clear();
            _focusKey = null;
        }

        BuildRings();
        if (OpenChanged.HasDelegate) await OpenChanged.InvokeAsync(open);
    }

    private Task ToggleRootAsync()
    {
        // In Drill mode the center is the Back affordance first and the close button second, which
        // is the whole point of the mode — you never lose your way out.
        if (CanGoBack)
        {
            GoBack();
            return Task.CompletedTask;
        }

        return SetOpenAsync(!_rootOpen);
    }

    private void GoBack()
    {
        var deepest = _openPaths.OrderByDescending(DepthOf).FirstOrDefault();
        if (deepest is not null) _openPaths.Remove(deepest);
        _focusKey = null;
        BuildRings();
    }

    private async Task OnCenterPointerEnterAsync()
    {
        if (Trigger == RadialMenuTrigger.Hover) await SetOpenAsync(true);
    }

    private async Task OnHostPointerLeaveAsync()
    {
        if (Trigger == RadialMenuTrigger.Hover) await SetOpenAsync(false);
    }

    // ---- activation ---------------------------------------------------------------------------

    private async Task ActivateAsync(int ringIndex, int slotIndex)
    {
        if (ringIndex < 0 || ringIndex >= _rings.Count) return;
        var ring = _rings[ringIndex];
        if (slotIndex < 0 || slotIndex >= ring.Layout.Slots.Count) return;

        var slot = ring.Layout.Slots[slotIndex];

        switch (slot.Kind)
        {
            case RadialMenuSlotKind.PagePrev:
                _pageIndex = Math.Max(0, _pageIndex - 1);
                BuildRings();
                return;

            case RadialMenuSlotKind.PageNext:
                _pageIndex++;   // RadialLayout clamps, and reports the page it actually rendered
                BuildRings();
                _pageIndex = _rings.Count > 0 ? _rings[0].Layout.PageIndex : 0;
                return;
        }

        var item = ring.Items[slot.ItemIndex];
        if (item.Disabled) return;

        _focusKey = SlotKey(ringIndex, slotIndex);

        if (item.IsBranch)
        {
            await ToggleBranchAsync(ring, slot.ItemIndex, item);
            return;
        }

        if (OnItemInvoked.HasDelegate) await OnItemInvoked.InvokeAsync(item);
        if (CloseOnLeafInvoke) await SetOpenAsync(false);
    }

    private async Task ToggleBranchAsync(RadialRing ring, int itemIndex, RadialMenuItem item)
    {
        var key = ChildKey(ring.PathKey, itemIndex);

        if (_openPaths.Remove(key))
        {
            // Closing a branch closes everything under it, or those descendants would be orphaned
            // in the open set and reappear the next time this branch is opened.
            _openPaths.RemoveWhere(k => k.StartsWith(key + "/", StringComparison.Ordinal));
            BuildRings();
            if (OnBranchClosed.HasDelegate) await OnBranchClosed.InvokeAsync(item);
            return;
        }

        if (SingleBranchOpen)
        {
            // Siblings are the other open paths at this depth under the same parent.
            var depth = DepthOf(key);
            _openPaths.RemoveWhere(k => DepthOf(k) >= depth && IsSiblingOrBelow(k, ring.PathKey, depth));
        }

        _openPaths.Add(key);
        BuildRings();
        if (OnBranchOpened.HasDelegate) await OnBranchOpened.InvokeAsync(item);
    }

    private static bool IsSiblingOrBelow(string candidate, string parentKey, int depth)
    {
        if (DepthOf(candidate) < depth) return false;
        if (parentKey.Length == 0) return true;
        return candidate.StartsWith(parentKey + "/", StringComparison.Ordinal);
    }

    private async Task OnItemPointerEnterAsync(int ringIndex, int slotIndex)
    {
        if (Trigger != RadialMenuTrigger.Hover) return;
        if (ringIndex < 0 || ringIndex >= _rings.Count) return;

        var ring = _rings[ringIndex];
        var slot = ring.Layout.Slots[slotIndex];
        if (slot.Kind != RadialMenuSlotKind.Item) return;

        var item = ring.Items[slot.ItemIndex];
        if (item.Disabled || !item.IsBranch) return;
        if (_openPaths.Contains(ChildKey(ring.PathKey, slot.ItemIndex))) return;

        await ToggleBranchAsync(ring, slot.ItemIndex, item);
    }

    // ---- spin ---------------------------------------------------------------------------------

    /// <summary>
    /// Rotates a spin ring. A wheel is a Blazor event like any other, so the dial needs no JS —
    /// only <see cref="RadialMenuOverflow.Spin"/> consumes the offset, and the layout keeps the
    /// radius and pitch constant while it turns.
    /// </summary>
    private void OnWheel(WheelEventArgs e)
    {
        if (Overflow != RadialMenuOverflow.Spin || _rings.Count == 0) return;

        var step = _rings[0].Layout.StepDegrees;
        _spinOffset += Math.Sign(e.DeltaY) * (step > 0 ? step : 30);
        BuildRings();
    }

    // ---- keyboard -----------------------------------------------------------------------------

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (!KeyboardNavigation) return;

        switch (e.Key)
        {
            case "Enter":
            case " ":
                await ActivateFocusedAsync();
                break;

            case "Escape":
                if (_openPaths.Count > 0) GoBack();
                else await SetOpenAsync(false);
                _focusDirty = true;
                break;

            case "ArrowRight":
                MoveAlongRing(1);
                break;

            case "ArrowLeft":
                MoveAlongRing(-1);
                break;

            case "ArrowDown":
                await MoveInwardOutwardAsync(outward: true);
                break;

            case "ArrowUp":
                await MoveInwardOutwardAsync(outward: false);
                break;

            case "Home":
                MoveToEnd(first: true);
                break;

            case "End":
                MoveToEnd(first: false);
                break;

            default:
                return;
        }
    }

    private async Task ActivateFocusedAsync()
    {
        if (_focusKey is null or CenterKey)
        {
            await ToggleRootAsync();
            _focusDirty = true;
            return;
        }

        var (ring, slot) = ParseKey(_focusKey);
        await ActivateAsync(ring, slot);
        _focusDirty = true;
    }

    /// <summary>Steps to the next enabled slot on the focused ring, wrapping, and never looping
    /// forever on a ring where everything is disabled.</summary>
    private void MoveAlongRing(int delta)
    {
        if (_rings.Count == 0) return;

        if (_focusKey is null or CenterKey)
        {
            FocusFirstEnabled(0);
            return;
        }

        var (ringIndex, slotIndex) = ParseKey(_focusKey);
        var slots = _rings[ringIndex].Layout.Slots;
        if (slots.Count == 0) return;

        for (var hop = 1; hop <= slots.Count; hop++)
        {
            var next = ((slotIndex + delta * hop) % slots.Count + slots.Count) % slots.Count;
            if (!IsDisabled(ringIndex, next))
            {
                _focusKey = SlotKey(ringIndex, next);
                _focusDirty = true;
                return;
            }
        }
    }

    private void MoveToEnd(bool first)
    {
        if (_rings.Count == 0) return;
        var ringIndex = _focusKey is null or CenterKey ? 0 : ParseKey(_focusKey).Ring;
        var slots = _rings[ringIndex].Layout.Slots;

        var order = first
            ? Enumerable.Range(0, slots.Count)
            : Enumerable.Range(0, slots.Count).Reverse();

        foreach (var i in order)
        {
            if (IsDisabled(ringIndex, i)) continue;
            _focusKey = SlotKey(ringIndex, i);
            _focusDirty = true;
            return;
        }
    }

    /// <summary>Down goes a level deeper (opening the branch if needed); up returns to the branch
    /// this ring belongs to, or to the center button at the top level.</summary>
    private async Task MoveInwardOutwardAsync(bool outward)
    {
        if (_rings.Count == 0) return;

        if (_focusKey is null or CenterKey)
        {
            if (outward) FocusFirstEnabled(0);
            return;
        }

        var (ringIndex, slotIndex) = ParseKey(_focusKey);
        var ring = _rings[ringIndex];

        if (outward)
        {
            var slot = ring.Layout.Slots[slotIndex];
            if (slot.Kind != RadialMenuSlotKind.Item) return;

            var item = ring.Items[slot.ItemIndex];
            if (!item.IsBranch || item.Disabled) return;

            var key = ChildKey(ring.PathKey, slot.ItemIndex);
            if (!_openPaths.Contains(key)) await ToggleBranchAsync(ring, slot.ItemIndex, item);

            var child = _rings.FindIndex(r => r.PathKey == key);
            if (child >= 0) FocusFirstEnabled(child);
            return;
        }

        if (ring.PathKey.Length == 0)
        {
            _focusKey = CenterKey;
            _focusDirty = true;
            return;
        }

        var parentKey = ParentKeyOf(ring.PathKey);
        var parentRing = _rings.FindIndex(r => r.PathKey == parentKey);
        if (parentRing < 0)
        {
            _focusKey = CenterKey;
            _focusDirty = true;
            return;
        }

        var ownIndex = int.Parse(ring.PathKey[(ring.PathKey.LastIndexOf('/') + 1)..], CultureInfo.InvariantCulture);
        var slotOfParent = _rings[parentRing].Layout.Slots
            .Select((s, i) => (s, i))
            .FirstOrDefault(t => t.s.ItemIndex == ownIndex && t.s.Kind == RadialMenuSlotKind.Item);

        _focusKey = SlotKey(parentRing, slotOfParent.i);
        _focusDirty = true;
    }

    private void FocusFirstEnabled(int ringIndex)
    {
        if (ringIndex < 0 || ringIndex >= _rings.Count) return;
        var slots = _rings[ringIndex].Layout.Slots;

        for (var i = 0; i < slots.Count; i++)
        {
            if (IsDisabled(ringIndex, i)) continue;
            _focusKey = SlotKey(ringIndex, i);
            _focusDirty = true;
            return;
        }
    }

    private bool IsDisabled(int ringIndex, int slotIndex)
    {
        var ring = _rings[ringIndex];
        var slot = ring.Layout.Slots[slotIndex];
        return slot.Kind == RadialMenuSlotKind.Item && ring.Items[slot.ItemIndex].Disabled;
    }

    private static string ParentKeyOf(string key)
    {
        var cut = key.LastIndexOf('/');
        return cut < 0 ? "" : key[..cut];
    }

    private static string SlotKey(int ring, int slot) =>
        string.Create(CultureInfo.InvariantCulture, $"{ring}:{slot}");

    private static (int Ring, int Slot) ParseKey(string key)
    {
        var cut = key.IndexOf(':');
        return (
            int.Parse(key[..cut], CultureInfo.InvariantCulture),
            int.Parse(key[(cut + 1)..], CultureInfo.InvariantCulture));
    }

    /// <summary>Roving tabindex: exactly one button in the whole menu is tabbable, so Tab moves past
    /// the menu rather than through every item.</summary>
    private int TabIndexFor(string key) =>
        (_focusKey ?? CenterKey) == key ? 0 : -1;

    private async Task ApplyFocusAsync()
    {
        _focusDirty = false;
        var key = _focusKey ?? CenterKey;
        if (!_refs.TryGetValue(key, out var reference)) return;

        // Context is null when the ref was never captured by a committed render — which is exactly
        // what happens if a render threw partway through. Focusing it then would raise an
        // InvalidOperationException that replaces the real exception and hides it completely.
        if (reference.Context is null) return;

        try
        {
            await reference.FocusAsync();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { /* element left the DOM between render and focus */ }
    }

    // ---- interop ------------------------------------------------------------------------------

    private async Task AttachAsync()
    {
        // Context is null when the root was never committed by a render — which is exactly what a
        // cancelled CancellationToken produces. Marshalling an uncaptured ElementReference throws
        // InvalidOperationException out of OnAfterRenderAsync, and that exception REPLACES whatever
        // really went wrong, so the original cause vanishes without trace.
        if (_hostRef.Context is null) return;

        var module = await LoadModuleAsync();
        if (module is null || _attached) return;

        try
        {
            _selfRef ??= DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("attach", _hostRef, _selfRef, new
            {
                watchResize = RadiusMode == RadialMenuRadiusMode.FitContainer,
                outsideClick = CloseOnOutsideClick,
            });
            _attached = true;
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { /* prerender: no JS yet, retried on the next render */ }
    }

    /// <summary>
    /// Measures every label we do not have a width for yet, in one batched call, then re-renders
    /// with the real numbers. Only <see cref="RadialMenuSizeMode.Measure"/> reaches this.
    /// </summary>
    private async Task RunMeasurePassAsync()
    {
        _awaitingMeasure = false;
        if (SizeMode != RadialMenuSizeMode.Measure) return;
        if (_hostRef.Context is null) return;   // nothing rendered to measure against

        var wanted = AllLabels().Where(l => !_measured.ContainsKey(l)).Distinct().ToArray();
        if (wanted.Length == 0) return;

        var module = await LoadModuleAsync();
        if (module is null) return;

        try
        {
            var font = string.Create(CultureInfo.InvariantCulture, $"{FontSize}px");
            var widths = await module.InvokeAsync<double[]>("measure", _hostRef, font, wanted);
            for (var i = 0; i < wanted.Length && i < widths.Length; i++) _measured[wanted[i]] = widths[i];

            BuildRings();
            StateHasChanged();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { /* prerender */ }
    }

    private IEnumerable<string> AllLabels()
    {
        var stack = new Stack<IReadOnlyList<RadialMenuItem>>();
        stack.Push(Items);

        for (var guard = 0; stack.Count > 0 && guard < 4096; guard++)
        {
            foreach (var item in stack.Pop())
            {
                if (!string.IsNullOrEmpty(item.Label)) yield return item.Label;
                if (item.Children is { Count: > 0 } kids) stack.Push(kids);
            }
        }
    }

    /// <summary>Reported by the module's ResizeObserver. Half the smaller side of the host's box,
    /// less room for the item itself, is the largest radius that can fit inside it.</summary>
    [JSInvokable]
    public void OnHostResized(double width, double height)
    {
        var half = Math.Min(width, height) / 2 - ItemSize / 2 - ItemGap;
        var next = Math.Max(0, half);
        if (Math.Abs(next - _hostHalf) < 0.5) return;

        _hostHalf = next;
        BuildRings();
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnOutsideClick()
    {
        if (!CloseOnOutsideClick) return;
        await SetOpenAsync(false);
        StateHasChanged();
    }

    private async ValueTask<IJSObjectReference?> LoadModuleAsync()
    {
        try
        {
            return _module ??= await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
        }
        catch (JSDisconnectedException) { return null; }
        catch (OperationCanceledException) { return null; }
        catch (InvalidOperationException) { return null; /* prerender */ }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is { } module)
        {
            // Each teardown call is separately timed and separately guarded, so one that hangs or
            // fails still lets the others run. A skipped detach leaves the document listener and
            // the ResizeObserver attached, firing into a DotNetObjectReference that is already gone.
            await TryJsAsync(() => _attached ? module.InvokeVoidAsync("detach", _hostRef) : ValueTask.CompletedTask);
            await TryJsAsync(() => module.DisposeAsync());
            _module = null;
            _attached = false;
        }

        _selfRef?.Dispose();
        _selfRef = null;
        GC.SuppressFinalize(this);
    }

    private static async ValueTask TryJsAsync(Func<ValueTask> call)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await call().AsTask().WaitAsync(cts.Token);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    // ---- style + shape plumbing ---------------------------------------------------------------

    private static string RootClass => "atom-radial-menu";

    private string? RootStyle => new StyleVars("radialmenu")
        .Add("center-size", CenterSize)
        .Add("item-size", ItemSize)
        .Add("font-size", FontSize)
        .Add("label-offset", LabelOffset)
        .Add("border-width", BorderWidth)
        .Add("spoke-width", SpokeWidth)
        .Add("extent", Snap(Extent))
        .Add("line-height", Num(LineHeight))
        .Add("duration", Ms(AnimationDuration))
        .Add("easing", Easing)
        .Add("disabled-opacity", Num(DisabledOpacity))
        .Add("color", ItemColor)
        .Add("bg", ItemBackground)
        .Add("border", ItemBorderColor)
        .Add("center-color", CenterColor)
        .Add("center-bg", CenterBackground)
        .Add("hover-bg", HoverBackground)
        .Add("active-bg", ActiveBackground)
        .Add("label-color", LabelColor)
        .Add("spoke-color", SpokeColor)
        .ToString() is { Length: > 0 } s ? s : null;

    /// <summary>Half the box the whole menu occupies, so a consumer can reserve room for it. The
    /// center button alone sets the floor when nothing is open.</summary>
    private double Extent => _rings.Count == 0
        ? CenterSize / 2
        : Math.Max(CenterSize / 2, _rings.Max(r => Hypot(r.OriginX, r.OriginY) + r.Layout.Extent));

    /// <summary>The center button sits at the origin, so it only needs its own size.</summary>
    private string CenterStyle => new StyleVars("radialmenu")
        .Add("x", 0d)
        .Add("y", 0d)
        .Add("size", CenterSize)
        .Add("delay", Ms(0))
        .ToString();

    private string CenterAccessibleName => CanGoBack
        ? "Back"
        : Trigger == RadialMenuTrigger.Always
            ? AriaLabel ?? "Radial menu"
            : EffectiveOpen ? "Close menu" : "Open menu";

    // ---- one slot, whether it holds an item or a pagination stepper ---------------------------
    // The markup renders a single button element for both, so these helpers answer every attribute
    // for either case rather than the markup branching on kind.

    /// <summary>The item behind a slot, or null when the slot is a pagination stepper.</summary>
    private static RadialMenuItem? SlotItem(RadialRing ring, RadialSlot slot) =>
        slot.Kind == RadialMenuSlotKind.Item ? ring.Items[slot.ItemIndex] : null;

    private string SlotClass(RadialRing ring, RadialSlot slot)
    {
        var item = SlotItem(ring, slot);
        if (item is null) return "atom-radial-menu-item atom-radial-menu-stepper";

        return string.IsNullOrEmpty(item.CssClass)
            ? "atom-radial-menu-item"
            : $"atom-radial-menu-item {item.CssClass}";
    }

    private static string? SlotKindAttr(RadialSlot slot) => slot.Kind switch
    {
        RadialMenuSlotKind.PagePrev => "page-prev",
        RadialMenuSlotKind.PageNext => "page-next",
        _ => null,
    };

    private bool SlotExpanded(RadialRing ring, RadialSlot slot)
    {
        var item = SlotItem(ring, slot);
        return item?.IsBranch == true && _openPaths.Contains(ChildKey(ring.PathKey, slot.ItemIndex));
    }

    /// <summary>A leaf is not a popup, so it must not carry <c>aria-expanded</c> at all — an empty
    /// string here would still render the attribute and claim a collapsed popup that does not
    /// exist.</summary>
    private string? SlotAriaExpanded(RadialRing ring, RadialSlot slot)
    {
        var item = SlotItem(ring, slot);
        if (item?.IsBranch != true) return null;
        return SlotExpanded(ring, slot) ? "true" : "false";
    }

    private static string SlotAccessibleName(RadialSlot slot, RadialMenuItem? item) => item is not null
        ? AccessibleName(item)
        : slot.Kind == RadialMenuSlotKind.PagePrev ? "Previous page" : "Next page";

    private RadialMenuShape SlotShape(RadialMenuItem? item) => item?.Shape ?? ItemShape;

    private string SlotStyle(RadialRing ring, RadialSlot slot, int slotIndex)
    {
        var x = ring.OriginX + slot.X;
        var y = ring.OriginY + slot.Y;

        var vars = new StyleVars("radialmenu")
            .Add("x", Snap(x))
            .Add("y", Snap(y))
            .Add("size", Snap(slot.Size))
            .Add("delay", Ms(slotIndex * StaggerDelay))
            .Add("angle", Deg(Snap(slot.AngleDegrees)));

        return vars.ToString();
    }

    /// <summary>
    /// Geometry of the spoke from a ring's origin out to one slot. <c>ToShapeEdge</c> starts at the
    /// hub's inradius and stops at the item's, so no line is drawn under an opaque button.
    /// </summary>
    private string SpokeStyle(RadialRing ring, RadialSlot slot)
    {
        var radius = Hypot(slot.X, slot.Y);
        var start = 0.0;
        var length = radius;

        if (SpokeMode == RadialMenuSpokeMode.ToShapeEdge)
        {
            start = ring.HubSize / 2 * RadialShapeGeometry.InradiusRatio(RadialShapeGeometry.Sides(CenterShape, ShapeSides));
            var itemInradius = slot.Size / 2 * RadialShapeGeometry.InradiusRatio(RadialShapeGeometry.Sides(ItemShape, ShapeSides));
            length = Math.Max(0, radius - start - itemInradius);
        }

        return new StyleVars("radialmenu")
            .Add("x", Snap(ring.OriginX))
            .Add("y", Snap(ring.OriginY))
            .Add("spoke-start", Snap(start))
            .Add("spoke-len", Snap(length))
            .Add("angle", Deg(Snap(slot.AngleDegrees)))
            .ToString();
    }

    private string RingOriginStyle(RadialRing ring) => new StyleVars("radialmenu")
        .Add("x", Snap(ring.OriginX))
        .Add("y", Snap(ring.OriginY))
        .Add("radius", Snap(ring.Layout.Radius))
        .Add("outer-radius", Snap(ring.Layout.OuterRadius))
        .ToString();

    /// <summary>Polygon points for a shape, or null when it is not a polygon.</summary>
    private string? PointsFor(RadialMenuShape shape)
    {
        if (shape == RadialMenuShape.Custom) return null;
        var sides = RadialShapeGeometry.Sides(shape, ShapeSides);
        return sides is int n
            ? RadialShapeGeometry.PolygonPoints(n, RadialShapeGeometry.BaseRotation(shape) + ShapeRotation)
            : null;
    }

    private bool UsesCustomPath(RadialMenuShape shape) =>
        shape == RadialMenuShape.Custom && !string.IsNullOrEmpty(CustomPath);

    private static bool IsSquircle(RadialMenuShape shape) => shape == RadialMenuShape.Squircle;

    private string LabelFor(RadialMenuItem item)
    {
        var label = item.Label ?? "";
        return MaxLabelChars is int max && max > 0 && label.Length > max
            ? label[..max] + "…"
            : label;
    }

    private static string AccessibleName(RadialMenuItem item) =>
        item.Tooltip ?? item.Label ?? "Menu item";

    private string? DebugLabel(RadialSlot slot) => Debug
        ? string.Create(CultureInfo.InvariantCulture, $"{slot.AngleDegrees:0}° r{Hypot(slot.X, slot.Y):0}")
        : null;

    /// <summary>Every advisory the current layout produced, plus anything the ring walk itself had to
    /// report. Surfaced only under <see cref="Debug"/>.</summary>
    private IEnumerable<string> Advisories => _buildAdvisories
        .Concat(_rings.SelectMany(r => r.Layout.Advisories))
        .Distinct();

    /// <summary>Point on a circle of <paramref name="radius"/> at <paramref name="angleDeg"/>, in the
    /// same up-is-zero clockwise convention the ring uses. Used only to draw the debug overlay's
    /// arc-boundary lines.</summary>
    private static (double X, double Y) DebugEdge(double angleDeg, double radius)
    {
        var rad = angleDeg * Math.PI / 180.0;
        return (radius * Math.Sin(rad), -radius * Math.Cos(rad));
    }

    /// <summary>Half-side of the debug overlay's viewBox for a ring — enough to contain the outer
    /// ring plus an item.</summary>
    private static double DebugHalf(RadialRing ring) => Math.Max(1, ring.Layout.Extent);

    private static string DebugViewBox(RadialRing ring)
    {
        var half = DebugHalf(ring);
        return string.Create(CultureInfo.InvariantCulture, $"{-half} {-half} {half * 2} {half * 2}");
    }

    /// <summary>
    /// Rounds a computed pixel value to something CSS can actually parse.
    /// </summary>
    /// <remarks>
    /// <see cref="StyleVars"/>'s <see cref="double"/> overload formats with no format string, so a
    /// trigonometric residue such as <c>cos(90 deg) = 6.1e-17</c> would reach the stylesheet as
    /// <c>-3.9E-15px</c> — not a CSS length, and silently ignored by the browser. Rounding also
    /// collapses negative zero, which is legal CSS but reads as a defect in the DOM inspector.
    /// </remarks>
    private static double Snap(double v)
    {
        var r = Math.Round(v, 3);
        return r == 0 ? 0 : r;
    }

    private static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);

    private static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Ms(double v) => string.Create(CultureInfo.InvariantCulture, $"{v:0.###}ms");

    private static string Deg(double v) => string.Create(CultureInfo.InvariantCulture, $"{v:0.###}deg");
}
