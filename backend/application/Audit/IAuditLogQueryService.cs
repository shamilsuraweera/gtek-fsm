using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Requests;

namespace GTEK.FSM.Backend.Application.Audit;

public interface IAuditLogQueryService
{
    Task<AuditLogsQueryResult> GetLogsAsync(
        AuthenticatedPrincipal principal,
        GetAuditLogsRequest request,
        CancellationToken cancellationToken = default);

    Task<AuditLogExportResult> ExportCsvAsync(
        AuthenticatedPrincipal principal,
        GetAuditLogsRequest request,
        CancellationToken cancellationToken = default);
}
