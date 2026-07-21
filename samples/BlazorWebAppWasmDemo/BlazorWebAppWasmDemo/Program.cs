using BlazorWebAppWasmDemo.Client.Pages;
using BlazorWebAppWasmDemo.Components;
using BlazorWebAppWasmDemo.services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Raise SignalR's inbound message ceiling (default 32 KB → 5 MB). Kept even though this host runs
// pages in WebAssembly render mode — any future page authored under InteractiveServer mode gets
// the same protection, matching the Server + Auto demos.
builder.Services.AddSignalR(o => o.MaximumReceiveMessageSize = 5 * 1024 * 1024);

// Register route list service
builder.Services.AddSingleton<RouteInfoService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorWebAppWasmDemo.Client._Imports).Assembly);

app.Run();
