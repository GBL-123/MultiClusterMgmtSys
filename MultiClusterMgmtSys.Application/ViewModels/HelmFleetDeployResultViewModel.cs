namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>批量下发整批结果:逐集群结果行与成功/失败计数(对话框汇总态展示数据)。</summary>
public class HelmFleetDeployResultViewModel
{
    /// <summary>逐集群结果行,顺序与请求中的目标集群顺序一致。</summary>
    public List<HelmFleetDeployItemViewModel> Items { get; set; } = [];

    /// <summary>成功集群数。</summary>
    public int SuccessCount => Items.Count(item => item.Succeeded);

    /// <summary>失败集群数。</summary>
    public int FailureCount => Items.Count(item => !item.Succeeded);
}
