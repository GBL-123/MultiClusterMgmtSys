namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>舰队下发整批结果:逐集群结果行与成功/失败计数(结果汇总展示数据;口径与 Helm 批量下发一致)。</summary>
public class FleetTemplateDeployResultViewModel
{
    /// <summary>逐集群结果行,顺序与请求中的目标集群顺序一致。</summary>
    public List<FleetTemplateDeployItemViewModel> Items { get; set; } = [];

    /// <summary>成功集群数(新建 / 更新执行成功或一致跳过)。</summary>
    public int SuccessCount => Items.Count(item => item.Succeeded);

    /// <summary>失败集群数(执行失败或获取失败)。</summary>
    public int FailureCount => Items.Count(item => !item.Succeeded);
}
