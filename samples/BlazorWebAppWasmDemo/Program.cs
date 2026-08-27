using BlazorWebAppWasmDemo;
using Demos.Shared.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Standalone WebAssembly boots its own root components — there is no server-rendered host page to
// place render-mode markers, which is exactly why this project has no @rendermode anywhere.
builder.RootComponents.Add<App>("#app");
// Required for the <PageTitle> / <HeadContent> used across the playground pages.
builder.RootComponents.Add<HeadOutlet>("head::after");

// Backs the /RouteInfo diagnostic page (mirrors BlazorWebAppSvrDemo).
builder.Services.AddSingleton<RouteInfoService>();

await builder.Build().RunAsync();
