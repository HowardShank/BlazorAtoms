using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Display-only CRT-terminal companion to <see cref="AtomCrtInput"/>. Same phosphor / glow /
/// scanlines / bezel / font look; renders <see cref="Value"/> with an optional typewriter
/// animation. No JS — a cancellable C# <see cref="Task.Delay"/> loop drives the visible-character
/// count, so speed is tunable per-second regardless of render mode. Value changes cancel and
/// restart the animation from the beginning.
/// </summary>
public partial class AtomCrtDisplay : AtomComponentBase, IAsyncDisposable
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ---- content ----------------------------------------------------------------------------

    /// <summary>Text to display. Value changes restart the typewriter from the start.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Placeholder shown when <see cref="Value"/> is null/empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    // ---- structure --------------------------------------------------------------------------

    /// <summary>Form-style label above/beside the display.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Responsive classes for the label column.</summary>
    [Parameter] public string LabelCol { get; set; } = "clr-col-12 clr-col-md-2";

    /// <summary>Responsive classes for the control column.</summary>
    [Parameter] public string ControlCol { get; set; } = "clr-col-12 clr-col-md-10";

    /// <summary>Muted help text shown below the display.</summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>Accessible label; falls back to <see cref="Label"/>.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ---- state ------------------------------------------------------------------------------

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    // ---- layout -----------------------------------------------------------------------------

    /// <summary>When true (default), <c>\n</c> in <see cref="Value"/> is preserved as a line break.</summary>
    [Parameter] public bool Multiline { get; set; } = true;

    /// <summary>Explicit width in px → <c>--crt-width</c>.</summary>
    [Parameter] public double? Width { get; set; }

    /// <summary>Explicit height in px → <c>--crt-height</c>.</summary>
    [Parameter] public double? Height { get; set; }

    /// <summary>Rows (default 4). Height defaults to this many text lines when
    /// <see cref="Height"/> is unset and <see cref="Multiline"/> is true.</summary>
    [Parameter] public int Rows { get; set; } = 4;

    /// <summary>Font size in px → <c>--crt-font-size</c>.</summary>
    [Parameter] public double? FontSize { get; set; }

    // ---- CRT appearance (mirrors AtomCrtInput's) --------------------------------------------

    /// <summary>Phosphor preset. Overridden by <see cref="Color"/> when set.</summary>
    [Parameter] public CrtPhosphor Phosphor { get; set; } = CrtPhosphor.Green;

    /// <summary>Explicit text/glow color (any CSS color). Overrides the <see cref="Phosphor"/>
    /// preset. Maps to <c>--crt-color</c>.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Explicit screen background color. Maps to <c>--crt-bg</c>.</summary>
    [Parameter] public string? BackgroundColor { get; set; }

    /// <summary>Font. See <see cref="CrtFont"/>.</summary>
    [Parameter] public CrtFont Font { get; set; } = CrtFont.System;

    /// <summary>Phosphor glow via <c>text-shadow</c>. Default true.</summary>
    [Parameter] public bool Glow { get; set; } = true;

    /// <summary>Scanline overlay. Default true.</summary>
    [Parameter] public bool Scanlines { get; set; } = true;

    /// <summary>Rounded metallic monitor-bezel frame. Default true.</summary>
    [Parameter] public bool Bezel { get; set; } = true;

    /// <summary>Blinking cursor after the last displayed character. Default true.</summary>
    [Parameter] public bool CursorBlink { get; set; } = true;

    // ---- animation --------------------------------------------------------------------------

    /// <summary>When true (default), types the value one character at a time. When false, the
    /// whole value appears instantly.</summary>
    [Parameter] public bool Animate { get; set; } = true;

    /// <summary>Typing speed in characters per second. Default 20 (fast, readable). Clamped to
    /// at least 0.1 to avoid a stall.</summary>
    [Parameter] public double CharactersPerSecond { get; set; } = 20;

    /// <summary>When true, restarts the animation after finishing (after <see cref="LoopDelayMs"/>).
    /// Default false.</summary>
    [Parameter] public bool Loop { get; set; }

    /// <summary>Pause in ms between loop iterations when <see cref="Loop"/> is true. Default 1500.</summary>
    [Parameter] public int LoopDelayMs { get; set; } = 1500;

    // ---- state / rendering ------------------------------------------------------------------

    private string? _lastAnimatedValue;
    private int _displayedLength;
    private CancellationTokenSource? _cts;

    protected override void OnParametersSet()
    {
        if (!string.Equals(_lastAnimatedValue, Value, StringComparison.Ordinal))
        {
            _lastAnimatedValue = Value;
            RestartAnimation();
        }
    }

    private void RestartAnimation()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        var value = Value ?? "";
        if (!Animate || value.Length == 0)
        {
            _displayedLength = value.Length;
            return;
        }

        _displayedLength = 0;
        var cts = new CancellationTokenSource();
        _cts = cts;
        _ = RunAsync(cts.Token);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var value = Value ?? "";
        var delayMs = Math.Max(1, (int)(1000.0 / Math.Max(0.1, CharactersPerSecond)));
        try
        {
            do
            {
                _displayedLength = 0;
                await InvokeAsync(StateHasChanged);
                for (var i = 1; i <= value.Length; i++)
                {
                    await Task.Delay(delayMs, ct);
                    _displayedLength = i;
                    await InvokeAsync(StateHasChanged);
                }
                if (!Loop) return;
                await Task.Delay(Math.Max(0, LoopDelayMs), ct);
            } while (!ct.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        {
            // Expected on Value change or dispose.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        await Task.CompletedTask;
    }

    // ---- derived render state ---------------------------------------------------------------

    private string RootClass => "atom-crt-display";

    private string PhosphorAttr => Phosphor.ToString().ToLowerInvariant();

    private string FontAttr => Font switch
    {
        CrtFont.Vt323 => "vt323",
        CrtFont.PressStart2P => "press-start-2p",
        _ => "system",
    };

    private string EffectiveAriaLabel => AriaLabel ?? Label ?? "Terminal display";

    private string DisplayedText
    {
        get
        {
            var value = Value ?? "";
            if (value.Length == 0) return "";
            var len = Math.Clamp(_displayedLength, 0, value.Length);
            return value[..len];
        }
    }

    private bool ShowPlaceholder =>
        string.IsNullOrEmpty(Value) && !string.IsNullOrEmpty(Placeholder);

    private string? RootStyle
    {
        get
        {
            var sb = new StringBuilder();
            if (!Visible) sb.Append("display:none;");
            if (Width is not null) sb.Append($"--crt-width:{Width.Value.ToString(Inv)}px;");
            if (Height is not null) sb.Append($"--crt-height:{Height.Value.ToString(Inv)}px;");
            else if (Multiline) sb.Append($"--crt-height:{Rows * 1.35:0.###}em;");
            if (FontSize is not null) sb.Append($"--crt-font-size:{FontSize.Value.ToString(Inv)}px;");
            if (!string.IsNullOrEmpty(Color)) sb.Append($"--crt-color:{Color};");
            if (!string.IsNullOrEmpty(BackgroundColor)) sb.Append($"--crt-bg:{BackgroundColor};");
            return sb.Length == 0 ? null : sb.ToString();
        }
    }
}
