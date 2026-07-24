using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Typography;

/// <summary>Zero-JS, one-shot per-character entrance animation for a single word. See
/// <see cref="AtomTextCycle"/> for the analogous list-cycling component — this one is deliberately
/// single-word/specialized rather than iterating a word list.</summary>
public partial class AtomTextScramble
{
    /// <summary>The word (or short phrase) to animate in, one character at a time. Empty renders nothing.</summary>
    [Parameter, EditorRequired] public string Word { get; set; } = "";

    /// <summary>Which entrance animation each character plays.</summary>
    [Parameter] public TextScrambleEffect Effect { get; set; } = TextScrambleEffect.RevolveScale;

    /// <summary>Delay added per character index (character <c>i</c> starts at <c>i * StaggerDelay</c>).
    /// Any CSS time (e.g. <c>"0.05s"</c>, <c>"50ms"</c>).</summary>
    [Parameter] public string StaggerDelay { get; set; } = "0.05s";

    /// <summary>How long each character's own entrance animation takes. Any CSS time.</summary>
    [Parameter] public string AnimationDuration { get; set; } = "0.5s";

    private int _replayKey;
    private string? _lastWord;

    private string EffectClass => Effect switch
    {
        TextScrambleEffect.RevolveScale => "atom-text-scramble-revolve-scale",
        TextScrambleEffect.BallDrop => "atom-text-scramble-ball-drop",
        TextScrambleEffect.SideSlide => "atom-text-scramble-side-slide",
        TextScrambleEffect.RevolveDrop => "atom-text-scramble-revolve-drop",
        TextScrambleEffect.DropVanish => "atom-text-scramble-drop-vanish",
        TextScrambleEffect.Twister => "atom-text-scramble-twister",
        TextScrambleEffect.LeftRight => "atom-text-scramble-left-right",
        _ => "atom-text-scramble-revolve-scale",
    };

    // Auto-replays whenever the consumer changes Word — no external trigger is required for the
    // common case of just swapping in a new word. _replayKey also bumps on the very first
    // OnParametersSet (_lastWord starts null), so the animation plays on initial render too.
    protected override void OnParametersSet()
    {
        if (_lastWord != Word)
        {
            _replayKey++;
            _lastWord = Word;
        }
    }

    /// <summary>Force-replays the entrance animation for the current word without changing it —
    /// wire this to a button's <c>onclick</c> (via <c>@ref</c>) for a "Repeat Animation" control.</summary>
    public void Replay()
    {
        _replayKey++;
        StateHasChanged();
    }
}
