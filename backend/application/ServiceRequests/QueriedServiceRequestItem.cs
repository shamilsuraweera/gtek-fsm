//-----------------------------------------------------------------------
// <copyright file="QueriedServiceRequestItem.cs" company="GTEK">
// Copyright (c) 2026 GTEK. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents a single service request item projected for list responses.
/// </summary>
/// <param name="RequestId">The unique service request identifier.</param>
/// <param name="Status">The current request status.</param>
/// <param name="Summary">The request title or summary text.</param>
/// <param name="TenantId">The tenant identifier that owns the request.</param>
/// <param name="CustomerUserId">The customer user who created the request.</param>
/// <param name="CreatedAtUtc">The request creation timestamp in UTC.</param>
/// <param name="UpdatedAtUtc">The most recent update timestamp in UTC.</param>
/// <param name="ActiveJobId">The active linked job identifier, when present.</param>
/// <param name="AssignedWorkerUserId">The currently assigned worker identifier, when present.</param>
public sealed record QueriedServiceRequestItem(
    Guid RequestId,
    string Status,
    string Summary,
    Guid TenantId,
    Guid CustomerUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    Guid? ActiveJobId,
    Guid? AssignedWorkerUserId);
