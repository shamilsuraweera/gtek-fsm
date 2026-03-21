using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GTEK.FSM.WebPortal;
using GTEK.FSM.WebPortal.Services.Theme;
using GTEK.FSM.WebPortal.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<ResilientDataFetcher>();

await builder.Build().RunAsync();
