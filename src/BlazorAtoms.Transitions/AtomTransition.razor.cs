using BlazorAtoms.Behaviors;
using BlazorAtoms.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Transitions;

/// <summary>
/// Wraps arbitrary <see cref="ChildContent"/> and plays a CSS enter/exit transition around it
/// whenever <see cref="Show"/> changes. Unlike an overlay (e.g. AtomDrawer), the wrapper element
/// stays mounted permanently — visibility is a pure CSS class toggle, so every toggle after the
/// first render animates in any browser with no JS involved. See
/// <see cref="BlazorAtoms.Behaviors.TransitionState"/> for the engine and why only the very first
/// render (when already shown) needs the CSS-native/JS-fallback hybrid at all.
/// </summary>
public partial class AtomTransition : AtomComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly TransitionState _state = new();
    private bool _prevShow;
    private bool _initializedPrevShow;

    /// <summary>Controls whether the transition is in its shown (entered) or hidden (exited) state.</summary>
    [Parameter]
    public bool Show { get; set; }

    /// <summary>Which enter/exit animation to play.</summary>
    [Parameter]
    public AtomTransitionEffect Effect { get; set; } = AtomTransitionEffect.Fade;

    /// <summary>Animation duration in milliseconds.</summary>
    [Parameter]
    public int Duration { get; set; } = 240;

    /// <summary>Fired after the transition finishes entering (best-effort — fires when the shown
    /// class is applied, not tied to the CSS transitionend event).</summary>
    [Parameter]
    public EventCallback OnEntered { get; set; }

    /// <summary>Fired after the transition finishes exiting (same caveat as <see cref="OnEntered"/>).</summary>
    [Parameter]
    public EventCallback OnExited { get; set; }

    /// <summary>Content to show/hide.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string EffectClass => Effect.ToString().ToLowerInvariant();

    private string Classes => ClassAttr(_state.Shown
        ? $"atom-transition atom-transition-{EffectClass} atom-transition-shown"
        : $"atom-transition atom-transition-{EffectClass}");

    private string? TransitionStyle => StyleAttr(new StyleVars("atom-transition")
        .Add("duration", $"{Duration}ms")
        .ToString());

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (!_initializedPrevShow)
        {
            _initializedPrevShow = true;
            _prevShow = Show;
            return; // first render: TransitionState.InitializeAsync (from OnAfterRenderAsync) owns this
        }

        if (Show == _prevShow)
        {
            return;
        }

        _prevShow = Show;
        _state.SetShown(Show);
        await (Show ? OnEntered.InvokeAsync() : OnExited.InvokeAsync());
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        if (await _state.InitializeAsync(JS, Show))
        {
            StateHasChanged();
            await (Show ? OnEntered.InvokeAsync() : OnExited.InvokeAsync());
        }
    }
}
