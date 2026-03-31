//-----------------------------------------------------------------------
// <copyright file="ServiceRequestQueryResult.cs" company="GTEK">
// Copyright (c) 2026 GTEK. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents the query operation outcome for service request list retrieval.
/// </summary>
public sealed class ServiceRequestQueryResult
{
    private ServiceRequestQueryResult(
        bool isSuccess,
        string message,
        string? errorCode,
        int? statusCode,
        QueriedServiceRequestPage? payload)
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
    /// Gets the HTTP-compatible status code to surface to API callers when the query fails.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the paginated query payload when the query succeeds.
    /// </summary>
    public QueriedServiceRequestPage? Payload { get; }

    /// <summary>
    /// Creates a successful query result with payload data.
    /// </summary>
    /// <param name="payload">The paginated service request payload.</param>
    /// <returns>A successful query result instance.</returns>
    public static ServiceRequestQueryResult Success(QueriedServiceRequestPage payload)
    {
        return new ServiceRequestQueryResult(
            isSuccess: true,
            message: "Service requests retrieved.",
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
    public static ServiceRequestQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new ServiceRequestQueryResult(
            isSuccess: false,
            message: message,
            errorCode: errorCode,
            statusCode: statusCode,
            payload: null);
    }
}
