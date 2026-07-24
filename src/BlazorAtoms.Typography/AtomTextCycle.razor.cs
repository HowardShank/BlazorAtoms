using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Typography;

/// <summary>Zero-JS flip-cascade word rotator (cycles through <see cref="Words"/> in an infinite
/// loop, sliding or spinning per <see cref="Effect"/>). The animation is a pure CSS keyframe loop
/// whose percentage breakpoints depend on the word count, so they are generated per-instance
/// rather than living in static CSS.</summary>
public partial class AtomTextCycle
{
    /// <summary>The words/phrases to cycle through, in order. At least 2 are needed to animate —
    /// a single word (or an empty list) renders statically/nothing.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<string> Words { get; set; } = [];

    /// <summary>Total duration, in milliseconds, of one full loop through every word.</summary>
    [Parameter] public int Duration { get; set; } = 5000;

    /// <summary>Fraction (0–1) of each word's time slot spent transitioning to the next word; the
    /// remainder is spent holding still, upright and readable. Lower values (the default) favor a
    /// long hold with a quick transition.</summary>
    [Parameter] public double SlideRatio { get; set; } = 0.12;

    /// <summary>CSS easing applied to the transition phase. <c>"ease-out"</c> gives the classic
    /// fast-then-slowing-to-a-stop feel for Spin.</summary>
    [Parameter] public string Easing { get; set; } = "ease-in-out";

    /// <summary>How words transition — slide axis/direction, or spin (rotate in place) direction.</summary>
    [Parameter] public TextCycleEffect Effect { get; set; } = TextCycleEffect.SlideBottomToTop;

    /// <summary>Full rotations Spin makes during each word's transition — higher values spin
    /// faster/longer before landing upright. Ignored for Slide effects.</summary>
    [Parameter] public int SpinTurns { get; set; } = 2;

    /// <summary>Row height for the vertical slide effects and for Spin — must fit the tallest word
    /// at the component's font size. Any CSS length (e.g. <c>"3rem"</c>, <c>"48px"</c>). Ignored
    /// for horizontal slide effects.</summary>
    [Parameter] public string ItemHeight { get; set; } = "3.5rem";

    /// <summary>Column width for the horizontal slide effects — must fit the widest word. Any CSS
    /// length. Ignored for vertical slide effects and for Spin.</summary>
    [Parameter] public string ItemWidth { get; set; } = "8rem";

    private string _keyframesCss = "";

    private bool IsHorizontalSlide => Effect is TextCycleEffect.SlideLeftToRight or TextCycleEffect.SlideRightToLeft;
    private bool IsSpin => Effect is TextCycleEffect.SpinClockwise or TextCycleEffect.SpinCounterClockwise;

    // The "base" direction per motion kind (SlideBottomToTop / SlideRightToLeft / SpinClockwise)
    // plays the generated keyframes forward. Its pair (SlideTopToBottom / SlideLeftToRight /
    // SpinCounterClockwise) plays the *same* keyframes in reverse via animation-direction —
    // reversing time reverses both the spatial/rotational direction and the word-visit order, which
    // is the cheapest correct way to get the opposite direction without a second keyframe generator.
    private bool IsReversed => Effect is TextCycleEffect.SlideTopToBottom or TextCycleEffect.SlideLeftToRight
        or TextCycleEffect.SpinCounterClockwise;

    // Spin reuses the exact vertical-slide layout (a normal horizontal line of text, one word
    // visible at a time, revealed via translateY) — it only adds a rotate() riding along on the
    // same transform, so it gets the same axis CSS as vertical slide.
    private string AxisClass => IsHorizontalSlide ? "atom-text-cycle-axis-h" : "atom-text-cycle-axis-v";

    private string ViewportStyle => IsHorizontalSlide ? $"width:{ItemWidth};" : $"height:{ItemHeight};";

