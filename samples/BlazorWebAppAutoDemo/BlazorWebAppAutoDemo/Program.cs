using BlazorWebAppAutoDemo.Client.Pages;
using BlazorWebAppAutoDemo.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Raise SignalR's inbound message ceiling (default 32 KB → 5 MB). Playgrounds that base64-encode
// clipboard image pastes or return larger JS-interop payloads to the server would otherwise close
// the circuit with "Server returned an error on close". Only affects the Interactive Server path;
// WebAssembly components don't cross this boundary.
builder.Services.AddSignalR(o => o.MaximumReceiveMessageSize = 5 * 1024 * 1024);

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
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorWebAppAutoDemo.Client._Imports).Assembly);

app.Run();
