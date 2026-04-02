//-----------------------------------------------------------------------
// <copyright file="OperationalQueueItem.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using GTEK.FSM.Shared.Contracts.Vocabulary;

namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Enhanced queue item with operational state, status, and signals.
/// </summary>
public class OperationalQueueItem
{
    /// <summary>
    /// Gets or sets the stable backend request identifier.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue list item reference.
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Customer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant context for this request.
    /// </summary>
    public string TenantId { get; set; } = "TENANT-01";

    /// <summary>
    /// Gets or sets the request stage.
    /// </summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request priority level.
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last update time in UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the current lifecycle status of the request.
    /// </summary>
    public RequestStage Status { get; set; } = RequestStage.New;

    /// <summary>
    /// Gets or sets the urgency level based on priority, age, and SLA.
    /// </summary>
    public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Normal;

    /// <summary>
    /// Gets or sets the number of minutes since the request was created.
    /// </summary>
    public int AgeMinutes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this request has been escalated.
    /// </summary>
    public bool IsEscalated { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether this request is in SLA breach.
    /// </summary>
    public bool IsSLABreach { get; set; } = false;

    /// <summary>
    /// Gets or sets the optional assigned worker name.
    /// </summary>
    public string? AssignedWorker { get; set; }

    /// <summary>
    /// Gets or sets the optional assigned worker user identifier.
    /// </summary>
    public string? AssignedWorkerId { get; set; }

    /// <summary>
    /// Gets or sets the estimate of time to resolution in minutes (if available).
    /// </summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>
    /// Gets or sets the user-facing hint about workload/urgency context.
    /// </summary>
    public string WorkloadHint { get; set; } = string.Empty;

    /// <summary>
    /// Gets the available actions for this queue item based on its status.
    /// </summary>
    public IReadOnlyList<TriageAction> AvailableActions => this.Status switch
    {
        RequestStage.New => new[] { TriageAction.Assign, TriageAction.Escalate, TriageAction.RequestInfo },
        RequestStage.Assigned => new[] { TriageAction.Reassign, TriageAction.Complete, TriageAction.Hold, TriageAction.ViewDetails },
        RequestStage.InProgress => new[] { TriageAction.Complete, TriageAction.RequestInfo, TriageAction.Hold, TriageAction.ViewDetails },
        RequestStage.OnHold => new[] { TriageAction.Reassign, TriageAction.Escalate, TriageAction.Reopen, TriageAction.Reject, TriageAction.ViewDetails },
        RequestStage.Completed => new[] { TriageAction.Reopen, TriageAction.AddNote, TriageAction.ViewDetails },
        RequestStage.Cancelled => new[] { TriageAction.Reopen, TriageAction.ViewDetails },
        _ => Array.Empty<TriageAction>(),
    };
}
