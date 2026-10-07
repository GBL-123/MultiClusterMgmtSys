using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>批量下发在单个目标集群上的执行结果(结果行展示数据)。</summary>
public class HelmFleetDeployItemViewModel
{
    /// <summary>目标集群 Id。</summary>
    public int ClusterId { get; set; }

    /// <summary>目标集群展示名。</summary>
    public string ClusterName { get; set; } = "";

    /// <summary>实际执行的动作(安装或升级)。</summary>
    public HelmFleetDeployAction Action { get; set; }

    /// <summary>是否成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>中文结果消息;成功时为空,失败时为可直接展示的原因。</summary>
    public string Message { get; set; } = "";
}
