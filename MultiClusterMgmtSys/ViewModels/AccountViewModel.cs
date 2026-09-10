namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 账号管理页展示数据。
/// </summary>
public class AccountViewModel
{
    /// <summary>用户主键。</summary>
    public int Id { get; set; }

    /// <summary>登录名。</summary>
    public string UserName { get; set; } = "";

    /// <summary>角色名(Admin/Member)。</summary>
    public string RoleName { get; set; } = "";

    /// <summary>账号创建时间。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>最近更新时间;从未更新为 null。</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>最近登录时间;从未登录为 null。</summary>
    public DateTime? LastLoginAt { get; set; }
}
