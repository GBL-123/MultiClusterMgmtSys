namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 排空预检中单个 Pod 的展示行:名称、命名空间与归属分类。
/// </summary>
public class DrainPodViewModel
{
    /// <summary>Pod 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>Pod 所在命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>直接属主类型(如 ReplicaSet、StatefulSet);裸 Pod 为空。</summary>
    public string? OwnerKind { get; set; }
}

/// <summary>
/// 节点排空预检结果:按迁移性质分类的 Pod 清单,驱动强确认对话框。
/// </summary>
public class NodeDrainPreflightViewModel
{
    /// <summary>节点名称。</summary>
    public string NodeName { get; set; } = "";

    /// <summary>控制器 Pod 清单(驱逐后由对应控制器在其它节点重建)。</summary>
    public List<DrainPodViewModel> MigratablePods { get; set; } = new();

    /// <summary>DaemonSet Pod 清单(随节点常驻,排空时跳过)。</summary>
    public List<DrainPodViewModel> DaemonSetPods { get; set; } = new();

    /// <summary>无控制器裸 Pod 清单(第一版默认不驱逐,驱逐后不会重建)。</summary>
    public List<DrainPodViewModel> BarePods { get; set; } = new();
}

/// <summary>
/// 节点排空的汇总结果:「成功 n / 跳过 m / 阻塞 k」与被阻塞 Pod 清单。
/// 语义为尽力迁移:被阻塞项不中断其余驱逐,系统不等待节点 Pod 清零。
/// </summary>
public class NodeDrainReportViewModel
{
    /// <summary>成功驱逐的控制器 Pod 数。</summary>
    public int Evicted { get; set; }

    /// <summary>跳过的 Pod 数(DaemonSet Pod 与未驱逐的裸 Pod)。</summary>
    public int Skipped { get; set; }

    /// <summary>被 PDB(429)阻塞的 Pod 数。</summary>
    public int Blocked { get; set; }

    /// <summary>被阻塞 Pod 清单(供汇总展示)。</summary>
    public List<DrainPodViewModel> BlockedPods { get; set; } = new();
}

/// <summary>
/// 排空执行中的进度回报(复用全量刷新的进度反馈模式):每迁移一个 Pod 回报一次。
/// </summary>
/// <param name="Completed">已处理的 Pod 数(含成功、阻塞与失败累计)。</param>
/// <param name="Total">本次将迁移的目标 Pod 总数。</param>
public record NodeDrainProgress(int Completed, int Total);
