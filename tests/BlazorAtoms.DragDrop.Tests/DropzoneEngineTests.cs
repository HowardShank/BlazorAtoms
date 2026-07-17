namespace BlazorAtoms.DragDrop.Tests;

/// <summary>
/// Pure list-mutation tests for <see cref="DropzoneEngine"/>. Locks the reference implementation's
/// reorder / transfer / copy / swap semantics so refactors don't drift.
/// </summary>
public class DropzoneEngineTests
{
    private sealed record Item(string Name);

    [Fact]
    public void InsertAt_moves_item_within_same_list_before_target()
    {
        var list = new List<Item> { new("a"), new("b"), new("c") };
        var active = list[2];

        DropzoneEngine.InsertAt(list, list, active, 0);

        Assert.Equal(new[] { "c", "a", "b" }, list.Select(i => i.Name));
    }

    [Fact]
    public void InsertAt_moves_item_within_same_list_after_target_corrects_index_shift()
    {
        var list = new List<Item> { new("a"), new("b"), new("c") };
        var active = list[0];

        DropzoneEngine.InsertAt(list, list, active, 3);

        Assert.Equal(new[] { "b", "c", "a" }, list.Select(i => i.Name));
    }

    [Fact]
    public void InsertAt_transfer_removes_from_source_and_inserts_into_target()
    {
        var source = new List<Item> { new("a"), new("b") };
        var target = new List<Item> { new("x") };
        var active = source[0];

        DropzoneEngine.InsertAt(source, target, active, 1);

        Assert.Equal(new[] { "b" }, source.Select(i => i.Name));
        Assert.Equal(new[] { "x", "a" }, target.Select(i => i.Name));
    }

    [Fact]
    public void InsertAt_transfer_with_CopyItem_leaves_source_untouched_and_clones_into_target()
    {
        var source = new List<Item> { new("a") };
        var target = new List<Item>();

        DropzoneEngine.InsertAt(source, target, source[0], 0, copyItem: it => new Item(it.Name + "'"));

        Assert.Single(source);
        Assert.Equal("a", source[0].Name);
        Assert.Equal("a'", target[0].Name);
    }

    [Fact]
    public void InsertAt_clamps_target_index_above_target_count()
    {
        var source = new List<Item> { new("a") };
        var target = new List<Item> { new("x") };

        DropzoneEngine.InsertAt(source, target, source[0], 999);

        Assert.Equal(new[] { "x", "a" }, target.Select(i => i.Name));
    }

    [Fact]
    public void InsertAt_clamps_target_index_below_zero_is_impossible_but_guarded()
    {
        var source = new List<Item> { new("a") };
        var target = new List<Item> { new("x") };

        DropzoneEngine.InsertAt(source, target, source[0], -5);

        Assert.Equal(new[] { "a", "x" }, target.Select(i => i.Name));
    }

    [Fact]
    public void Swap_within_same_list_trades_positions()
    {
        var list = new List<Item> { new("a"), new("b"), new("c") };

        DropzoneEngine.Swap(list, list, list[0], list[2]);

        Assert.Equal(new[] { "c", "b", "a" }, list.Select(i => i.Name));
    }

    [Fact]
    public void Swap_across_lists_inserts_after_target_and_removes_from_source()
    {
        var source = new List<Item> { new("a") };
        var target = new List<Item> { new("x"), new("y") };
        var active = source[0];

        DropzoneEngine.Swap(source, target, active, target[0]);

        Assert.Empty(source);
        Assert.Equal(new[] { "x", "a", "y" }, target.Select(i => i.Name));
    }

    [Fact]
    public void Swap_across_lists_with_CopyItem_clones_and_leaves_source_intact()
    {
        var source = new List<Item> { new("a") };
        var target = new List<Item> { new("x") };

        DropzoneEngine.Swap(source, target, source[0], target[0], it => new Item(it.Name + "-c"));

        Assert.Single(source);
        Assert.Equal(new[] { "x", "a-c" }, target.Select(i => i.Name));
    }

    [Fact]
    public void Swap_returns_early_when_active_equals_target()
    {
        var list = new List<Item> { new("a"), new("b") };

        DropzoneEngine.Swap(list, list, list[0], list[0]);

        Assert.Equal(new[] { "a", "b" }, list.Select(i => i.Name));
    }

    [Fact]
    public void IsAtCapacity_is_false_when_no_limit()
    {
        var list = new List<Item> { new("a"), new("b") };
        Assert.False(DropzoneEngine.IsAtCapacity(list, new Item("c"), null));
    }

    [Fact]
    public void IsAtCapacity_is_false_when_active_is_same_list_reorder()
    {
        var list = new List<Item> { new("a"), new("b") };
        Assert.False(DropzoneEngine.IsAtCapacity(list, list[0], 2));
    }

    [Fact]
    public void IsAtCapacity_is_true_for_new_item_when_target_is_full()
    {
        var list = new List<Item> { new("a"), new("b") };
        Assert.True(DropzoneEngine.IsAtCapacity(list, new Item("c"), 2));
    }

    [Fact]
    public void ShouldAccept_null_predicate_accepts_everything()
    {
        Assert.True(DropzoneEngine.ShouldAccept(new Item("a"), (Item?)null, null));
    }

    [Fact]
    public void InsertAt_throws_when_target_is_read_only()
    {
        var source = new List<Item> { new("a") };
        IList<Item> target = new List<Item> { new("x") }.AsReadOnly();

        Assert.Throws<InvalidOperationException>(() =>
            DropzoneEngine.InsertAt(source, target, source[0], 0));
    }

    [Fact]
    public void InsertAt_throws_when_source_is_read_only_and_no_copy_delegate()
    {
        IList<Item> source = new List<Item> { new("a") }.AsReadOnly();
        var target = new List<Item>();

        Assert.Throws<InvalidOperationException>(() =>
            DropzoneEngine.InsertAt(source, target, source[0], 0));
    }

    [Fact]
    public void InsertAt_allows_read_only_source_when_copyItem_is_supplied()
    {
        IList<Item> source = new List<Item> { new("a") }.AsReadOnly();
        var target = new List<Item>();

        DropzoneEngine.InsertAt(source, target, source[0], 0, copyItem: it => new Item(it.Name));

        Assert.Single(source);
        Assert.Equal("a", target[0].Name);
    }

    [Fact]
    public void Swap_throws_when_target_is_read_only()
    {
        var source = new List<Item> { new("a") };
        IList<Item> target = new List<Item> { new("x") }.AsReadOnly();

        Assert.Throws<InvalidOperationException>(() =>
            DropzoneEngine.Swap(source, target, source[0], target[0]));
    }

    [Fact]
    public void ShouldAccept_defers_to_predicate_and_passes_target()
    {
        Item? seen = null;
        var accepted = DropzoneEngine.ShouldAccept(new Item("a"), new Item("b"),
            (active, target) => { seen = target; return false; });
        Assert.False(accepted);
        Assert.Equal("b", seen!.Name);
    }
}
