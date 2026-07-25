namespace MultiClusterMgmtSys.Features.Clusters.ViewModels;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }

    public PagedResult() { }

    public PagedResult(List<T> items, int total)
    {
        Items = items;
        Total = total;
    }
}