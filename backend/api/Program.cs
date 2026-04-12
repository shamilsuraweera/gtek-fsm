using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Api.Authentication;
using GTEK.FSM.Backend.Api.Authorization;
using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Api.Realtime;
using GTEK.FSM.Backend.Api.Routing;
using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Realtime;
using GTEK.FSM.Backend.Infrastructure;
using GTEK.FSM.Backend.Infrastructure.Configuration;
using GTEK.FSM.Shared.Contracts.Results;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Explicit environment-aware configuration load order:
// 1) base appsettings.json
// 2) appsettings.{Environment}.json
// 3) environment variables (highest precedence)
builder.Configuration.Sources.Clear();
builder.Configuration
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
	.AddEnvironmentVariables();

var signalROptions = builder.Configuration.GetSection("SignalR").Get<SignalROptions>() ?? new SignalROptions();
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
	?? new[] { "http://localhost:5001" };

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddApiAuthorizationPolicies();
builder.Services.AddCors(options =>
{
	options.AddPolicy("PortalLocal", policy =>
	{
		policy.WithOrigins(allowedCorsOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});
builder.Services.AddScoped<ILocalPasswordHasher, Pbkdf2LocalPasswordHasher>();
builder.Services.AddScoped<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<ILocalAuthService, LocalAuthService>();
builder.Services.AddScoped<ILocalAuthBootstrapService, LocalAuthBootstrapService>();
builder.Services.AddScoped<IOperationalUpdatePublisher, SignalROperationalUpdatePublisher>();
builder.Services.AddSignalR(options =>
{
	options.EnableDetailedErrors = signalROptions.EnableDetailedErrors;
	options.ClientTimeoutInterval = TimeSpan.FromSeconds(signalROptions.ClientTimeoutSeconds);
	options.HandshakeTimeout = TimeSpan.FromSeconds(signalROptions.HandshakeTimeoutSeconds);
});
builder.Services.Configure<TenantResolutionOptions>(builder.Configuration.GetSection(TenantResolutionOptions.SectionName));
builder.Services
	.AddHealthChecks()
	.AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

var app = builder.Build();
var resolvedSignalROptions = app.Services.GetRequiredService<IOptions<SignalROptions>>().Value;

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    using var scope = app.Services.CreateScope();
    var bootstrapService = scope.ServiceProvider.GetRequiredService<ILocalAuthBootstrapService>();
    await bootstrapService.EnsureBootstrappedAsync();
}

app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();
app.UseCors("PortalLocal");
app.UseAuthentication();
app.UseTenantResolution();
app.UseAuthorization();

// Health endpoint for deployment validation.
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("ready"),
	ResponseWriter = async (context, report) =>
	{
		context.Response.ContentType = "application/json";

		var payload = new
		{
			status = report.Status.ToString(),
			checks = report.Entries.Select(entry => new
			{
				name = entry.Key,
				status = entry.Value.Status.ToString()
			})
		};

		await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
	}
});

// Versioned API route organization baseline for future modules.
app.MapV1Endpoints();
app.MapHub<OperationsHub>(resolvedSignalROptions.HubPath)
	.RequireAuthorization(AuthorizationPolicyCatalog.RealTimeOperations);

app.Run();
