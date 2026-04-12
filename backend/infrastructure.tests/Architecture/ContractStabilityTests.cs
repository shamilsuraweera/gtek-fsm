using System.Reflection;

using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Responses;
using GTEK.FSM.Shared.Contracts.Results;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Architecture;

/// <summary>
/// Verifies that critical shared-contract DTO types still carry their required members.
/// Failures here indicate a breaking contract change that would break API consumers.
/// </summary>
public class ContractStabilityTests
{
    // ── Request DTOs ────────────────────────────────────────────────────────

    [Fact]
    public void CreateServiceRequestRequest_HasRequiredMembers()
    {
        AssertProperty<CreateServiceRequestRequest>(nameof(CreateServiceRequestRequest.Title));
    }

    [Fact]
    public void AssignServiceRequestRequest_HasRequiredMembers()
    {
        AssertProperty<AssignServiceRequestRequest>(nameof(AssignServiceRequestRequest.WorkerUserId));
        AssertProperty<AssignServiceRequestRequest>(nameof(AssignServiceRequestRequest.RowVersion));
    }

    [Fact]
    public void ReassignServiceRequestRequest_HasRequiredMembers()
    {
        AssertProperty<ReassignServiceRequestRequest>(nameof(ReassignServiceRequestRequest.WorkerUserId));
        AssertProperty<ReassignServiceRequestRequest>(nameof(ReassignServiceRequestRequest.RowVersion));
    }

    [Fact]
    public void CreateWorkerProfileRequest_HasRequiredMembers()
    {
        AssertProperty<CreateWorkerProfileRequest>(nameof(CreateWorkerProfileRequest.WorkerCode));
        AssertProperty<CreateWorkerProfileRequest>(nameof(CreateWorkerProfileRequest.DisplayName));
        AssertProperty<CreateWorkerProfileRequest>(nameof(CreateWorkerProfileRequest.IsActive));
    }

    // ── Response DTOs ───────────────────────────────────────────────────────

    [Fact]
    public void CreateServiceRequestResponse_HasRequiredMembers()
    {
        AssertProperty<CreateServiceRequestResponse>(nameof(CreateServiceRequestResponse.RequestId));
        AssertProperty<CreateServiceRequestResponse>(nameof(CreateServiceRequestResponse.TenantId));
        AssertProperty<CreateServiceRequestResponse>(nameof(CreateServiceRequestResponse.Status));
        AssertProperty<CreateServiceRequestResponse>(nameof(CreateServiceRequestResponse.CreatedAtUtc));
    }

    [Fact]
    public void GetJobDetailResponse_HasRequiredMembers()
    {
        AssertProperty<GetJobDetailResponse>(nameof(GetJobDetailResponse.JobId));
        AssertProperty<GetJobDetailResponse>(nameof(GetJobDetailResponse.TenantId));
        AssertProperty<GetJobDetailResponse>(nameof(GetJobDetailResponse.RequestId));
        AssertProperty<GetJobDetailResponse>(nameof(GetJobDetailResponse.AssignmentStatus));
        AssertProperty<GetJobDetailResponse>(nameof(GetJobDetailResponse.Timeline));
    }

    [Fact]
    public void WorkerProfileResponse_HasRequiredMembers()
    {
        AssertProperty<WorkerProfileResponse>(nameof(WorkerProfileResponse.WorkerId));
        AssertProperty<WorkerProfileResponse>(nameof(WorkerProfileResponse.TenantId));
        AssertProperty<WorkerProfileResponse>(nameof(WorkerProfileResponse.WorkerCode));
        AssertProperty<WorkerProfileResponse>(nameof(WorkerProfileResponse.AvailabilityStatus));
        AssertProperty<WorkerProfileResponse>(nameof(WorkerProfileResponse.Skills));
        AssertProperty<WorkerProfileResponse>(nameof(WorkerProfileResponse.IsActive));
    }

    [Fact]
    public void GetAuditLogResponse_HasRequiredMembers()
    {
        AssertProperty<GetAuditLogResponse>(nameof(GetAuditLogResponse.AuditLogId));
        AssertProperty<GetAuditLogResponse>(nameof(GetAuditLogResponse.TenantId));
        AssertProperty<GetAuditLogResponse>(nameof(GetAuditLogResponse.ActorUserId));
        AssertProperty<GetAuditLogResponse>(nameof(GetAuditLogResponse.EntityType));
        AssertProperty<GetAuditLogResponse>(nameof(GetAuditLogResponse.Action));
        AssertProperty<GetAuditLogResponse>(nameof(GetAuditLogResponse.Outcome));
        AssertProperty<GetAuditLogResponse>(nameof(GetAuditLogResponse.OccurredAtUtc));
    }

    [Fact]
    public void GetOrganizationSubscriptionResponse_HasRequiredMembers()
    {
        AssertProperty<GetOrganizationSubscriptionResponse>(nameof(GetOrganizationSubscriptionResponse.SubscriptionId));
        AssertProperty<GetOrganizationSubscriptionResponse>(nameof(GetOrganizationSubscriptionResponse.TenantId));
        AssertProperty<GetOrganizationSubscriptionResponse>(nameof(GetOrganizationSubscriptionResponse.PlanCode));
        AssertProperty<GetOrganizationSubscriptionResponse>(nameof(GetOrganizationSubscriptionResponse.UserLimit));
        AssertProperty<GetOrganizationSubscriptionResponse>(nameof(GetOrganizationSubscriptionResponse.RowVersion));
    }

    // ── Envelope stability ──────────────────────────────────────────────────

    [Fact]
    public void ApiResponse_HasSuccessAndFailFactories()
    {
        var type = typeof(ApiResponse<object>);
        Assert.NotNull(type.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(type.GetMethod("Fail", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(type.GetProperty("Success", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(type.GetProperty("Data", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(type.GetProperty("ErrorCode", BindingFlags.Public | BindingFlags.Instance));
    }

    // ── No PlaceholderCompany headers in shared contracts assembly ──────────

    [Fact]
    public void SharedContractsAssembly_ContainsNoPlaceholderCompanyNamespace()
    {
        var assembly = typeof(CreateServiceRequestRequest).Assembly;
        var types = assembly.GetTypes();
        foreach (var type in types)
        {
            Assert.False(
                type.Namespace?.Contains("PlaceholderCompany", StringComparison.OrdinalIgnoreCase) == true,
                $"Type '{type.FullName}' is under a PlaceholderCompany namespace — scaffold not cleaned up.");
        }
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static void AssertProperty<T>(string propertyName)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.True(prop is not null, $"{typeof(T).Name} is missing required property '{propertyName}'.");
    }
}
