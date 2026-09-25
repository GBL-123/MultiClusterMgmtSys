using Microsoft.AspNetCore.Identity;
using MultiClusterMgmtSys.Application.Services.Identity;

namespace MultiClusterMgmtSys.Tests.Application.Services.Identity;

public class ChineseIdentityErrorDescriberTests
{
    private readonly ChineseIdentityErrorDescriber _describer = new();

    [Fact]
    public void DuplicateUserName_is_chinese()
    {
        var error = _describer.DuplicateUserName("admin");

        Assert.Equal("DuplicateUserName", error.Code);
        Assert.Contains("admin", error.Description);
        Assert.Contains("已被使用", error.Description);
    }

    [Theory]
    [InlineData(4)]
    public void PasswordTooShort_includes_length(int length)
    {
        var error = _describer.PasswordTooShort(length);

        Assert.Equal("PasswordTooShort", error.Code);
        Assert.Contains($"{length}", error.Description);
        Assert.Contains("密码长度", error.Description);
    }

    [Fact]
    public void Password_rules_are_chinese()
    {
        Assert.Contains("特殊字符", _describer.PasswordRequiresNonAlphanumeric().Description);
        Assert.Contains("数字", _describer.PasswordRequiresDigit().Description);
        Assert.Contains("小写字母", _describer.PasswordRequiresLower().Description);
        Assert.Contains("大写字母", _describer.PasswordRequiresUpper().Description);
        Assert.Contains("不同字符", _describer.PasswordRequiresUniqueChars(3).Description);
    }
}
