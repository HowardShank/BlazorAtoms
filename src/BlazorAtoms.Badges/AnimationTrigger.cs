namespace BlazorAtoms.Badges;

/// <summary>When the badge's <see cref="BadgeAnimation"/> plays.</summary>
public enum AnimationTrigger
{
    /// <summary>Play once when the badge appears.</summary>
    Appear,
    /// <summary>Play continuously in a loop.</summary>
    Loop,
    /// <summary>Replay once each time <c>Value</c> changes (e.g. re-bounce when a count increments).</summary>
    OnChange,
    /// <summary>Play while the pointer is over the badge/host.</summary>
    Hover,
}
