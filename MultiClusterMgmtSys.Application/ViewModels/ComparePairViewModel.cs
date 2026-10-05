namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 跨集群同名资源对照结果(一次性呈现两侧 YAML 与存在性判定)。
/// </summary>
public class ComparePairViewModel
{
    /// <summary>对照的命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>对照的资源名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>源集群显示名。</summary>
    public string SourceClusterName { get; set; } = "";

    /// <summary>对照(目标)集群显示名。</summary>
    public string TargetClusterName { get; set; } = "";

    /// <summary>源侧资源是否存在。</summary>
    public bool SourceExists { get; set; }

    /// <summary>源侧原始 YAML;不存在时为空串。</summary>
    public string SourceYaml { get; set; } = "";

    /// <summary>对照侧资源是否存在。</summary>
    public bool TargetExists { get; set; }

    /// <summary>对照侧原始 YAML;不存在时为空串。</summary>
    public string TargetYaml { get; set; } = "";

    /// <summary>两侧都存在且 YAML 有差异。</summary>
    public bool HasDifference { get; set; }

    /// <summary>差异行数(对照页渲染后写入;服务层不做行级计算)。</summary>
    public int DifferenceCount { get; set; }

    /// <summary>是否可一键克隆(源存在且有操作权限,目标缺失或两侧有差异)。</summary>
    public bool CanClone { get; set; }
}
