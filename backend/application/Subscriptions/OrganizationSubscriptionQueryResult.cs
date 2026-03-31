namespace GTEK.FSM.Backend.Application.Subscriptions;

public sealed class OrganizationSubscriptionQueryResult
{
    private OrganizationSubscriptionQueryResult(bool isSuccess, string message, string? errorCode, int? statusCode, QueriedOrganizationSubscription? payload)
    {
        this.IsSuccess = isSuccess;
        this.Message = message;
        this.ErrorCode = errorCode;
        this.StatusCode = statusCode;
        this.Payload = payload;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public string? ErrorCode { get; }

    public int? StatusCode { get; }

    public QueriedOrganizationSubscription? Payload { get; }

    public static OrganizationSubscriptionQueryResult Success(QueriedOrganizationSubscription payload, string message = "Subscription retrieved.")
    {
        return new OrganizationSubscriptionQueryResult(true, message, null, null, payload);
    }

    public static OrganizationSubscriptionQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new OrganizationSubscriptionQueryResult(false, message, errorCode, statusCode, null);
    }
}