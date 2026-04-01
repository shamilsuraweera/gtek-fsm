using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Requests;

namespace GTEK.FSM.Backend.Application.Audit;

internal sealed class AuditLogQueryService : IAuditLogQueryService
{
    private const int ExportMaxRows = 10000;

    private readonly IAuditLogRepository auditLogRepository;

    public AuditLogQueryService(IAuditLogRepository auditLogRepository)
    {
        this.auditLogRepository = auditLogRepository;
    }

    public async Task<AuditLogsQueryResult> GetLogsAsync(
        AuthenticatedPrincipal principal,
        GetAuditLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return AuditLogsQueryResult.Failure(
                "Role is not authorized to access audit logs.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var page = new PageSpecification(request.Page ?? 1, request.PageSize ?? 50);
        var specification = BuildSpecification(principal, request, page);

        var items = await this.auditLogRepository.QueryAsync(specification, cancellationToken);
        var total = await this.auditLogRepository.CountAsync(specification, cancellationToken);

        var payload = new QueriedAuditLogsPage(
            Items: items.Select(Map).ToArray(),
            Page: page.NormalizedPageNumber,
            PageSize: page.NormalizedPageSize,
            Total: total);

        return AuditLogsQueryResult.Success(payload);
    }

    public async Task<AuditLogExportResult> ExportCsvAsync(
        AuthenticatedPrincipal principal,
        GetAuditLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return AuditLogExportResult.Failure(
                "Role is not authorized to access audit logs.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var specification = BuildSpecification(principal, request, new PageSpecification(1, ExportMaxRows));
        var items = await this.auditLogRepository.QueryAsync(specification, cancellationToken);

        return AuditLogExportResult.Success(items.Select(Map).ToArray());
    }

    private static AuditLogQuerySpecification BuildSpecification(
        AuthenticatedPrincipal principal,
        GetAuditLogsRequest request,
        PageSpecification? page)
    {
        return new AuditLogQuerySpecification(
            TenantId: principal.TenantId,
            ActorUserId: ParseGuid(request.ActorUserId),
            EntityType: Normalize(request.EntityType),
            EntityId: ParseGuid(request.EntityId),
            Action: Normalize(request.Action),
            Outcome: Normalize(request.Outcome),
            OccurredFromUtc: request.FromUtc,
            OccurredToUtc: request.ToUtc,
            Page: page);
    }

    private static QueriedAuditLogItem Map(Domain.Audit.AuditLog auditLog)
    {
        return new QueriedAuditLogItem(
            AuditLogId: auditLog.Id,
            TenantId: auditLog.TenantId,
            ActorUserId: auditLog.ActorUserId,
            EntityType: auditLog.EntityType,
            EntityId: auditLog.EntityId,
            Action: auditLog.Action,
            Outcome: auditLog.Outcome,
            OccurredAtUtc: auditLog.OccurredAtUtc,
            Details: auditLog.Details);
    }

    private static Guid? ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }
}
