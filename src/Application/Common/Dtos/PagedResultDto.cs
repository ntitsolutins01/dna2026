namespace DnaBrasilApi.Application.Common.Dtos;

public sealed class PagedResult<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public List<T> Items { get; init; } = new();
}
