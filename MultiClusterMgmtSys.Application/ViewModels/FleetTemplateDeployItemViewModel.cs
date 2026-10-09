using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>舰队下发在单个目标集群上的执行结果(结果行展示数据;口径与 Helm 批量下发一致)。</summary>
public class FleetTemplateDeployItemViewModel
{
    /// <summary>目标集群 Id。</summary>
    public int ClusterId { get; set; }

    /// <summary>目标集群展示名。</summary>
    public string ClusterName { get; set; } = "";

    /// <summary>实际执行的动作(新建 / 更新 / 一致跳过 / 获取失败)。</summary>
    public FleetTemplateAction Action { get; set; }

    /// <summary>是否成功(新建 / 更新执行成功或一致跳过视为成功;获取失败与执行失败为否)。</summary>
    public bool Succeeded { get; set; }

    /// <summary>中文结果消息;成功时为空,失败时为可直接展示的原因。</summary>
    public string Message { get; set; } = "";
}
