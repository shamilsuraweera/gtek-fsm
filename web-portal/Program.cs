using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GTEK.FSM.WebPortal;
using GTEK.FSM.WebPortal.Services.Theme;
using GTEK.FSM.WebPortal.Services;
using GTEK.FSM.WebPortal.Services.Security;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<ResilientDataFetcher>();
builder.Services.AddScoped<UiSecurityContext>();

await builder.Build().RunAsync();
