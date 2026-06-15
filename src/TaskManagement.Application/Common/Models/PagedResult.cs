namespace TaskManagement.Application.Common.Models;

/// <summary>
/// Sayfalanmış (paged) liste sonuçları için generic wrapper.
/// Filtreleme endpoint'lerinin tümü bu tipi döner.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
