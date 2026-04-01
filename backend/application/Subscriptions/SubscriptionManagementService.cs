using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;

namespace GTEK.FSM.Backend.Application.Subscriptions;


using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Domain.Audit;

internal sealed class SubscriptionManagementService : ISubscriptionManagementService
{
    private static readonly HashSet<string> AllowedPlans =
        new(StringComparer.OrdinalIgnoreCase) { "FREE", "PRO", "ENTERPRISE" };

    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly IUserRepository userRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IAuditLogWriter auditLogWriter;

    public SubscriptionManagementService(
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAuditLogWriter auditLogWriter)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.userRepository = userRepository;
        this.unitOfWork = unitOfWork;
        this.auditLogWriter = auditLogWriter;
    }

    public async Task<OrganizationSubscriptionQueryResult> UpdateOrganizationAsync(
        AuthenticatedPrincipal principal,
        UpdateOrganizationSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return OrganizationSubscriptionQueryResult.Failure(
                "Role is not authorized to update subscription management.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var normalizedPlanCode = request.PlanCode?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPlanCode) || !AllowedPlans.Contains(normalizedPlanCode))
        {
            return OrganizationSubscriptionQueryResult.Failure(
                "planCode must be one of FREE, PRO, ENTERPRISE.",
                "VALIDATION_PLAN_CODE_INVALID",
                400);
        }

        if (!request.UserLimit.HasValue)
        {
            return OrganizationSubscriptionQueryResult.Failure(
                "userLimit is required.",
                "VALIDATION_USER_LIMIT_REQUIRED",
                400);
        }

        var subscription = await this.subscriptionRepository.GetActiveForUpdateByTenantAsync(principal.TenantId, cancellationToken);
        if (subscription is null)
        {
            return OrganizationSubscriptionQueryResult.Failure(
                "Active subscription was not found for tenant.",
                "SUBSCRIPTION_NOT_FOUND",
                404);
        }

        if (!TryValidateRowVersion(request.RowVersion, subscription.RowVersion, out var validationErrorCode, out var validationMessage))
        {
            return OrganizationSubscriptionQueryResult.Failure(
                validationMessage,
                validationErrorCode,
                409);
        }

        var users = await this.userRepository.ListByTenantAsync(principal.TenantId, cancellationToken);
        var activeUsers = users.Count;
        if (request.UserLimit.Value < activeUsers)
        {
            return OrganizationSubscriptionQueryResult.Failure(
                "userLimit cannot be less than current active tenant users.",
                "SUBSCRIPTION_USER_LIMIT_CONFLICT",
                409);
        }

        try
        {
            subscription.ChangePlan(normalizedPlanCode.ToUpperInvariant());
            subscription.ChangeUserLimit(request.UserLimit.Value);
            this.subscriptionRepository.Update(subscription);
            try
            {
                await this.unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return OrganizationSubscriptionQueryResult.Failure(
                    "The subscription was modified by another operation. Refresh and retry.",
                    "CONCURRENCY_CONFLICT",
                    409);
            }

            // Write audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = principal.UserId,
                TenantId = principal.TenantId,
                EntityType = "Subscription",
                EntityId = subscription.Id,
                Action = $"UpdatePlan:{subscription.PlanCode},UserLimit:{subscription.UserLimit}",
                Outcome = "Success",
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Details = null
            };
            await this.auditLogWriter.WriteAsync(auditLog, cancellationToken);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return OrganizationSubscriptionQueryResult.Failure(
                ex.Message,
                "VALIDATION_USER_LIMIT_INVALID",
                400);
        }
        catch (InvalidOperationException ex)
        {
            return OrganizationSubscriptionQueryResult.Failure(
                ex.Message,
                "SUBSCRIPTION_UPDATE_INVALID",
                400);
        }

        return OrganizationSubscriptionQueryResult.Success(
            new QueriedOrganizationSubscription(
                SubscriptionId: subscription.Id,
                TenantId: subscription.TenantId,
                PlanCode: subscription.PlanCode,
                UserLimit: subscription.UserLimit,
                ActiveUsers: activeUsers,
                AvailableUserSlots: Math.Max(0, subscription.UserLimit - activeUsers),
                StartsOnUtc: subscription.StartsOnUtc,
                EndsOnUtc: subscription.EndsOnUtc,
                RowVersion: Convert.ToBase64String(subscription.RowVersion)),
            "Subscription updated.");
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }

    private static bool TryValidateRowVersion(
        string? requestRowVersion,
        byte[] currentRowVersion,
        out string errorCode,
        out string message)
    {
        errorCode = string.Empty;
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(requestRowVersion))
        {
            return true;
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(requestRowVersion.Trim());
        }
        catch (FormatException)
        {
            errorCode = "ROW_VERSION_INVALID";
            message = "rowVersion must be a valid base64 string.";
            return false;
        }

        if (!decoded.AsSpan().SequenceEqual(currentRowVersion))
        {
            errorCode = "CONCURRENCY_CONFLICT";
            message = "The subscription was modified by another operation. Refresh and retry.";
            return false;
        }

        return true;
    }
}