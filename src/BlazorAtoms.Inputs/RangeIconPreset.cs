namespace BlazorAtoms.Inputs;

/// <summary>
/// Built-in Start/End icon pairs for <see cref="AtomRangeInput{TValue}"/>, tied to the value's
/// min/max ends (not literal screen sides — see <see cref="AtomRangeInput{TValue}.IconPreset"/>).
/// </summary>
public enum RangeIconPreset
{
    /// <summary>No built-in icons.</summary>
    None,

    /// <summary>Mute (min end) / loud speaker (max end).</summary>
    Volume,

    /// <summary>Snowflake (min end) / flame (max end).</summary>
    Thermostat,

    /// <summary>Dim sun (min end) / bright sun (max end).</summary>
    Brightness,

    /// <summary>Single play triangle (min end) / fast-forward chevrons (max end).</summary>
    PlaybackSpeed,

    /// <summary>Single coin (min end) / coin stack (max end).</summary>
    Price,

    /// <summary>Dashed/hollow circle (min end) / solid filled circle (max end).</summary>
    Opacity,
}
