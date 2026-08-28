namespace MultiClusterMgmtSys.Requests;

public class AccountQueryRequest
{
    public string? SearchName { get; set; }

    public string? RoleFilter { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public bool SortDescending { get; set; } = true;
}
