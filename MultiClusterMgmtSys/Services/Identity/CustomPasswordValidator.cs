using Microsoft.AspNetCore.Identity;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Services.Identity;

public class CustomPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordIsInvalid",
                Description = "密码不能为空"
            }));
        }

        if (!password.Any(char.IsLetterOrDigit))
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
