using System.Diagnostics;
using System.Diagnostics.Metrics;
using GTEK.FSM.Backend.Application.Identity;

namespace GTEK.FSM.Backend.Api.Middleware;

public sealed class RequestObservabilityMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";

    private static readonly Meter Meter = new("GTEK.FSM.Backend.Api", "1.0.0");
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>(
        name: "api_requests_total",
        unit: "requests",
        description: "Total API requests processed.");
    private static readonly Histogram<double> RequestDurationHistogram = Meter.CreateHistogram<double>(
        name: "api_request_duration_ms",
        unit: "ms",
        description: "API request duration in milliseconds.");

    private readonly RequestDelegate next;
    private readonly ILogger<RequestObservabilityMiddleware> logger;

    public RequestObservabilityMiddleware(RequestDelegate next, ILogger<RequestObservabilityMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = ResolveCorrelationId(context);

        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        using var scope = this.logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
        });

        this.logger.LogInformation(
            "request_started method={Method} path={Path}",
            context.Request.Method,
            context.Request.Path.Value ?? "/");

        await this.next(context);

        stopwatch.Stop();

        var statusCode = context.Response.StatusCode;
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var tenantTag = ResolveTenantTag(context);

        RequestCounter.Add(1,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("path", path),
            new KeyValuePair<string, object?>("status_code", statusCode),
            new KeyValuePair<string, object?>("tenant", tenantTag));

        RequestDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("path", path),
            new KeyValuePair<string, object?>("status_code", statusCode),
            new KeyValuePair<string, object?>("tenant", tenantTag));

        this.logger.LogInformation(
            "request_completed method={Method} path={Path} statusCode={StatusCode} elapsedMs={ElapsedMs}",
            method,
            path,
            statusCode,
            stopwatch.Elapsed.TotalMilliseconds);
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var values)
            && !string.IsNullOrWhiteSpace(values.ToString()))
        {
            return values.ToString().Trim();
        }

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }

    private static string ResolveTenantTag(HttpContext context)
    {
        if (context.Items.TryGetValue(TenantContextConstants.HttpContextItemKey, out var tenantObj)
            && tenantObj is Guid tenantId
            && tenantId != Guid.Empty)
        {
            return tenantId.ToString();
        }

        return "unresolved";
    }
}
