@inherits AtomComponentBase

@*
    AtomHighlightDeep.razor
    -----------------------
    Blazor-native deep highlighter for rich HTML content (mixed markup: headings,
    lists, tables, links, etc.). The component highlights matches INSIDE the markup's
    text content during Blazor's own render pass and outputs the result as a
    MarkupString it fully owns. Because highlighting is part of the render tree (never
    a post-render DOM mutation), it is safe across re-renders with zero JavaScript.

    Pass the content as an HTML string via the Html parameter. The markup is rendered
    as trusted HTML, so do not pass untrusted user input.


    es — in Blazor you can intercept or transform the rendered HTML before it reaches the browser, but how you do it depends on whether you’re using Blazor Server or Blazor WebAssembly, and whether you want to do it globally or per component.

Here’s the breakdown:

1. Per-component interception (recommended for most cases)
Blazor components render to a RenderTree. You can override BuildRenderTree or wrap child content in a custom RenderFragment that post-processes the generated markup.

Example: wrapping child content and applying a text highlighter before rendering:

Csharp

Copy code
@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private RenderFragment HighlightedContent => builder =>
    {
        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        ChildContent?.Invoke(builder);
        var html = writer.ToString();

        // Simple highlight logic
        html = html.Replace("Blazor", "<mark>Blazor</mark>");

        builder.AddMarkupContent(0, html);
    };
}
Then use:

Razor

Copy code
<Highlighter>
    <p>This is Blazor content.</p>
</Highlighter>
2. Global interception in Blazor Server
If you’re on Blazor Server, you can hook into the rendering pipeline by creating a custom Renderer or middleware that wraps the HTML before it’s sent over SignalR.
This is advanced — you’d subclass Renderer and override RenderBatchAsync to modify the HTML fragments before they’re sent. This gives you full control but can be brittle if internal APIs change.

3. Post-render DOM manipulation
If you don’t need to change the HTML before it’s sent, you can highlight text after render using JavaScript interop.
For example, in OnAfterRenderAsync, call a JS function that finds and wraps matching text in <mark> tags. This works in both Server and WebAssembly without touching the render pipeline.

💡 Recommendation:
If your goal is just to highlight certain words in child components, the RenderFragment wrapper or JS interop after render is the safest and most maintainable.
Intercepting the raw HTML stream globally is possible but not officially supported and may break with framework updates.

If you want, I can show you a safe, reusable Blazor component that automatically highlights keywords in any child component’s output without touching the global renderer. Would you like me to prepare that?
=========================
Got it ✅ — I’ll prepare a safe, reusable Blazor component that can wrap any child content and automatically highlight keywords before it’s rendered to the client.

We’ll do this without hacking Blazor’s internal renderer, so it works in both Blazor Server and Blazor WebAssembly.

HighlightWrapper.razor
Razor

Copy code
@using System.Text.RegularExpressions

<div @ref="ContainerRef">
    @ChildContent
</div>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // Words to highlight
    [Parameter] public string[] Keywords { get; set; } = Array.Empty<string>();

    // Optional CSS class for highlight styling
    [Parameter] public string HighlightClass { get; set; } = "highlight";

    private ElementReference ContainerRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Keywords.Length > 0)
        {
            await JS.InvokeVoidAsync("highlightWrapper.highlightText", ContainerRef, Keywords, HighlightClass);
        }
    }
}
wwwroot/highlightWrapper.js
Javascript

Copy code
window.highlightWrapper = {
    highlightText: (container, keywords, cssClass) => {
        if (!container) return;
        const regex = new RegExp(`(${keywords.join("|")})`, "gi");

        const walk = (node) => {
            if (node.nodeType === Node.TEXT_NODE) {
                const matches = node.nodeValue.match(regex);
                if (matches) {
                    const span = document.createElement("span");
                    span.innerHTML = node.nodeValue.replace(regex, `<mark class="${cssClass}">$1</mark>`);
                    node.replaceWith(...span.childNodes);
                }
            } else if (node.nodeType === Node.ELEMENT_NODE) {
                node.childNodes.forEach(walk);
            }
        };

        walk(container);
    }
};
wwwroot/css/site.css
Css

