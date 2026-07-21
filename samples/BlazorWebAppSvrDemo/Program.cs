using BlazorWebAppSvrDemo.Components;
using BlazorWebAppSvrDemo.services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Raise SignalR's inbound message ceiling (default 32 KB → 5 MB). Playgrounds that base64-encode
// clipboard image pastes or return larger JS-interop payloads to the server (e.g. the older
// AtomQrCode PNG path) would otherwise close the circuit with "Server returned an error on close".
builder.Services.AddSignalR(o => o.MaximumReceiveMessageSize = 5 * 1024 * 1024);

// Register route list service
builder.Services.AddSingleton<RouteInfoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
