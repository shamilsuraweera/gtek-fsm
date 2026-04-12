namespace GTEK.FSM.MobileApp.Tests.Api;

using System.Net;
using System.Net.Http;
using System.Text;
using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Diagnostics;
using GTEK.FSM.MobileApp.Services.Identity;

public sealed class CrossChannelParityScenarioTests
{
    [Fact]
    public async Task TransitionRequestStatusAsync_ParitySuccessEnvelope_ReturnsEquivalentOperationResult()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var responseJson = """
            {
              "success": true,
              "message": "Service request status transitioned.",
              "data": {
                "requestId": "11111111-1111-1111-1111-111111111111",
                "tenantId": "tenant-1",
                "previousStatus": "Assigned",
                "currentStatus": "InProgress",
                "updatedAtUtc": "2026-04-02T10:00:00Z",
                "rowVersion": "AQID"
              }
            }
            """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        });

        var service = BuildService(handler, token: "jwt-token");

        var result = await service.TransitionRequestStatusAsync(
            requestId: "11111111-1111-1111-1111-111111111111",
            nextStatus: "InProgress",
            rowVersion: "AQID");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsConflict);
        Assert.Equal("InProgress", result.Transition.CurrentStatus);
        Assert.Equal("AQID", result.Transition.RowVersion);
    }

    [Fact]
    public async Task TransitionRequestStatusAsync_ParityConflictEnvelope_ReturnsDeterministicConflict()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var responseJson = """
            {
              "success": false,
              "message": "The request was modified by another operation. Refresh and retry.",
              "errorCode": "CONCURRENCY_CONFLICT",
              "currentRowVersion": "BQYH"
            }
            """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        });

        var service = BuildService(handler, token: "jwt-token");

        var result = await service.TransitionRequestStatusAsync(
            requestId: "11111111-1111-1111-1111-111111111111",
            nextStatus: "InProgress",
            rowVersion: "AQID");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsConflict);
        Assert.Equal("CONCURRENCY_CONFLICT", result.ErrorCode);
        Assert.Contains("Refresh and retry", result.Message, StringComparison.Ordinal);
    }

    private static OperationalDataQueryService BuildService(HttpMessageHandler handler, string token)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5050"),
        };

        return new OperationalDataQueryService(
            httpClient,
            new StaticTokenProvider(token),
            new NoOpDiagnosticsLogger());
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return responder(request);
        }
    }

    private sealed class StaticTokenProvider(string token) : IIdentityTokenProvider
    {
        public string GetAccessToken()
        {
            return token;
        }

        public void SetAccessToken(string accessToken) { }

        public void ClearAccessToken()
        {
        }
    }

    private sealed class NoOpDiagnosticsLogger : IMobileDiagnosticsLogger
    {
        public void Info(string category, string message)
        {
        }

        public void Warn(string category, string message)
        {
        }

        public void Error(string category, string message)
        {
        }
    }
}