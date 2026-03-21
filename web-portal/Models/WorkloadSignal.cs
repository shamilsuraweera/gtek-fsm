// <copyright file="WorkloadSignal.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Workload prioritization signal combining request priority and age/urgency.
/// </summary>
public record WorkloadSignal(
    UrgencyLevel UrgencyLevel,
    int AgeMinutes,
    bool IsEscalated,
    bool IsSLABreach,
    string ContextHint);
