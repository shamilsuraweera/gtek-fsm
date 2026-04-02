namespace GTEK.FSM.MobileApp.Tests.Api;

using System.Net;
using System.Net.Http;
using System.Text;
using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Diagnostics;
using GTEK.FSM.MobileApp.Services.Identity;

public sealed class OperationalDataQueryServiceTests
{
    [Fact]
    public async Task QueryActiveCategoriesAsync_ReturnsLiveItems_WhenEnvelopeContainsItems()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var responseJson = """
            {
              "success": true,
              "data": {
                "items": [
                  { "categoryId": "cat-1", "name": "Electrical", "code": "ELEC", "sortOrder": 10, "isEnabled": true },
                  { "categoryId": "cat-2", "name": "Plumbing", "code": "PLMB", "sortOrder": 20, "isEnabled": true }
                ]
              }
            }
            """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        });

        var service = BuildService(handler, token: "jwt-token");

        var result = await service.QueryActiveCategoriesAsync();

        Assert.True(result.IsLive);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("cat-1", result.Items[0].CategoryId);
        Assert.Equal("Electrical", result.Items[0].Name);
    }

    [Fact]
    public async Task CreateRequestAsync_ReturnsCreatedResponse_WhenEnvelopeContainsDataObject()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v1/requests", request.RequestUri?.PathAndQuery);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("jwt-token", request.Headers.Authorization?.Parameter);

            var responseJson = """
            {
              "success": true,
              "data": {
                "requestId": "REQ-901",
                "tenantId": "tenant-1",
                "customerUserId": "customer-1",
                "title": "Electrical: Main panel trips every evening",
                "status": "New",
                "createdAtUtc": "2026-04-02T10:00:00Z",
                "updatedAtUtc": "2026-04-02T10:00:00Z"
              }
            }
            """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        });

        var service = BuildService(handler, token: "jwt-token");

        var result = await service.CreateRequestAsync("Electrical: Main panel trips every evening");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Request);
        Assert.Equal("REQ-901", result.Request!.RequestId);
        Assert.Equal("New", result.Request.Status);
    }

    [Fact]
    public async Task CreateRequestAsync_ReturnsFailure_WhenTokenMissing()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var service = BuildService(handler, token: string.Empty);

        var result = await service.CreateRequestAsync("Plumbing: Bathroom pipe leak");

        Assert.False(result.IsSuccess);
        Assert.Equal(string.Empty, result.Request.RequestId);
        Assert.Equal("Missing token", result.Message);
    }

    private static OperationalDataQueryService BuildService(HttpMessageHandler handler, string token)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5050"),
        };

        return new OperationalDataQueryService(
            httpClient,
            new StubTokenProvider(token),
            new NoOpDiagnosticsLogger());
    }

    private sealed class StubTokenProvider : IIdentityTokenProvider
    {
        private readonly string _token;

        public StubTokenProvider(string token)
        {
            _token = token;
        }

        public string GetAccessToken()
        {
            return _token;
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

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _responseFactory(request);
        }
    }
}