Copy code
.highlight {
    background-color: yellow;
    padding: 0 2px;
    border-radius: 2px;
}
_Host.cshtml or index.html
Add the JS file:

Html

Copy code
<script src="highlightWrapper.js"></script>
Usage
Razor

Copy code
<HighlightWrapper Keywords="new[] { "Blazor", "C#" }" HighlightClass="highlight">
    <p>This is Blazor content with C# code.</p>
    <p>Highlighting works even inside nested components.</p>
</HighlightWrapper>
How it works
The component renders its child content normally.
After the first render, it calls a small JS function via JS interop.
The JS walks the DOM inside the wrapper and wraps matching keywords in <mark> tags (or any custom class).
This approach is safe, framework-compatible, and works in both hosting models.
If you want, I can also make a server-side only version that modifies the HTML before it’s sent to the client — but that’s more advanced and less future-proof.

Do you want me to prepare that advanced pre-send version too?
-------------------------------------
Alright — let’s go into advanced territory 🚀.
This version will intercept the rendered HTML before it’s sent to the client in Blazor Server only.

⚠ Important notes before we start:

This uses internal Blazor rendering APIs — they are not officially supported and may break in future .NET versions.
It’s server-only — won’t work in Blazor WebAssembly.
It’s best for scenarios where you must transform HTML globally before it reaches the browser.
Concept
Blazor Server sends UI updates as render batches over SignalR.
We can subclass Renderer and override RenderBatchAsync to:

Capture the HTML fragments.
Run a keyword highlighter on them.
Send the modified HTML to the client.
Custom Renderer
HighlightingRenderer.cs

Csharp

Copy code
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Text.RegularExpressions;

public class HighlightingRenderer : Renderer
{
    private readonly string[] _keywords;
    private readonly string _highlightClass;

    public HighlightingRenderer(IServiceProvider serviceProvider, 
                                 ILoggerFactory loggerFactory,
                                 string[] keywords,
                                 string highlightClass = "highlight")
        : base(serviceProvider, loggerFactory)
    {
        _keywords = keywords;
        _highlightClass = highlightClass;
    }

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        // Intercept HTML fragments
        for (int i = 0; i < renderBatch.UpdatedMarkup.Count; i++)
        {
            var markup = renderBatch.UpdatedMarkup.Array[i].Markup;
            if (!string.IsNullOrEmpty(markup))
            {
                markup = HighlightKeywords(markup);
                renderBatch.UpdatedMarkup.Array[i].Markup = markup;
            }
        }

        return base.UpdateDisplayAsync(renderBatch);
    }

    private string HighlightKeywords(string html)
    {
        if (_keywords.Length == 0) return html;

        var regex = new Regex($"({string.Join("|", _keywords)})", RegexOptions.IgnoreCase);
        return regex.Replace(html, $"<mark class=\"{_highlightClass}\">$1</mark>");
    }
}
Registering the Custom Renderer
In Program.cs (Blazor Server):

Csharp

Copy code
builder.Services.AddScoped<Renderer>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return new HighlightingRenderer(sp, loggerFactory, new[] { "Blazor", "C#" });
});
CSS
Css

Copy code
.highlight {
    background-color: yellow;
    padding: 0 2px;
    border-radius: 2px;
}
How it works
Every time Blazor Server generates a render batch, our HighlightingRenderer intercepts it.
It modifies the HTML fragments before they’re serialized and sent over SignalR.
The client receives already-highlighted HTML — no JS needed.
Pros & Cons
✅ Pros

Works automatically for all components — no need to wrap them.
No client-side JS required.
Runs before the browser sees the HTML.
⚠ Cons

Relies on internal APIs — may break with .NET updates.
Only works in Blazor Server.
Slight performance cost for regex processing on every render.
If you want, I can also prepare a hybrid approach — where we do server-side pre-send highlighting for initial render, and client-side JS highlighting for dynamic updates — so you get the best of both worlds.

