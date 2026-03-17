using GTEK.FSM.Shared.Contracts.Results;

namespace GTEK.FSM.Backend.Api.Routing;

public static class V1RouteGroupExtensions
{
    public static IEndpointRouteBuilder MapV1Endpoints(this IEndpointRouteBuilder app)
    {
        // Business and feature endpoints register under versioned route groups.
        var v1 = app.MapGroup(ApiRouteConstants.V1);

        v1.MapGet("/ping", (HttpContext context) =>
            Results.Ok(ApiResponse<object>.Ok(
                data: new { status = "ok" },
                message: "API is reachable.",
                traceId: context.TraceIdentifier)));

        v1.MapGet("/error-test", () =>
        {
            throw new InvalidOperationException("Error test endpoint triggered.");
        });

        // Operational endpoints (for example /health) remain outside versioned groups.
        return app;
    }
}
