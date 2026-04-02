namespace GTEK.FSM.MobileApp.Tests.Notifications;

using GTEK.FSM.MobileApp.Services.Notifications;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

public sealed class MobileNotificationPayloadFactoryTests
{
    [Fact]
    public void TryBuildForStatusUpdate_ReturnsPayload_ForMatchingCustomerTenant()
    {
        var payload = new ServiceRequestStatusUpdatedEvent
        {
            RequestId = "REQ-100",
            TenantId = "tenant-a",
            CurrentStatus = "InProgress",
            UpdatedAtUtc = DateTime.UtcNow,
        };

        var success = MobileNotificationPayloadFactory.TryBuildForStatusUpdate(
            payload,
            role: "Customer",
            tenantId: "tenant-a",
            out var notification);

        Assert.True(success);
        Assert.Equal("service_request.status_updated", notification.NotificationType);
        Assert.Contains("CustomerRequests", notification.Route);
        Assert.Contains("requestId=REQ-100", notification.Route);
    }

    [Fact]
    public void TryBuildForStatusUpdate_ReturnsFalse_ForTenantMismatch()
    {
        var payload = new ServiceRequestStatusUpdatedEvent
        {
            RequestId = "REQ-100",
            TenantId = "tenant-a",
            CurrentStatus = "InProgress",
            UpdatedAtUtc = DateTime.UtcNow,
        };

        var success = MobileNotificationPayloadFactory.TryBuildForStatusUpdate(
            payload,
            role: "Customer",
            tenantId: "tenant-b",
            out _);

        Assert.False(success);
    }

    [Fact]
    public void TryBuildForAssignmentUpdate_ReturnsPayload_ForMatchingWorkerTenant()
    {
        var payload = new JobAssignmentUpdatedEvent
        {
            JobId = "JOB-200",
            RequestId = "REQ-200",
            TenantId = "tenant-a",
            AssignmentStatus = "Accepted",
            UpdatedAtUtc = DateTime.UtcNow,
        };

        var success = MobileNotificationPayloadFactory.TryBuildForAssignmentUpdate(
            payload,
            role: "Worker",
            tenantId: "tenant-a",
            out var notification);

        Assert.True(success);
        Assert.Equal("job.assignment_updated", notification.NotificationType);
        Assert.Contains("WorkerJobs", notification.Route);
        Assert.Contains("jobId=JOB-200", notification.Route);
        Assert.Contains("requestId=REQ-200", notification.Route);
    }

    [Fact]
    public void TryBuildForAssignmentUpdate_ReturnsFalse_ForNonWorkerRole()
    {
        var payload = new JobAssignmentUpdatedEvent
        {
            JobId = "JOB-200",
            RequestId = "REQ-200",
            TenantId = "tenant-a",
            AssignmentStatus = "Accepted",
            UpdatedAtUtc = DateTime.UtcNow,
        };

        var success = MobileNotificationPayloadFactory.TryBuildForAssignmentUpdate(
            payload,
            role: "Customer",
            tenantId: "tenant-a",
            out _);

        Assert.False(success);
    }
}
