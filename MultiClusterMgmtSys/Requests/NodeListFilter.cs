namespace MultiClusterMgmtSys.Requests;

public class NodeListFilter
{
    public string Name { get; set; } = "";

    public string? Role { get; set; }

    public string? Status { get; set; }

    public bool? Schedulable { get; set; }

    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Name)
        || !string.IsNullOrEmpty(Role)
        || !string.IsNullOrEmpty(Status)
        || Schedulable.HasValue;

    public void Reset()
    {
        Name = "";
        Role = null;
        Status = null;
        Schedulable = null;
    }
}
