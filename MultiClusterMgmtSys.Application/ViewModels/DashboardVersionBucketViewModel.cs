namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 看板版本分布桶:一个 Kubernetes 版本下的集群数;未探测到版本的集群单独成桶。
/// </summary>
public class DashboardVersionBucketViewModel
{
    /// <summary>版本号原值;未探测到版本时为空。</summary>
    public string? Version { get; set; }

    /// <summary>展示标签:有版本时为版本号原值,未探测时为「未探测」。</summary>
    public string Label { get; set; } = "";

    /// <summary>该版本下的集群数。</summary>
    public int Count { get; set; }
}
