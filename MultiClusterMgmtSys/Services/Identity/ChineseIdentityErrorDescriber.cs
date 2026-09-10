using Microsoft.AspNetCore.Identity;

namespace MultiClusterMgmtSys.Services.Identity;

/// <summary>
/// 将 Identity 校验错误文案本地化为中文的错误描述器,替换框架默认英文提示。
/// </summary>
public class ChineseIdentityErrorDescriber : IdentityErrorDescriber
{
    /// <summary>用户名已被占用的中文提示文案。</summary>
    public override IdentityError DuplicateUserName(string userName)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateUserName),
            Description = $"用户名“{userName}”已被使用"
        };
    }

    /// <summary>密码长度不足的中文提示文案(含最小长度要求)。</summary>
    public override IdentityError PasswordTooShort(int length)
    {
        return new IdentityError
        {
            Code = nameof(PasswordTooShort),
            Description = $"密码长度不能少于 {length} 位"
        };
    }

    /// <summary>缺少特殊字符的中文提示文案。</summary>
    public override IdentityError PasswordRequiresNonAlphanumeric()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = "密码必须包含至少一个特殊字符"
        };
    }

    /// <summary>缺少数字的中文提示文案。</summary>
    public override IdentityError PasswordRequiresDigit()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresDigit),
            Description = "密码必须包含至少一个数字"
        };
    }

    /// <summary>缺少小写字母的中文提示文案。</summary>
    public override IdentityError PasswordRequiresLower()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresLower),
            Description = "密码必须包含至少一个小写字母"
        };
    }

    /// <summary>缺少大写字母的中文提示文案。</summary>
    public override IdentityError PasswordRequiresUpper()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresUpper),
            Description = "密码必须包含至少一个大写字母"
        };
    }

    /// <summary>不同字符数不足的中文提示文案(含要求个数)。</summary>
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresUniqueChars),
            Description = $"密码至少需要包含 {uniqueChars} 个不同字符"
        };
    }
}
