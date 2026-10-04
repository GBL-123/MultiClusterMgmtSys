using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// PVC 持久卷声明详情页展示数据。
/// </summary>
public class StorageClaimDetailViewModel
{
    /// <summary>持久卷声明名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所在的 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>对象 UID。</summary>
    public string Uid { get; set; } = "";

    /// <summary>原始 Phase(Bound/Pending/Lost,未登记值原样保留)。</summary>
    public string Phase { get; set; } = "";

    /// <summary>Phase 中文展示名(契约见 display-conventions)。</summary>
    public string PhaseText => K8sDisplayText.PvcPhaseText(Phase);

    /// <summary>Phase 对应的状态徽章 CSS 类。</summary>
    public string PhaseCssClass => K8sDisplayText.PvcPhaseCssClass(Phase);

    /// <summary>请求容量,如 5Gi。</summary>
    public string Capacity { get; set; } = "";

    /// <summary>访问模式(ReadWriteOnce/ReadOnlyMany/ReadWriteMany 等,原始字符串)。</summary>
    public List<string> AccessModes { get; set; } = [];

    /// <summary>目标存储类;空表示未指定。</summary>
    public string StorageClass { get; set; } = "";

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;

    /// <summary>只读 YAML 原文(kubectl 等价视图;详情页不提供编辑入口)。</summary>
    public string Yaml { get; set; } = "";

    /// <summary>当前用户是否可操作该资源(Admin 或创建者本人,fail-closed;契约见 k8s-resource-ownership)。</summary>
    public bool CanOperate { get; set; }

    /// <summary>挂载此持久卷声明的 Pod(加载失败降级为空表,契约见 storage-management)。</summary>
    public List<StorageMountedPodViewModel> MountedPods { get; set; } = [];
}
