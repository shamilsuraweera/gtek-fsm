namespace GTEK.FSM.WebPortal.Tests.Integration;

using System.Net;
using System.Text;
using GTEK.FSM.Shared.Contracts.Vocabulary;
using GTEK.FSM.WebPortal.Services.Requests;

public sealed class CrossChannelParityScenarioTests
{
    [Fact]
    public async Task TransitionStatusAsync_ParitySuccessEnvelope_ReturnsEquivalentOperationResult()
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

        var apiClient = BuildClient(handler);

        var result = await apiClient.TransitionStatusAsync(
            requestId: "11111111-1111-1111-1111-111111111111",
            nextStage: RequestStage.InProgress,
            rowVersion: "AQID");

        Assert.Equal("Service request status transitioned.", result.Message);
        Assert.Equal("AQID", result.RowVersion);
    }

    [Fact]
    public async Task TransitionStatusAsync_ParityConflictEnvelope_ThrowsConflictWithCurrentRowVersion()
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

        var apiClient = BuildClient(handler);

        var exception = await Assert.ThrowsAsync<RequestWorkspaceConflictException>(async () =>
            await apiClient.TransitionStatusAsync(
                requestId: "11111111-1111-1111-1111-111111111111",
                nextStage: RequestStage.InProgress,
                rowVersion: "AQID"));

        Assert.Equal("CONCURRENCY_CONFLICT", exception.ErrorCode);
        Assert.Equal("BQYH", exception.CurrentRowVersion);
        Assert.Contains("Refresh and retry", exception.Message, StringComparison.Ordinal);
    }

    private static RequestWorkspaceApiClient BuildClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5050/"),
        };

        return new RequestWorkspaceApiClient(httpClient);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return responder(request);
        }
    }
}