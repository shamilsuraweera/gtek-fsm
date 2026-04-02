namespace GTEK.FSM.WebPortal.Models;

public sealed class QueueFilterState
{
    public string SearchText { get; set; } = string.Empty;

    public string StageFilter { get; set; } = "All";

    public string StatusFilter { get; set; } = string.Empty;

    public string UrgencyFilter { get; set; } = string.Empty;

    public bool IsDefault =>
        string.IsNullOrWhiteSpace(this.SearchText)
        && string.Equals(this.StageFilter, "All", StringComparison.OrdinalIgnoreCase)
        && string.IsNullOrWhiteSpace(this.StatusFilter)
        && string.IsNullOrWhiteSpace(this.UrgencyFilter);

    public QueueFilterState Clone()
    {
        return new QueueFilterState
        {
            SearchText = this.SearchText,
            StageFilter = this.StageFilter,
            StatusFilter = this.StatusFilter,
            UrgencyFilter = this.UrgencyFilter,
        };
    }
}
