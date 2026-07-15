using Microsoft.AspNetCore.Identity;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Services.Identity;

public class AlphanumericPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooWeak",
                Description = "密码至少 8 位且包含字母和数字"
            }));
        }

        var hasLetter = false;
        var hasDigit = false;
        foreach (var c in password)
        {
            if (char.IsLetter(c)) hasLetter = true;
            else if (char.IsDigit(c)) hasDigit = true;
            if (hasLetter && hasDigit) break;
        }

        if (!hasLetter || !hasDigit)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooWeak",
                Description = "密码至少 8 位且包含字母和数字"
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
