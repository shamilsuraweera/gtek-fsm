namespace GTEK.FSM.WebPortal.Tests.Integration;

using Bunit;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using GTEK.FSM.Shared.Contracts.Vocabulary;
using GTEK.FSM.WebPortal.Models;
using GTEK.FSM.WebPortal.Pages.CustomerCare;
using GTEK.FSM.WebPortal.Services;
using GTEK.FSM.WebPortal.Services.Realtime;
using GTEK.FSM.WebPortal.Services.Requests;
using GTEK.FSM.WebPortal.Services.Security;
using Microsoft.Extensions.DependencyInjection;

public sealed class OperationalRealtimePageIntegrationTests : TestContext
{
    [Fact]
    public async Task Pipeline_StatusUpdate_RemovesCompletedCardFromBoard()
    {
        var realtimeClient = new FakeOperationalRealtimeClient();
        RegisterServices(realtimeClient, FakeRequestWorkspaceApiClient.CreateDefault(), this.Services);

        var cut = this.RenderComponent<Pipeline>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("REQ-1043", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));

        await realtimeClient.EmitStatusUpdateAsync(new ServiceRequestStatusUpdatedEvent
        {
            RequestId = "REQ-1043",
            TenantId = "TENANT-01",
            PreviousStatus = "Assigned",
            CurrentStatus = "Completed",
            UpdatedAtUtc = DateTime.UtcNow,
        });

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("REQ-1043", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task RequestWorkspace_StatusUpdate_UpdatesStatusSignals_AndTimeline()
    {
        var realtimeClient = new FakeOperationalRealtimeClient();
        var workspaceApiClient = FakeRequestWorkspaceApiClient.CreateDefault();
        RegisterServices(realtimeClient, workspaceApiClient, this.Services);

        var requestId = workspaceApiClient.Snapshot.Item.RequestId;

        var cut = this.RenderComponent<RequestWorkspace>(parameters => parameters
            .Add(component => component.RequestId, requestId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("REQ-1001", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));

        await realtimeClient.EmitStatusUpdateAsync(new ServiceRequestStatusUpdatedEvent
        {
            RequestId = requestId,
            TenantId = "TENANT-01",
            PreviousStatus = "Assigned",
            CurrentStatus = "Completed",
            UpdatedAtUtc = DateTime.UtcNow,
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Completed", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Live update: status changed from Assigned to Completed.", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void RequestWorkspace_HoldAction_UsesWorkspaceApi_AndReloadsState()
    {
        var realtimeClient = new FakeOperationalRealtimeClient();
        var workspaceApiClient = FakeRequestWorkspaceApiClient.CreateDefault();
        RegisterServices(realtimeClient, workspaceApiClient, this.Services);

        var cut = this.RenderComponent<RequestWorkspace>(parameters => parameters
            .Add(component => component.RequestId, workspaceApiClient.Snapshot.Item.RequestId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Assigned", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));

        cut.Find("button.btn-triage-warning").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, workspaceApiClient.TransitionCallCount);
            Assert.Contains("On Hold", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Lifecycle action applied: Hold.", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Assignments_AssignmentUpdate_UpdatesVisibleRequestAssignment()
    {
        var realtimeClient = new FakeOperationalRealtimeClient();
        RegisterServices(realtimeClient, FakeRequestWorkspaceApiClient.CreateDefault(), this.Services);

        var cut = this.RenderComponent<Assignments>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("ASN-219", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));

        var requestBefore = cut.FindAll(".assignment-request-item").First(item => item.TextContent.Contains("ASN-219", StringComparison.Ordinal));
        Assert.Contains("Unassigned", requestBefore.TextContent, StringComparison.Ordinal);

        await realtimeClient.EmitAssignmentUpdateAsync(new JobAssignmentUpdatedEvent
        {
            RequestId = "ASN-219",
            TenantId = "TENANT-01",
            JobId = "JOB-219",
            PreviousWorkerUserId = null,
            CurrentWorkerUserId = "W-100",
            AssignmentStatus = "Assigned",
            UpdatedAtUtc = DateTime.UtcNow,
        });

        cut.WaitForAssertion(() =>
        {
            var requestAfter = cut.FindAll(".assignment-request-item").First(item => item.TextContent.Contains("ASN-219", StringComparison.Ordinal));
            Assert.DoesNotContain("Unassigned", requestAfter.TextContent, StringComparison.Ordinal);
            Assert.Contains("Alice Chen", requestAfter.TextContent, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Assignments_Dispose_RemovesRealtimeSubscriptions()
    {
        var realtimeClient = new FakeOperationalRealtimeClient();
        RegisterServices(realtimeClient, FakeRequestWorkspaceApiClient.CreateDefault(), this.Services);

        var cut = this.RenderComponent<Assignments>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, realtimeClient.AssignmentSubscriberCount);
            Assert.Equal(1, realtimeClient.ConnectionStateSubscriberCount);
        }, TimeSpan.FromSeconds(3));

        ((IDisposable)cut.Instance).Dispose();

        Assert.Equal(0, realtimeClient.AssignmentSubscriberCount);
        Assert.Equal(0, realtimeClient.ConnectionStateSubscriberCount);
    }

    private static void RegisterServices(FakeOperationalRealtimeClient realtimeClient, FakeRequestWorkspaceApiClient workspaceApiClient, IServiceCollection services)
    {
        services.AddScoped<ResilientDataFetcher>();
        services.AddScoped<UiSecurityContext>();
        services.AddScoped<IOperationalRealtimeClient>(_ => realtimeClient);
        services.AddScoped<IRequestWorkspaceApiClient>(_ => workspaceApiClient);
    }

    private sealed class FakeOperationalRealtimeClient : IOperationalRealtimeClient
    {
        private readonly List<Func<ServiceRequestStatusUpdatedEvent, Task>> statusHandlers = [];
        private readonly List<Func<JobAssignmentUpdatedEvent, Task>> assignmentHandlers = [];
        private Action<OperationalRealtimeConnectionState>? connectionStateChanged;

        public bool IsEnabled => true;

        public OperationalRealtimeConnectionState ConnectionState { get; private set; } = OperationalRealtimeConnectionState.Connected;

        public int AssignmentSubscriberCount => this.assignmentHandlers.Count;

        public int ConnectionStateSubscriberCount { get; private set; }

        public event Action<OperationalRealtimeConnectionState>? ConnectionStateChanged
        {
            add
            {
                this.connectionStateChanged += value;
                this.ConnectionStateSubscriberCount++;
            }

            remove
            {
                this.connectionStateChanged -= value;
                this.ConnectionStateSubscriberCount--;
            }
        }

        public IDisposable SubscribeToStatusUpdates(Func<ServiceRequestStatusUpdatedEvent, Task> handler)
        {
            this.statusHandlers.Add(handler);
            return new Subscription(() => this.statusHandlers.Remove(handler));
        }

        public IDisposable SubscribeToAssignmentUpdates(Func<JobAssignmentUpdatedEvent, Task> handler)
        {
            this.assignmentHandlers.Add(handler);
            return new Subscription(() => this.assignmentHandlers.Remove(handler));
        }

        public Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
        {
            this.connectionStateChanged?.Invoke(this.ConnectionState);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            this.ConnectionState = OperationalRealtimeConnectionState.Disconnected;
            this.connectionStateChanged?.Invoke(this.ConnectionState);
            return Task.CompletedTask;
        }

        public async Task EmitStatusUpdateAsync(ServiceRequestStatusUpdatedEvent update)
        {
            foreach (var handler in this.statusHandlers.ToArray())
            {
                await handler(update);
            }
        }

        public async Task EmitAssignmentUpdateAsync(JobAssignmentUpdatedEvent update)
        {
            foreach (var handler in this.assignmentHandlers.ToArray())
            {
                await handler(update);
            }
        }

        private sealed class Subscription(Action dispose) : IDisposable
        {
            private Action? dispose = dispose;

            public void Dispose()
            {
                Interlocked.Exchange(ref this.dispose, null)?.Invoke();
            }
        }
    }

    private sealed class FakeRequestWorkspaceApiClient : IRequestWorkspaceApiClient
    {
        public RequestWorkspaceSnapshot Snapshot { get; private set; } = new();

        public int TransitionCallCount { get; private set; }

        public static FakeRequestWorkspaceApiClient CreateDefault()
        {
            var requestId = Guid.NewGuid().ToString();

            return new FakeRequestWorkspaceApiClient
            {
                Snapshot = new RequestWorkspaceSnapshot
                {
                    RowVersion = "row-version-1",
                    CustomerUserId = "customer-01",
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-90),
                    ActiveJobId = "job-01",
                    ActiveJobStatus = "Assigned",
                    Item = new OperationalQueueItem
                    {
                        RequestId = requestId,
                        Reference = "REQ-1001",
                        Customer = "Contoso Facilities",
                        TenantId = "TENANT-01",
                        Stage = "Assigned",
                        Priority = "High",
                        Summary = "HVAC outage escalation with tenant comfort impact.",
                        UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                        Status = RequestStage.Assigned,
                        UrgencyLevel = UrgencyLevel.High,
                        AgeMinutes = 90,
                        AssignedWorker = "worker-01",
                        AssignedWorkerId = "worker-01",
                        WorkloadHint = "Active job status: Assigned.",
                    },
                    Timeline =
                    [
                        new RequestWorkspaceTimelineEntry(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-65), "REQUEST_CREATED: Service request created."),
                        new RequestWorkspaceTimelineEntry(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-15), "JOB_ASSIGNED: Job assignment status: Assigned. (worker-01)"),
                    ],
                },
            };
        }

        public Task<RequestWorkspaceSnapshot> GetAsync(string requestId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.Snapshot);
        }

        public Task<RequestWorkspaceOperationResult> TransitionStatusAsync(string requestId, RequestStage nextStage, string? rowVersion, CancellationToken cancellationToken = default)
        {
            this.TransitionCallCount++;
            this.Snapshot.Item.Status = nextStage;
            this.Snapshot.Item.Stage = RequestStagePresentation.MapWorkspaceStage(nextStage);
            this.Snapshot.Item.UpdatedAtUtc = DateTime.UtcNow;
            this.Snapshot.RowVersion = "row-version-2";

            return Task.FromResult(new RequestWorkspaceOperationResult("Request status updated.", this.Snapshot.RowVersion));
        }

        public Task<RequestWorkspaceOperationResult> AssignWorkerAsync(string requestId, string workerUserId, string? rowVersion, bool isReassignment, CancellationToken cancellationToken = default)
        {
            this.Snapshot.Item.AssignedWorkerId = workerUserId;
            this.Snapshot.Item.AssignedWorker = workerUserId;
            this.Snapshot.ActiveJobStatus = isReassignment ? "Reassigned" : "Assigned";
            this.Snapshot.RowVersion = isReassignment ? "row-version-3" : "row-version-2";

            return Task.FromResult(new RequestWorkspaceOperationResult("Assignment updated.", this.Snapshot.RowVersion));
        }
    }
}