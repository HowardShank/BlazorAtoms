using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Typography;

/// <summary>Zero-JS single-word effect: a word rises up out of an animated molten-lava-gradient
/// background. The lava background always loops (it's ambient); <see cref="Loop"/> controls only
/// whether the word itself rises once and holds, or keeps bubbling up and down.</summary>
public partial class AtomTextLava
{
    /// <summary>The word/short phrase rising out of the lava. Empty renders nothing.</summary>
    [Parameter, EditorRequired] public string Word { get; set; } = "";

    /// <summary>Whether the word keeps bubbling (rise, sink, repeat — the default) or rises once
    /// from below and holds at rest.</summary>
    [Parameter] public bool Loop { get; set; } = true;

    /// <summary>How far below rest the word starts (and, when <see cref="Loop"/>, sinks back down
    /// to). Any CSS length.</summary>
    [Parameter] public string RiseDistance { get; set; } = "1.5rem";

    /// <summary>How long one rise (or, when looping, one rise-or-sink half-cycle) takes. Any CSS time.</summary>
    [Parameter] public string Duration { get; set; } = "1.2s";

    /// <summary>Color of the heat glow (text-shadow) around the word.</summary>
    [Parameter] public string GlowColor { get; set; } = "#ff5500";

    /// <summary>Color of the hotter of the two radial-gradient lava blobs.</summary>
    [Parameter] public string BgColorHot { get; set; } = "#ff6a00";

    /// <summary>Color of the cooler of the two radial-gradient lava blobs.</summary>
    [Parameter] public string BgColorCool { get; set; } = "#ff2d00";

    /// <summary>Darker end (top) of the base linear-gradient behind the lava blobs.</summary>
    [Parameter] public string BgColorBaseDark { get; set; } = "#3a0a00";

    /// <summary>Lighter end (bottom) of the base linear-gradient behind the lava blobs.</summary>
    [Parameter] public string BgColorBaseLight { get; set; } = "#1a0500";

    private int _replayKey;

    private string RootStyle =>
        $"--atom-text-lava-glow:{GlowColor};" +
        $"--atom-text-lava-bg-hot:{BgColorHot};" +
        $"--atom-text-lava-bg-cool:{BgColorCool};" +
        $"--atom-text-lava-bg-base-dark:{BgColorBaseDark};" +
        $"--atom-text-lava-bg-base-light:{BgColorBaseLight};";

    // Both trigger modes reuse the same @keyframes (see AtomTextLava.razor.css) — Loop just flips
    // iteration-count/direction/fill-mode rather than needing a second keyframe block.
    private string WordStyle =>
        $"--atom-text-lava-rise-distance:{RiseDistance};" +
        $"animation-duration:{Duration};" +
        (Loop
            ? "animation-iteration-count:infinite;animation-direction:alternate;"
            : "animation-iteration-count:1;animation-direction:normal;animation-fill-mode:forwards;");

    /// <summary>Force-reruns the effect from its initial state — same <c>@@key</c>-remount trick
    /// <see cref="AtomTextScramble"/> uses: bumps an internal counter used as the root's
    /// <c>@@key</c>, forcing Blazor to tear down and rebuild the subtree, which restarts the CSS
    /// animation from 0% (rising from below again) instead of wherever it currently sits.</summary>
    public void Replay()
    {
        _replayKey++;
        StateHasChanged();
    }
}
