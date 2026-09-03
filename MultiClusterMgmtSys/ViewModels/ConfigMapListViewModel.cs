namespace MultiClusterMgmtSys.ViewModels;

public class ConfigMapListViewModel
{
    public string Name { get; set; } = "";

    public string Namespace { get; set; } = "";

    public int DataKeyCount { get; set; } = 0;

    public string DataKeyPreview { get; set; } = "";

    public DateTime? CreatedAt { get; set; } = null;
}