Do you want me to prepare that hybrid version?

====================================================
Alright — let’s build the hybrid highlighting system so you get:

Server-side pre-send highlighting for the initial render (fast, SEO-friendly, no flicker).
Client-side JS highlighting for dynamic updates after the page is loaded (keeps things highlighted even when components re-render).
This way, you get the best of both worlds.

1. Server-side Pre-Send Highlighting
We’ll reuse the HighlightingRenderer from before, but tweak it so it only runs on the first render.

HighlightingRenderer.cs

Csharp

Copy code
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

public class HighlightingRenderer : Renderer
{
    private readonly string[] _keywords;
    private readonly string _highlightClass;
    private bool _initialRenderDone = false;

    public HighlightingRenderer(IServiceProvider serviceProvider, 
                                 ILoggerFactory loggerFactory,
                                 string[] keywords,
                                 string highlightClass = "highlight")
        : base(serviceProvider, loggerFactory)
    {
        _keywords = keywords;
        _highlightClass = highlightClass;
    }

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        if (!_initialRenderDone)
        {
            for (int i = 0; i < renderBatch.UpdatedMarkup.Count; i++)
            {
                var markup = renderBatch.UpdatedMarkup.Array[i].Markup;
                if (!string.IsNullOrEmpty(markup))
                {
                    markup = HighlightKeywords(markup);
                    renderBatch.UpdatedMarkup.Array[i].Markup = markup;
                }
            }
            _initialRenderDone = true;
        }

        return base.UpdateDisplayAsync(renderBatch);
    }

    private string HighlightKeywords(string html)
    {
        if (_keywords.Length == 0) return html;

        var regex = new Regex($"({string.Join("|", _keywords)})", RegexOptions.IgnoreCase);
        return regex.Replace(html, $"<mark class=\"{_highlightClass}\">$1</mark>");
    }
}
2. Client-side Dynamic Highlighting
We’ll use the same JS from the earlier HighlightWrapper but make it global so it can run after any re-render.

wwwroot/highlightWrapper.js

Javascript

Copy code
window.highlightWrapper = {
    highlightText: (keywords, cssClass) => {
        const regex = new RegExp(`(${keywords.join("|")})`, "gi");

        const walk = (node) => {
            if (node.nodeType === Node.TEXT_NODE) {
                const matches = node.nodeValue.match(regex);
                if (matches) {
                    const span = document.createElement("span");
                    span.innerHTML = node.nodeValue.replace(regex, `<mark class="${cssClass}">$1</mark>`);
                    node.replaceWith(...span.childNodes);
                }
            } else if (node.nodeType === Node.ELEMENT_NODE) {
                node.childNodes.forEach(walk);
            }
        };

        walk(document.body);
    }
};
3. Trigger Client-side Highlighting After Renders
We’ll hook into Blazor’s OnAfterRenderAsync in a root-level component (e.g., App.razor) so that after every render, the JS runs.

App.razor

Razor

Copy code
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        </Found>
        <NotFound>
            <p>Sorry, there's nothing at this address.</p>
        </NotFound>
    </Router>
</CascadingAuthenticationState>

@code {
    private string[] keywords = new[] { "Blazor", "C#" };

    [Inject] IJSRuntime JS { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JS.InvokeVoidAsync("highlightWrapper.highlightText", keywords, "highlight");
    }
}
4. Register the Custom Renderer
In Program.cs:

Csharp

Copy code
builder.Services.AddScoped<Renderer>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return new HighlightingRenderer(sp, loggerFactory, new[] { "Blazor", "C#" });
});
5. CSS
Css

