using System.Diagnostics.Metrics;

using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Application.Identity;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Runtime;

public class RequestObservabilityMiddlewareRuntimeTests
{
    [Fact]
    public async Task InvokeAsync_WhenCorrelationHeaderMissing_SetsResponseHeader()
    {
        var middleware = new RequestObservabilityMiddleware(
            next: _ => Task.CompletedTask,
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-abc-123";
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/health";

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.TryGetValue(RequestObservabilityMiddleware.CorrelationIdHeaderName, out var value));
        Assert.Equal("trace-abc-123", value.ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationHeaderProvided_PreservesHeaderValue()
    {
        var middleware = new RequestObservabilityMiddleware(
            next: _ => Task.CompletedTask,
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/health";
        context.Request.Headers[RequestObservabilityMiddleware.CorrelationIdHeaderName] = "corr-user-001";

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.TryGetValue(RequestObservabilityMiddleware.CorrelationIdHeaderName, out var value));
        Assert.Equal("corr-user-001", value.ToString());
    }

    [Fact]
    public async Task InvokeAsync_RecordsApiRequestMetricsWithTenantTag()
    {
        long requestCountMeasurements = 0;
        double durationMeasurements = 0;
        string? observedTenantTag = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "GTEK.FSM.Backend.Api"
                && (instrument.Name == "api_requests_total" || instrument.Name == "api_request_duration_ms"))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name != "api_requests_total")
            {
                return;
            }

            requestCountMeasurements += measurement;

            foreach (var tag in tags)
            {
                if (tag.Key == "tenant")
                {
                    observedTenantTag = tag.Value?.ToString();
                    break;
                }
            }
        });

        listener.SetMeasurementEventCallback<double>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "api_request_duration_ms")
            {
                durationMeasurements += measurement;
            }
        });

        listener.Start();

        var middleware = new RequestObservabilityMiddleware(
            next: async ctx =>
            {
                await Task.Yield();
                ctx.Response.StatusCode = StatusCodes.Status201Created;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/requests";
        context.Items[TenantContextConstants.HttpContextItemKey] = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await middleware.InvokeAsync(context);
        listener.RecordObservableInstruments();

        Assert.True(requestCountMeasurements >= 1);
        Assert.True(durationMeasurements >= 0);
        Assert.Equal("00000000-0000-0000-0000-000000000001", observedTenantTag);
    }
}
