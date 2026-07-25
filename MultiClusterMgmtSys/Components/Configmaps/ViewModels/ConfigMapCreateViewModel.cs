namespace MultiClusterMgmtSys.Features.Configmaps.ViewModels;

public class ConfigMapCreateViewModel
{
    public int ClusterId { get; set; }
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public List<ConfigMapDataEntryViewModel> DataEntries { get; set; } = new();
}
