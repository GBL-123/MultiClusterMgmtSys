using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>舰队模板预览在单个目标集群上的判定结果(预览页行数据)。</summary>
public class FleetTemplatePreviewItemViewModel
{
    /// <summary>目标集群 Id。</summary>
    public int ClusterId { get; set; }

    /// <summary>目标集群展示名。</summary>
    public string ClusterName { get; set; } = "";

    /// <summary>判定结果(新建 / 更新 / 一致 / 获取失败)。</summary>
    public FleetTemplateAction Action { get; set; }

    /// <summary>该集群对象现值(剥离服务器侧字段后);对象不存在时为 null。</summary>
    public string? CurrentYaml { get; set; }

    /// <summary>渲染后 YAML(与下发一致,剥离服务器侧字段口径)。</summary>
    public string RenderedYaml { get; set; } = "";

    /// <summary>中文说明;获取失败时为失败原因,其余场景为空。</summary>
    public string Message { get; set; } = "";
}
