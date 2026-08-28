namespace MultiClusterMgmtSys.ViewModels;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];

    public int Total { get; set; } = 0;

    public PagedResult() { }

    public PagedResult(List<T> items, int total)
    {
        Items = items;
        Total = total;
    }
}