using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ApexCharts;
using TaskPilot.Client;
using TaskPilot.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddApexCharts();

builder.Services.AddScoped<IHttpClientService, HttpClientService>();
builder.Services.AddScoped<TaskHttpService>();
builder.Services.AddScoped<TagHttpService>();
builder.Services.AddScoped<ApiKeyHttpService>();
builder.Services.AddScoped<AuditHttpService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
