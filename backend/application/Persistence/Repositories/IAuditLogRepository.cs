using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Audit;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> QueryAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default);

    Task<int> CountAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default);
}
