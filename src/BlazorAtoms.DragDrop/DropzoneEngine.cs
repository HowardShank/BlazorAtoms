namespace BlazorAtoms.DragDrop;

/// <summary>
/// Pure-static reordering / transfer engine. Split out of <see cref="AtomDropzone{TItem}"/> so the
/// list-mutation logic can be exercised without spinning up bUnit, and so callers building custom
/// drag surfaces can reuse the same rules.
/// </summary>
public static class DropzoneEngine
{
    /// <summary>Insert <paramref name="active"/> into <paramref name="target"/> at
    /// <paramref name="targetIndex"/>. Handles same-list moves (correcting the index for the
    /// removal shift) and cross-list transfers (removing from <paramref name="source"/> unless
    /// <paramref name="copyItem"/> is set). Returns the item that landed in the target list.</summary>
    public static TItem InsertAt<TItem>(
        IList<TItem> source,
        IList<TItem> target,
        TItem active,
        int targetIndex,
        Func<TItem, TItem>? copyItem = null) where TItem : class
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (active is null) throw new ArgumentNullException(nameof(active));
        if (target.IsReadOnly) throw new InvalidOperationException("target list is read-only; DropzoneEngine cannot mutate it.");
        if (!ReferenceEquals(source, target) && copyItem is null && source.IsReadOnly)
            throw new InvalidOperationException("source list is read-only; pass a copyItem delegate to clone across lists instead.");

        var sameList = ReferenceEquals(source, target);
        var oldIndex = target.IndexOf(active);

        if (sameList && oldIndex >= 0)
        {
            target.RemoveAt(oldIndex);
            if (targetIndex > oldIndex) targetIndex--;
            targetIndex = Clamp(targetIndex, 0, target.Count);
            target.Insert(targetIndex, active);
            return active;
        }

        if (!sameList)
        {
            if (copyItem is null)
                source.Remove(active);
        }

        targetIndex = Clamp(targetIndex, 0, target.Count);
        var toInsert = copyItem is null || sameList ? active : copyItem(active);
        target.Insert(targetIndex, toInsert);
        return toInsert;
    }

    /// <summary>Swap <paramref name="active"/> with <paramref name="targetItem"/>. When they live
    /// in the same list, positions trade one-for-one. When the active is from another list,
    /// it lands where the target sits and the source loses (or clones) it.</summary>
    public static void Swap<TItem>(
        IList<TItem> source,
        IList<TItem> target,
        TItem active,
        TItem targetItem,
        Func<TItem, TItem>? copyItem = null) where TItem : class
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (active is null) throw new ArgumentNullException(nameof(active));
        if (targetItem is null) throw new ArgumentNullException(nameof(targetItem));
        if (target.IsReadOnly) throw new InvalidOperationException("target list is read-only; DropzoneEngine cannot mutate it.");
        if (!ReferenceEquals(source, target) && copyItem is null && source.IsReadOnly)
            throw new InvalidOperationException("source list is read-only; pass a copyItem delegate to clone across lists instead.");

        var indexTarget = target.IndexOf(targetItem);
        if (indexTarget < 0) return;

        var sameList = ReferenceEquals(source, target);
        var indexActive = target.IndexOf(active);

        if (indexActive < 0)
        {
            var toInsert = copyItem is null ? active : copyItem(active);
            target.Insert(indexTarget + 1, toInsert);
            if (!sameList && copyItem is null)
                source.Remove(active);
            return;
        }

        if (indexActive == indexTarget) return;

        (target[indexTarget], target[indexActive]) = (target[indexActive], target[indexTarget]);
    }

    /// <summary>True when adding one more item would exceed <paramref name="maxItems"/>. Ignores
    /// same-list reorders (the item is already counted).</summary>
    public static bool IsAtCapacity<TItem>(IList<TItem> target, TItem active, int? maxItems) where TItem : class
    {
        if (maxItems is null) return false;
        if (target.Contains(active)) return false;
        return target.Count >= maxItems.Value;
    }

    /// <summary>Runs the caller's <paramref name="accepts"/> predicate; a null predicate accepts
    /// everything. <paramref name="target"/> is the hovered item (null when hovering over an
    /// empty zone or a spacer).</summary>
    public static bool ShouldAccept<TItem>(TItem active, TItem? target, Func<TItem, TItem?, bool>? accepts) where TItem : class
    {
        if (accepts is null) return true;
        return accepts(active, target);
    }

    private static int Clamp(int value, int min, int max) =>
        value < min ? min : (value > max ? max : value);
}
