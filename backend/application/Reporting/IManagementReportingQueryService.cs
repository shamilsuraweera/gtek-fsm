using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Requests;

namespace GTEK.FSM.Backend.Application.Reporting;

public interface IManagementReportingQueryService
{
    Task<ManagementReportingOverviewQueryResult> GetOverviewAsync(
        AuthenticatedPrincipal principal,
        GetManagementAnalyticsOverviewRequest request,
        CancellationToken cancellationToken = default);
}
