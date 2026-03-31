namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents the query operation outcome for job list retrieval.
/// </summary>
public sealed class JobQueryResult
{
    private JobQueryResult(
        bool isSuccess,
        string message,
        string? errorCode,
        int? statusCode,
        QueriedJobPage? payload)
    {
        this.IsSuccess = isSuccess;
        this.Message = message;
        this.ErrorCode = errorCode;
        this.StatusCode = statusCode;
        this.Payload = payload;
    }

    /// <summary>
    /// Gets a value indicating whether the query succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a human-readable operation message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets an optional machine-readable error code when the query fails.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the HTTP-compatible status code for failure cases.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the paginated query payload when the query succeeds.
    /// </summary>
    public QueriedJobPage? Payload { get; }

    /// <summary>
    /// Creates a successful query result with payload data.
    /// </summary>
    /// <param name="payload">The paginated job payload.</param>
    /// <returns>A successful query result instance.</returns>
    public static JobQueryResult Success(QueriedJobPage payload)
    {
        return new JobQueryResult(
            isSuccess: true,
            message: "Jobs retrieved.",
            errorCode: null,
            statusCode: null,
            payload: payload);
    }

    /// <summary>
    /// Creates a failed query result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="statusCode">The HTTP-compatible status code.</param>
    /// <returns>A failed query result instance.</returns>
    public static JobQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new JobQueryResult(
            isSuccess: false,
            message: message,
            errorCode: errorCode,
            statusCode: statusCode,
            payload: null);
    }
}
