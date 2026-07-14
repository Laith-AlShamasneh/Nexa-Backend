namespace Shared.Responses;

public sealed record PagedResponse<T>
{
    public IReadOnlyList<T> Items      { get; init; } = [];
    public int              TotalCount { get; init; }
    public int              PageNumber { get; init; }
    public int              PageSize   { get; init; }

    public PagedResponse(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize)
    {
        this.Items      = Items;
        this.TotalCount = TotalCount;
        this.PageNumber = PageNumber;
        this.PageSize   = PageSize;
    }
}
