using Microsoft.AspNetCore.Identity;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.ViewModels.Accounts;

namespace MultiClusterMgmtSys.ViewModels.Accounts.Mappings;

public static class AccountMappingExtensions
{
    public static AccountViewModel ToAccountViewModel(this ApplicationUser user, string roleName)
    {
        return new AccountViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            DisplayName = user.DisplayName,
            RoleName = roleName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
