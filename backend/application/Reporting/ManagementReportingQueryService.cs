using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Requests;

namespace GTEK.FSM.Backend.Application.Reporting;

internal sealed class ManagementReportingQueryService : IManagementReportingQueryService
{
    private const int MaxTrendSampleRows = 500;

    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IJobRepository jobRepository;
    private readonly IAuditLogRepository auditLogRepository;

    public ManagementReportingQueryService(
        IServiceRequestRepository serviceRequestRepository,
        IJobRepository jobRepository,
        IAuditLogRepository auditLogRepository)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.jobRepository = jobRepository;
        this.auditLogRepository = auditLogRepository;
    }

    public async Task<ManagementReportingOverviewQueryResult> GetOverviewAsync(
        AuthenticatedPrincipal principal,
        GetManagementAnalyticsOverviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return ManagementReportingOverviewQueryResult.Failure(
                "Role is not authorized to access management analytics.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var windowDays = Math.Clamp(request.WindowDays ?? 7, 3, 30);
        var trendBuckets = Math.Clamp(request.TrendBuckets ?? 7, 3, 14);

        var nowUtc = DateTime.UtcNow;
        var windowFromUtc = nowUtc.AddDays(-windowDays);

        var totalRequests = await this.serviceRequestRepository.CountAsync(
            new ServiceRequestQuerySpecification(
                TenantId: principal.TenantId,
                CreatedFromUtc: windowFromUtc,
                CreatedToUtc: nowUtc),
            cancellationToken);

        var completedRequests = await this.serviceRequestRepository.CountAsync(
            new ServiceRequestQuerySpecification(
                TenantId: principal.TenantId,
                Status: ServiceRequestStatus.Completed,
                CreatedFromUtc: windowFromUtc,
                CreatedToUtc: nowUtc),
            cancellationToken);

        var activeJobs = await this.jobRepository.CountAsync(
            new JobQuerySpecification(
                TenantId: principal.TenantId,
                AssignmentStatus: AssignmentStatus.Accepted),
            cancellationToken);

        var twentyFourHoursAgo = nowUtc.AddHours(-24);
        var recentAuditSpec = new AuditLogQuerySpecification(
            TenantId: principal.TenantId,
            OccurredFromUtc: twentyFourHoursAgo,
            OccurredToUtc: nowUtc,
            Page: new PageSpecification(1, MaxTrendSampleRows));

        var recentAuditLogs = await this.auditLogRepository.QueryAsync(recentAuditSpec, cancellationToken);
        var sensitiveActions24h = recentAuditLogs.Count;
        var deniedActions24h = recentAuditLogs.Count(x => !string.Equals(x.Outcome, "Success", StringComparison.OrdinalIgnoreCase));

        var intakeTrend = new List<QueriedManagementTrendPoint>(trendBuckets);
        var completionTrend = new List<QueriedManagementTrendPoint>(trendBuckets);

        for (var i = trendBuckets - 1; i >= 0; i--)
        {
            var bucketStart = nowUtc.Date.AddDays(-i);
            var bucketEnd = bucketStart.AddDays(1).AddTicks(-1);

            var intakeCount = await this.serviceRequestRepository.CountAsync(
                new ServiceRequestQuerySpecification(
                    TenantId: principal.TenantId,
                    CreatedFromUtc: bucketStart,
                    CreatedToUtc: bucketEnd),
                cancellationToken);

            var completionCount = await this.auditLogRepository.CountAsync(
                new AuditLogQuerySpecification(
                    TenantId: principal.TenantId,
                    Action: "COMPLETED",
                    OccurredFromUtc: new DateTimeOffset(bucketStart),
                    OccurredToUtc: new DateTimeOffset(bucketEnd)),
                cancellationToken);

            intakeTrend.Add(new QueriedManagementTrendPoint(bucketStart, intakeCount));
            completionTrend.Add(new QueriedManagementTrendPoint(bucketStart, completionCount));
        }

        var windowAuditLogs = await this.auditLogRepository.QueryAsync(
            new AuditLogQuerySpecification(
                TenantId: principal.TenantId,
                OccurredFromUtc: new DateTimeOffset(windowFromUtc),
                OccurredToUtc: new DateTimeOffset(nowUtc),
                Page: new PageSpecification(1, MaxTrendSampleRows)),
            cancellationToken);

        var actionDrilldown = windowAuditLogs
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Action) ? "Unknown" : x.Action)
            .Select(x => new QueriedManagementDrilldownItem(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToArray();

        var outcomeDrilldown = windowAuditLogs
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Outcome) ? "Unknown" : x.Outcome)
            .Select(x => new QueriedManagementDrilldownItem(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToArray();

        var anomalies = BuildAnomalies(totalRequests, completedRequests, activeJobs, deniedActions24h);

        var payload = new QueriedManagementAnalyticsOverview(
            TotalRequestsInWindow: totalRequests,
            CompletedRequestsInWindow: completedRequests,
            ActiveJobs: activeJobs,
            SensitiveActions24h: sensitiveActions24h,
            DeniedActions24h: deniedActions24h,
            IntakeTrend: intakeTrend,
            CompletionTrend: completionTrend,
            Anomalies: anomalies,
            ActionDrilldown: actionDrilldown,
            OutcomeDrilldown: outcomeDrilldown);

        return ManagementReportingOverviewQueryResult.Success(payload);
    }

    private static IReadOnlyList<QueriedManagementAnomalyIndicator> BuildAnomalies(
        int totalRequests,
        int completedRequests,
        int activeJobs,
        int deniedActions24h)
    {
        var anomalies = new List<QueriedManagementAnomalyIndicator>();

        if (deniedActions24h >= 5)
        {
            anomalies.Add(new QueriedManagementAnomalyIndicator(
                "DENIED_ACTION_SPIKE",
                "High",
                $"Denied or failed sensitive actions in last 24h: {deniedActions24h}."));
        }

        if (totalRequests > (completedRequests * 2) && totalRequests >= 10)
        {
            anomalies.Add(new QueriedManagementAnomalyIndicator(
                "REQUEST_BACKLOG_GROWTH",
                "Medium",
                "Request intake is outpacing completion in the selected window."));
        }

        if (activeJobs >= 25)
        {
            anomalies.Add(new QueriedManagementAnomalyIndicator(
                "ACTIVE_JOB_PRESSURE",
                "Medium",
                $"Active accepted jobs are elevated ({activeJobs})."));
        }

        if (anomalies.Count == 0)
        {
            anomalies.Add(new QueriedManagementAnomalyIndicator(
                "NO_CRITICAL_ANOMALY",
                "Low",
                "No high-risk anomaly indicators detected for the selected window."));
        }

        return anomalies;
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }
}
