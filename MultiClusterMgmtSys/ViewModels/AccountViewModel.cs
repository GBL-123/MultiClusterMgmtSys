namespace MultiClusterMgmtSys.ViewModels;

public class AccountViewModel
{
    public int Id { get; set; }

    public string UserName { get; set; } = "";

    public string RoleName { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
