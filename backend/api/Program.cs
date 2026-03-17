using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Infrastructure;
using GTEK.FSM.Shared.Contracts.Results;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
	.AddHealthChecks()
	.AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

var app = builder.Build();

app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();

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

// Versioned API route baseline for future modules.
var v1 = app.MapGroup("/api/v1");

v1.MapGet("/ping", (HttpContext context) =>
	Results.Ok(ApiResponse<object>.Ok(
		data: new { status = "ok" },
		message: "API is reachable.",
		traceId: context.TraceIdentifier)));

v1.MapGet("/error-test", () =>
{
	throw new InvalidOperationException("Error test endpoint triggered.");
});

app.Run();
