namespace MultiClusterMgmtSys.ViewModels;

public class SvcDetailViewModel
{
    public string Name { get; set; } = "";

    public string Namespace { get; set; } = "";

    public string Uid { get; set; } = "";

    public DateTime? CreatedAt { get; set; } = null;

    public string Type { get; set; } = "";

    public string ClusterIP { get; set; } = "";

    public bool Headless { get; set; }

    public string ExternalEntry { get; set; } = "";

    public string? ExternalName { get; set; } = null;

    public Dictionary<string, string> Selector { get; set; } = new();

    public List<SvcPortViewModel> Ports { get; set; } = new();

    public List<SvcEndpointViewModel> Endpoints { get; set; } = new();

    public bool EndpointsLoadFailed { get; set; }

    public string Yaml { get; set; } = "";
}
