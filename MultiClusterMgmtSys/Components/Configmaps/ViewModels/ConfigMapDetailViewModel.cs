namespace MultiClusterMgmtSys.Features.Configmaps.ViewModels;

public class ConfigMapDetailViewModel
{
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Uid { get; set; } = "";
    public DateTime? CreatedAt { get; set; } = null;
    public List<ConfigMapDataEntryViewModel> Data { get; set; } = new();
    public string Yaml { get; set; } = "";
}
