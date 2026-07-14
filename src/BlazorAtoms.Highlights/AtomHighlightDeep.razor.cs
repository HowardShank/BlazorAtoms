using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Highlights;

/// <summary>
/// Deep, full-DOM text highlighter. Use this when the wrapped content is produced by
/// other components, contains mixed markup, or is otherwise unknown at compile time.
/// A small JS module walks the rendered DOM text nodes, similar to jquery.highlight,
/// and wraps matches in &lt;mark class="atom-highlight"&gt;. SSR renders the original
/// content unchanged; highlighting applies once the component becomes interactive.
/// </summary>
public partial class AtomHighlightDeep : AtomComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The child content whose text should be highlighted. Required.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>A single search term; merged with <see cref="Terms"/>.</summary>
    [Parameter] public string? Term { get; set; }

    /// <summary>Multiple search terms.</summary>
    [Parameter] public IReadOnlyList<string>? Terms { get; set; }

    /// <summary>Case-sensitive matching. Default: false.</summary>
    [Parameter] public bool CaseSensitive { get; set; }

    /// <summary>Match whole words only. Default: false.</summary>
    [Parameter] public bool WholeWord { get; set; }

    /// <summary>Visual treatment for highlighted matches. Default: Mark.</summary>
    [Parameter] public HighlightStyle HighlightStyle { get; set; } = HighlightStyle.Mark;

    /// <summary>Background color (or underline/outline color). Sets --highlight-bg.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color of highlighted matches. Sets --highlight-color.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Corner radius of highlighted background. Sets --highlight-radius.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Inline padding of highlighted background. Sets --highlight-padding.</summary>
    [Parameter] public string? Padding { get; set; }

    /// <summary>Accessible label for the highlighted region.</summary>
    [Parameter] public string AriaLabel { get; set; } = "Highlighted content";

    private ElementReference RootRef;
    private IJSObjectReference? _module;
    private bool _attached;
    private bool _hasTerms;
    private string? _lastTermsJson;
    private string? _lastOptionsJson;

    internal string SerializedTerms => _lastTermsJson ??= JsonSerializer.Serialize(
        GetTerms().ToArray(),
        JsonOptions);

    internal string OptionsJson => _lastOptionsJson ??= JsonSerializer.Serialize(
        new HighlightOptions(CaseSensitive, WholeWord),
        JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string StyleValue => HighlightStyle switch
    {
        HighlightStyle.Underline => "underline",
        HighlightStyle.Outline => "outline",
        _ => "mark",
    };

    private string RootStyle => new StyleVars("highlight")
        .Add("bg", Background)
        .Add("color", Color)
        .Add("radius", Radius)
        .Add("padding", Padding)
        .ToString();

    private IReadOnlyList<string> GetTerms()
    {
        var result = Terms?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(Term) && !result.Contains(Term))
        {
            result.Insert(0, Term);
        }
        return result;
    }

    protected override void OnParametersSet()
    {
        _hasTerms = GetTerms().Count > 0;
        _lastTermsJson = null;
        _lastOptionsJson = null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _module ??= await JS.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorAtoms.Highlights/atom-highlight-deep.js");

        if (!_hasTerms)
        {
            if (_attached)
            {
                await _module.InvokeVoidAsync("clear", RootRef);
                _attached = false;
            }
            return;
        }

        await _module.InvokeVoidAsync("highlight", RootRef);
        _attached = true;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                if (_attached) await _module.InvokeVoidAsync("clear", RootRef);
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        finally
        {
            _module = null;
            _attached = false;
        }

        GC.SuppressFinalize(this);
    }

    private sealed record HighlightOptions(bool CaseSensitive, bool WholeWord);
}
