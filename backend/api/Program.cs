var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHttpsRedirection();

// Health endpoint for deployment validation.
app.MapHealthChecks("/health");

// Versioned API route baseline for future modules.
app.MapGroup("/api/v1");

app.Run();
