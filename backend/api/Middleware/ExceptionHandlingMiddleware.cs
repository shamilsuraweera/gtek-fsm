using System.Net;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Results;

namespace GTEK.FSM.Backend.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request.");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = ApiErrorResponse.Create(
                message: "An unexpected error occurred.",
                errorCode: "UNHANDLED_ERROR",
                traceId: context.TraceIdentifier);

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