    private string TrackStyle
    {
        get
        {
            var sizeVar = IsHorizontalSlide ? $"--atom-text-cycle-item-width:{ItemWidth};" : $"--atom-text-cycle-item-height:{ItemHeight};";
            if (Words.Count <= 1) return sizeVar;

            var kind = IsSpin ? "spin" : IsHorizontalSlide ? "h" : "v";
            var animName = $"atom-text-cycle-{kind}-n{Words.Count}";
            var direction = IsReversed ? "reverse" : "normal";
            return $"{sizeVar}animation:{animName} {Duration}ms {Easing} infinite;animation-direction:{direction};";
        }
    }

    protected override void OnParametersSet()
    {
        _keyframesCss = Words.Count > 1 ? BuildKeyframesCss(Words.Count, SlideRatio, Effect, SpinTurns) : "";
    }

    /// <summary>
    /// Builds an <c>@keyframes</c> block driving the animated track for <paramref name="effect"/>,
    /// sized to <paramref name="n"/> words. Every effect steps a <c>translateX</c>/<c>translateY</c>
    /// track through <paramref name="n"/> words plus one duplicate of the first word appended as an
    /// (n+1)th row/column — the duplicate lets the loop's 100%→0% wrap be an instant, invisible
    /// snap (both show the same word), instead of a visible jump. Each of the n word-to-word
    /// transitions gets an equal 100/n share of the timeline: a hold at the current position
    /// (upright, readable), then a move (sized by <paramref name="slideRatio"/>) to the next. Spin
    /// adds a <c>rotate()</c> term to the same transform — <paramref name="spinTurns"/> full
    /// rotations per transition, landing back on a multiple of 360° (upright) exactly as each hold
    /// begins, riding along on the *same* translateY the vertical slide already uses; it does not
    /// need its own layout, radius, or duplicate-row scheme. The keyframe name is
    /// motion-kind-qualified so same-word-count instances using different effects don't silently
    /// overwrite each other's rule.
    /// </summary>
    private static string BuildKeyframesCss(int n, double slideRatio, TextCycleEffect effect, int spinTurns)
    {
        var isSpin = effect is TextCycleEffect.SpinClockwise or TextCycleEffect.SpinCounterClockwise;
        var isHorizontal = effect is TextCycleEffect.SlideLeftToRight or TextCycleEffect.SlideRightToLeft;

        var ratio = Math.Clamp(slideRatio, 0.01, 0.9);
        var segment = 100.0 / n;
        var kind = isSpin ? "spin" : isHorizontal ? "h" : "v";
        var name = $"atom-text-cycle-{kind}-n{n}";
        var turns = Math.Max(1, spinTurns);

        string ValueAt(int step)
        {
            var translate = isHorizontal
                ? (step == 0 ? "translateX(0)" : $"translateX(calc(var(--atom-text-cycle-item-width) * -{step}))")
                : (step == 0 ? "translateY(0)" : $"translateY(calc(var(--atom-text-cycle-item-height) * -{step}))");
            if (!isSpin) return translate;

            // Always a multiple of 360*turns, so it's visually upright at every hold — but each
            // consecutive step differs by exactly 360*turns, so CSS still animates a genuine spin
            // (of that many full rotations) between one hold and the next.
            var angle = -step * 360 * turns;
            return $"{translate} rotate({angle}deg)";
        }

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"@keyframes {name} {{");
        sb.Append(CultureInfo.InvariantCulture, $"0%{{transform:{ValueAt(0)};}}");
        for (var i = 0; i < n; i++)
        {
            var segEnd = (i + 1) * segment;
            var holdEnd = segEnd - segment * ratio;
            sb.Append(CultureInfo.InvariantCulture,
                $"{holdEnd.ToString("0.###", CultureInfo.InvariantCulture)}%{{transform:{ValueAt(i)};}}");
            sb.Append(CultureInfo.InvariantCulture,
                $"{segEnd.ToString("0.###", CultureInfo.InvariantCulture)}%{{transform:{ValueAt(i + 1)};}}");
        }
        sb.Append('}');
        return sb.ToString();
    }
}
