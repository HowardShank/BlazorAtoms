using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace BlazorAtoms.DragDrop;

/// <summary>
/// Options forwarded to the underlying <see cref="Virtualize{TItem}"/> when
/// <see cref="AtomDropzone{TItem}.Virtualize"/> is true.
/// </summary>
public sealed class VirtualizeOptions<TItem>
{
    /// <summary>Additional items rendered above / below the visible viewport. Higher = fewer
    /// re-renders while scrolling, more DOM. Default 3, matches the reference implementation.</summary>
    public int OverscanCount { get; set; } = 3;

    /// <summary>Row height in pixels used by <see cref="Virtualize{TItem}"/> to size the scroll
    /// spacer. Default 50.</summary>
    public float ItemSize { get; set; } = 50f;

    /// <summary>Optional async provider — when set, drives infinite / on-demand loading instead
    /// of a materialized <see cref="AtomDropzone{TItem}.Items"/> list.</summary>
    public ItemsProviderDelegate<TItem>? ItemsProvider { get; set; }
}
