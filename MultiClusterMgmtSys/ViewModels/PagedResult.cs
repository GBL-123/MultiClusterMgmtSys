namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 通用分页结果包装,服务层返回列表数据的标准输出载体。
/// </summary>
public class PagedResult<T>
{
    /// <summary>当前页数据项。</summary>
    public List<T> Items { get; set; } = [];

    /// <summary>全部数据项总数(跨页合计,非当前页条数)。</summary>
    public int Total { get; set; } = 0;

    /// <summary>无参构造,供反序列化或先建后填使用。</summary>
    public PagedResult() { }

    /// <summary>用指定数据项与总数构造分页结果。</summary>
    /// <param name="items">当前页数据项。</param>
    /// <param name="total">全部数据项总数。</param>
    public PagedResult(List<T> items, int total)
    {
        Items = items;
        Total = total;
    }
}