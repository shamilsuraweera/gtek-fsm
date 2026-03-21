// <copyright file="RequestStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Request lifecycle status with presentation and behavior hints.
/// </summary>
public enum RequestStatus
{
    /// <summary>New request awaiting initial triage.</summary>
    New = 0,

    /// <summary>Request is being assessed for scope and effort.</summary>
    Assessing = 1,

    /// <summary>Request is assigned and awaiting worker start.</summary>
    Assigned = 2,

    /// <summary>Request is actively being worked on.</summary>
    Active = 3,

    /// <summary>Request is waiting for customer information or feedback.</summary>
    Waiting = 4,

    /// <summary>Request has been completed successfully.</summary>
    Completed = 5,

    /// <summary>Request was rejected or cancelled.</summary>
    Cancelled = 6,

    /// <summary>Request is escalated due to complexity or urgency.</summary>
    Escalated = 7,

    /// <summary>Request is held pending further action.</summary>
    OnHold = 8,
}
