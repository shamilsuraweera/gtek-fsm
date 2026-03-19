namespace GTEK.FSM.Backend.Application.Persistence.Specifications;

public sealed record PageSpecification(int PageNumber = 1, int PageSize = 50)
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public int NormalizedPageNumber => this.PageNumber < 1 ? 1 : this.PageNumber;

    public int NormalizedPageSize => this.PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => this.PageSize,
    };

    public int Skip => (this.NormalizedPageNumber - 1) * this.NormalizedPageSize;

    public int Take => this.NormalizedPageSize;
}
