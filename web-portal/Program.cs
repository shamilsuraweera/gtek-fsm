using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GTEK.FSM.WebPortal;
using GTEK.FSM.WebPortal.Services.Realtime;
using GTEK.FSM.WebPortal.Services.Requests;
using GTEK.FSM.WebPortal.Services.Theme;
using GTEK.FSM.WebPortal.Services;
using GTEK.FSM.WebPortal.Services.Security;
using GTEK.FSM.WebPortal.Services.Management;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<PortalRealtimeOptions>(builder.Configuration.GetSection(PortalRealtimeOptions.SectionName));
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
	apiBaseUrl = builder.HostEnvironment.BaseAddress;
}

if (!apiBaseUrl.EndsWith('/'))
{
	apiBaseUrl += "/";
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) });
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<ResilientDataFetcher>();
builder.Services.AddScoped<IQueueSavedViewService, QueueSavedViewService>();
builder.Services.AddScoped<PortalAuthState>();
builder.Services.AddScoped<UiSecurityContext>();
builder.Services.AddScoped<IRequestWorkspaceApiClient, RequestWorkspaceApiClient>();
builder.Services.AddScoped<IManagementWorkersApiClient, ManagementWorkersApiClient>();
builder.Services.AddScoped<IManagementSubscriptionsApiClient, ManagementSubscriptionsApiClient>();
builder.Services.AddScoped<IManagementCategoriesApiClient, ManagementCategoriesApiClient>();
builder.Services.AddScoped<IManagementReportsApiClient, ManagementReportsApiClient>();
builder.Services.AddScoped<IPortalAccessTokenProvider, PortalAuthStateAccessTokenProvider>();
builder.Services.AddScoped<IOperationalRealtimeClient, SignalROperationalRealtimeClient>();

await builder.Build().RunAsync();
