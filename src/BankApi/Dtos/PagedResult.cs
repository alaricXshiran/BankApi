namespace BankApi.Dtos;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    // Divide as decimals (double), then round UP, so a partly full last page still counts.
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}