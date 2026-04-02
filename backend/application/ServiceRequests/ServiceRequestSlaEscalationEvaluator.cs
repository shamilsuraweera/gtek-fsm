using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Evaluates SLA snapshots and emits actionable escalation transitions.
/// </summary>
internal static class ServiceRequestSlaEscalationEvaluator
{
    /// <summary>
    /// Produces escalation transitions when a dimension moves upward into AtRisk or Breached.
    /// </summary>
    /// <param name="previousResponse">Previously persisted response SLA state.</param>
    /// <param name="previousAssignment">Previously persisted assignment SLA state.</param>
    /// <param name="previousCompletion">Previously persisted completion SLA state.</param>
    /// <param name="snapshot">Freshly computed SLA snapshot.</param>
    /// <returns>Deterministically ordered escalation transitions for response, assignment, and completion dimensions.</returns>
    public static IReadOnlyList<SlaEscalationTransition> Evaluate(
        SlaState previousResponse,
        SlaState previousAssignment,
        SlaState previousCompletion,
        ServiceRequestSlaSnapshot snapshot)
    {
        var escalations = new List<SlaEscalationTransition>(capacity: 3);

        AddTransitionIfEscalated(escalations, "Response", previousResponse, snapshot.ResponseSlaState, snapshot.ResponseDueAtUtc);
        AddTransitionIfEscalated(escalations, "Assignment", previousAssignment, snapshot.AssignmentSlaState, snapshot.AssignmentDueAtUtc);
        AddTransitionIfEscalated(escalations, "Completion", previousCompletion, snapshot.CompletionSlaState, snapshot.CompletionDueAtUtc);

        return escalations;
    }

    private static void AddTransitionIfEscalated(
        ICollection<SlaEscalationTransition> escalations,
        string slaDimension,
        SlaState previousState,
        SlaState currentState,
        DateTime? dueAtUtc)
    {
        // Emit only upward transitions into actionable states.
        if (currentState >= SlaState.AtRisk && currentState > previousState)
        {
            escalations.Add(new SlaEscalationTransition(slaDimension, previousState, currentState, dueAtUtc));
        }
    }
}
