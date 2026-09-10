namespace MultiClusterMgmtSys.ViewModels;

public class SvcListViewModel
{
    public string Name { get; set; } = "";

    public string Namespace { get; set; } = "";

    public string Type { get; set; } = "";

    public string ClusterIP { get; set; } = "";

    public bool Headless { get; set; }

    public List<SvcPortViewModel> Ports { get; set; } = new();

    public string ExternalEntry { get; set; } = "";

    public DateTime? CreatedAt { get; set; } = null;
}
