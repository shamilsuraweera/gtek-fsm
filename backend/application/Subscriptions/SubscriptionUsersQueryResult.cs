namespace GTEK.FSM.Backend.Application.Subscriptions;

public sealed class SubscriptionUsersQueryResult
{
    private SubscriptionUsersQueryResult(bool isSuccess, string message, string? errorCode, int? statusCode, QueriedSubscriptionUsersPage? payload)
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

    public QueriedSubscriptionUsersPage? Payload { get; }

    public static SubscriptionUsersQueryResult Success(QueriedSubscriptionUsersPage payload)
    {
        return new SubscriptionUsersQueryResult(true, "Subscription users retrieved.", null, null, payload);
    }

    public static SubscriptionUsersQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new SubscriptionUsersQueryResult(false, message, errorCode, statusCode, null);
    }
}