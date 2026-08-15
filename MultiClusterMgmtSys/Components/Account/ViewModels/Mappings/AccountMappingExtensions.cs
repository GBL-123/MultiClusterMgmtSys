using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Features.Account.ViewModels;

namespace MultiClusterMgmtSys.Features.Account.ViewModels.Mappings;

public static class AccountMappingExtensions
{
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
