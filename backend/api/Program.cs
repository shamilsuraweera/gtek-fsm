using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Infrastructure;

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
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHttpsRedirection();

// Health endpoint for deployment validation.
app.MapHealthChecks("/health");

// Versioned API route baseline for future modules.
app.MapGroup("/api/v1");

app.Run();
