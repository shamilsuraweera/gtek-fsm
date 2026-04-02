namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

public sealed class GetWorkersRequest
{
    public string? SearchText { get; set; }

    public bool? IncludeInactive { get; set; }

    public int? Page { get; set; }

    public int? PageSize { get; set; }
}
