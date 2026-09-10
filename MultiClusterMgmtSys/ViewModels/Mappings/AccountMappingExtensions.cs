using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

/// <summary>
/// 账号实体 → 账号展示模型的映射。
/// </summary>
public static class AccountMappingExtensions
{
    /// <summary>账号实体 → 账号展示 ViewModel 映射(角色名由调用方传入)。</summary>
    public static AccountViewModel ToAccountViewModel(this ApplicationUser user, string roleName)
    {
        return new AccountViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            RoleName = roleName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
