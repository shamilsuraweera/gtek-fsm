namespace GTEK.FSM.Backend.Domain.Enums;

/// <summary>
/// Categorization of feedback types for service quality tracking.
/// </summary>
public enum FeedbackType
{
    /// <summary>Feedback about overall quality of the completed service.</summary>
    ServiceQuality = 0,

    /// <summary>Feedback about the assigned worker's behavior and professionalism.</summary>
    WorkerBehavior = 1,

    /// <summary>Feedback about response time and SLA adherence.</summary>
    ResponseTimeliness = 2,

    /// <summary>Feedback about professionalism and communication.</summary>
    Communication = 3,

    /// <summary>Feedback about technical skill and competence demonstrated.</summary>
    TechnicalCompetence = 4,

    /// <summary>General feedback not fitting other categories.</summary>
    Other = 5
}