Copy code
.highlight {
    background-color: yellow;
    padding: 0 2px;
    border-radius: 2px;
}
How It Works
First render:
`Highlight

Undo

Redo

Copy

Export

======================
Alright — let’s go all-in and make this hybrid highlighting system as complete and future-proof as possible.
This approach combines server-side pre-send HTML transformation for the initial render with client-side DOM scanning for dynamic updates after the page is loaded.
The result:

Initial page load: Keywords are already highlighted before the HTML even reaches the browser — no flicker, SEO-friendly, and works even if JavaScript is disabled.
Subsequent UI updates: Any new content rendered by Blazor after the initial load is automatically highlighted in the browser without requiring a full page refresh.


1. Server-side Pre-Send Highlighting (Initial Render)
We’ll start by creating a custom Blazor Server renderer that intercepts the render batch before it’s sent to the client.
This renderer will only modify the first render to avoid unnecessary processing on every UI update.
HighlightingRenderer.cs
Csharpusing Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

public class HighlightingRenderer : Renderer
{
    private readonly string[] _keywords;
    private readonly string _highlightClass;
    private bool _initialRenderDone = false;

    public HighlightingRenderer(IServiceProvider serviceProvider, 
                                 ILoggerFactory loggerFactory,
                                 string[] keywords,
                                 string highlightClass = "highlight")
        : base(serviceProvider, loggerFactory)
    {
        _keywords = keywords;
        _highlightClass = highlightClass;
    }

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        // Only run on the very first render
        if (!_initialRenderDone)
        {
            for (int i = 0; i < renderBatch.UpdatedMarkup.Count; i++)
            {
                var markup = renderBatch.UpdatedMarkup.Array[i].Markup;
                if (!string.IsNullOrEmpty(markup))
                {
                    markup = HighlightKeywords(markup);
                    renderBatch.UpdatedMarkup.Array[i].Markup = markup;
                }
            }
            _initialRenderDone = true;
        }

        return base.UpdateDisplayAsync(renderBatch);
    }

    private string HighlightKeywords(string html)
    {
        if (_keywords.Length == 0) return html;

        // Regex to match keywords case-insensitively
        var regex = new Regex($"({string.Join("|", _keywords)})", RegexOptions.IgnoreCase);
        return regex.Replace(html, $"<mark class=\"{_highlightClass}\">$1</mark>");
    }
}


2. Client-side Dynamic Highlighting (Post-render Updates)
After the initial render, Blazor will continue to update the DOM when components change.
We’ll use JavaScript interop to scan the DOM and wrap matching keywords in <mark> tags after each render.
wwwroot/highlightWrapper.js
Javascriptwindow.highlightWrapper = {
    highlightText: (keywords, cssClass) => {
        if (!keywords || keywords.length === 0) return;

        const regex = new RegExp(`(${keywords.join("|")})`, "gi");

        const walk = (node) => {
            if (node.nodeType === Node.TEXT_NODE) {
                const matches = node.nodeValue.match(regex);
                if (matches) {
                    const span = document.createElement("span");
                    span.innerHTML = node.nodeValue.replace(regex, `<mark class="${cssClass}">$1</mark>`);
                    node.replaceWith(...span.childNodes);
                }
            } else if (node.nodeType === Node.ELEMENT_NODE) {
                node.childNodes.forEach(walk);
            }
        };

        walk(document.body);
    }
};


3. Trigger Client-side Highlighting After Every Render
We’ll hook into OnAfterRenderAsync in a root-level component (such as App.razor) so that after every render, the JavaScript function runs and highlights any new content.
App.razor
Razor<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        </Found>
        <NotFound>
            <p>Sorry, there's nothing at this address.</p>
        </NotFound>
    </Router>
</CascadingAuthenticationState>

@code {
    private string[] keywords = new[] { "Blazor", "C#" };

    [Inject] IJSRuntime JS { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Always run after render to catch new content


*@

<link rel="stylesheet" href="_content/BlazorAtoms.Highlights/atom-highlight.css" />
<span @attributes="AdditionalAttributes"
      class="@ClassAttr("atom-highlight-root")"
      style="@StyleAttr(RootStyle)"
      aria-label="@AriaLabel">
    @_highlighted
</span>
