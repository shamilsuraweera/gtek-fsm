using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.Workers;

public sealed record QueriedWorkerProfileItem(
    Guid WorkerId,
    Guid TenantId,
    string WorkerCode,
    string DisplayName,
    decimal InternalRating,
    WorkerAvailabilityStatus AvailabilityStatus,
    bool IsActive,
    IReadOnlyList<string> Skills,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
