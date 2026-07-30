namespace BlazorAtoms.Charts.Tests;

/// <summary>
/// Builds the <see cref="RenderFragment"/>s that go into a chart's element slots.
/// </summary>
/// <remarks>
/// A slot takes a fragment, not a component instance, so a test that wants
/// <c>&lt;ValueAxis&gt;&lt;AtomChartValueAxis /&gt;&lt;/ValueAxis&gt;</c> has to build the fragment by hand.
/// This is the whole of that boilerplate, kept in one place because every chrome test needs it.
/// </remarks>
internal static class Slot
{
    /// <summary>A fragment rendering one <typeparamref name="T"/> with no parameters.</summary>
    internal static RenderFragment Of<T>() where T : IComponent => builder =>
    {
        builder.OpenComponent<T>(0);
        builder.CloseComponent();
    };

    /// <summary>A fragment rendering one <typeparamref name="T"/> with the given parameters.</summary>
    internal static RenderFragment Of<T>(params (string Name, object Value)[] parameters)
        where T : IComponent => builder =>
    {
        builder.OpenComponent<T>(0);

        var seq = 1;
        foreach (var (name, value) in parameters) builder.AddComponentParameter(seq++, name, value);

        builder.CloseComponent();
    };

    /// <summary>A fragment of plain text, for the content slots.</summary>
    internal static RenderFragment Text(string text) => builder => builder.AddContent(0, text);
}
