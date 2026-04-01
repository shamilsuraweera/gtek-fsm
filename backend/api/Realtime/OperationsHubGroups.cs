namespace GTEK.FSM.Backend.Api.Realtime;

internal static class OperationsHubGroups
{
    public static string ForTenant(Guid tenantId)
    {
        return $"tenant:{tenantId:D}";
    }

    public static string ForRequest(Guid tenantId, Guid requestId)
    {
        return $"{ForTenant(tenantId)}:request:{requestId:D}";
    }

    public static string ForJob(Guid tenantId, Guid jobId)
    {
        return $"{ForTenant(tenantId)}:job:{jobId:D}";
    }
}