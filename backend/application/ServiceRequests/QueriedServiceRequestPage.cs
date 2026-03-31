//-----------------------------------------------------------------------
// <copyright file="QueriedServiceRequestPage.cs" company="GTEK">
// Copyright (c) 2026 GTEK. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents a paginated service request query result page.
/// </summary>
/// <param name="Items">The service request items in the current page.</param>
/// <param name="Page">The requested page number.</param>
/// <param name="PageSize">The requested page size.</param>
/// <param name="Total">The total number of matching items across all pages.</param>
public sealed record QueriedServiceRequestPage(
    IReadOnlyList<QueriedServiceRequestItem> Items,
    int Page,
    int PageSize,
    int Total);
