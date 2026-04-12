namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Represents a worker candidate for assignment matching decisions.
/// </summary>
public sealed class AssignmentWorkerOption
{
    /// <summary>
    /// Gets or sets the worker identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the worker display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary skill tag.
    /// </summary>
    public string SkillTag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the active assignment count.
    /// </summary>
    public int ActiveAssignments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the worker has a known conflict.
    /// </summary>
    public bool HasConflict { get; set; }

    /// <summary>
    /// Gets or sets the conflict reason if one exists.
    /// </summary>
    public string? ConflictReason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the worker is currently available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;
}
