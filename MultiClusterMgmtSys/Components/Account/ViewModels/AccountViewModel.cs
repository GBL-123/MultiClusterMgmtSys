namespace MultiClusterMgmtSys.Features.Account.ViewModels;

public class AccountViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string? DisplayName { get; set; }
    public string RoleName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
