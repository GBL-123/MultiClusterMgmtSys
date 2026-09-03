namespace MultiClusterMgmtSys.ViewModels;

public class ConfigMapDetailViewModel
{
    public string Name { get; set; } = "";

    public string Namespace { get; set; } = "";

    public string Uid { get; set; } = "";

    public DateTime? CreatedAt { get; set; } = null;

    public Dictionary<string, string> Data { get; set; } = new();

    public string Yaml { get; set; } = "";
}
