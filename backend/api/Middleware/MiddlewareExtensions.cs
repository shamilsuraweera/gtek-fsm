namespace GTEK.FSM.Backend.Api.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestObservability(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestObservabilityMiddleware>();
    }

    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantResolutionMiddleware>();
    }
}
