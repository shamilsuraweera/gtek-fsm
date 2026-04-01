using System.Security.Claims;

using GTEK.FSM.Backend.Application.Identity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GTEK.FSM.Backend.Api.Realtime;

[Authorize(Policy = AuthorizationPolicyCatalog.RealTimeOperations)]
public sealed class OperationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = this.GetCurrentTenantId(requireResolvedTenant: true);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, OperationsHubGroups.ForTenant(tenantId));
        await base.OnConnectedAsync();
    }

    public Task<string> GetTenantChannelAsync()
    {
        var tenantId = this.GetCurrentTenantId(requireResolvedTenant: true);
        return Task.FromResult(OperationsHubGroups.ForTenant(tenantId));
    }

    public async Task<string> SubscribeToTenantChannelAsync(string tenantId)
    {
        var currentTenantId = this.GetCurrentTenantId(requireResolvedTenant: true);
        var targetTenantId = ParseGuid(tenantId, "tenantId must be a valid guid.");

        if (currentTenantId != targetTenantId)
        {
            throw new HubException("Cross-tenant subscription is forbidden.");
        }

        var groupName = OperationsHubGroups.ForTenant(currentTenantId);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, groupName);
        return groupName;
    }

    public async Task<string> SubscribeToRequestChannelAsync(string requestId)
    {
        var tenantId = this.GetCurrentTenantId(requireResolvedTenant: true);
        var parsedRequestId = ParseGuid(requestId, "requestId must be a valid guid.");

        var groupName = OperationsHubGroups.ForRequest(tenantId, parsedRequestId);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, groupName);
        return groupName;
    }

    public async Task<string> SubscribeToJobChannelAsync(string jobId)
    {
        var tenantId = this.GetCurrentTenantId(requireResolvedTenant: true);
        var parsedJobId = ParseGuid(jobId, "jobId must be a valid guid.");

        var groupName = OperationsHubGroups.ForJob(tenantId, parsedJobId);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, groupName);
        return groupName;
    }

    private Guid GetCurrentTenantId(bool requireResolvedTenant)
    {
        var claimTenantText = this.Context.User?.FindFirstValue(TokenClaimNames.TenantId);
        if (!Guid.TryParse(claimTenantText, out var claimTenantId) || claimTenantId == Guid.Empty)
        {
            throw new HubException("Tenant context is required.");
        }

        var httpContext = this.Context.GetHttpContext();
        if (httpContext is null)
        {
            if (requireResolvedTenant)
            {
                throw new HubException("Tenant ownership validation failed.");
            }

            return claimTenantId;
        }

        if (!httpContext.Items.TryGetValue(TenantContextConstants.HttpContextItemKey, out var resolvedTenantValue)
            || !TryResolveTenantId(resolvedTenantValue, out var resolvedTenantId))
        {
            if (requireResolvedTenant)
            {
                throw new HubException("Tenant ownership validation failed.");
            }

            return claimTenantId;
        }

        if (resolvedTenantId != claimTenantId)
        {
            throw new HubException("Tenant ownership validation failed.");
        }

        return claimTenantId;
    }

    private static Guid ParseGuid(string value, string errorMessage)
    {
        if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
        {
            throw new HubException(errorMessage);
        }

        return parsed;
    }

    private static bool TryResolveTenantId(object? rawValue, out Guid tenantId)
    {
        switch (rawValue)
        {
            case Guid parsedGuid when parsedGuid != Guid.Empty:
                tenantId = parsedGuid;
                return true;
            case string text when Guid.TryParse(text, out var parsedTextGuid) && parsedTextGuid != Guid.Empty:
                tenantId = parsedTextGuid;
                return true;
            default:
                tenantId = Guid.Empty;
                return false;
        }
    }
}