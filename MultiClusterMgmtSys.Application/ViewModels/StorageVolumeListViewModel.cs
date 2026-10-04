using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// PV 持久卷列表页展示数据(只读浏览)。
/// </summary>
public class StorageVolumeListViewModel
{
    /// <summary>持久卷名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>原始 Phase(Available/Bound/Released/Failed,未登记值原样保留)。</summary>
    public string Phase { get; set; } = "";

    /// <summary>Phase 中文展示名(契约见 display-conventions)。</summary>
    public string PhaseText => K8sDisplayText.PvPhaseText(Phase);

    /// <summary>Phase 对应的状态徽章 CSS 类。</summary>
    public string PhaseCssClass => K8sDisplayText.PvPhaseCssClass(Phase);

    /// <summary>容量,如 10Gi。</summary>
    public string Capacity { get; set; } = "";

    /// <summary>回收策略(Retain/Delete/Recycle)。</summary>
    public string ReclaimPolicy { get; set; } = "";

    /// <summary>绑定的持久卷声明(namespace/name),未绑定为 null。</summary>
    public string? BoundClaim { get; set; } = null;

    /// <summary>来源存储类名;空表示未指定。</summary>
    public string StorageClassName { get; set; } = "";

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;
}
