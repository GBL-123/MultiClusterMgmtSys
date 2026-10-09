namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>舰队模板预览整批结果:逐集群预览行(确认下发前的展示数据)。</summary>
public class FleetTemplatePreviewResultViewModel
{
    /// <summary>逐集群预览行,顺序与请求中的目标集群顺序一致。</summary>
    public List<FleetTemplatePreviewItemViewModel> Items { get; set; } = [];
}
