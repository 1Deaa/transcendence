namespace HrmSystem.Application.Common.Models;

/*
    //?     Offset-pagination envelope (DevHabit pattern) returned by every list endpoint.
    //?     Carries the items plus everything a client needs to render a pager.
    //
    //!     Built via Create() from a repository's (Items, TotalCount) tuple — the Application
    //!     layer never touches IQueryable, so this type has no EF Core dependency.
*/
public sealed record PaginationResult<TItem>(
    IReadOnlyList<TItem> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public static PaginationResult<TItem> Create(
        IReadOnlyList<TItem> items,
        int page,
        int pageSize,
        int totalCount
    ) => new(items, page, pageSize, totalCount);
}
