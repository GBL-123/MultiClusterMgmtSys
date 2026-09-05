namespace MultiClusterMgmtSys.Data.Entities;

public class AppSetting
{
    public int Id { get; set; }

    public string Key { get; set; } = "";

    public string Value { get; set; } = "";

    public DateTime UpdatedAt { get; set; }
}
