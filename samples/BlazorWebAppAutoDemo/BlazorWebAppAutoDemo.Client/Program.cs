using Demos.Shared.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Backs the /RouteInfo page on the WebAssembly side of InteractiveAuto. The host project registers
// the same service for the server-side pass; each runtime has its own container and scans its own
// AppDomain, so the two answers are allowed to differ — seeing whether they do is the reason this
// page exists in the Auto demo at all.
builder.Services.AddSingleton<RouteInfoService>();

await builder.Build().RunAsync();
