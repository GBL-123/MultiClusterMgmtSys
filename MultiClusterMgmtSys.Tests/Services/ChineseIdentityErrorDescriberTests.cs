using Microsoft.AspNetCore.Identity;
using MultiClusterMgmtSys.Services.Identity;

namespace MultiClusterMgmtSys.Tests.Services;

public class ChineseIdentityErrorDescriberTests
{
    private readonly ChineseIdentityErrorDescriber describer = new();

    [Fact]
    public void DuplicateUserName_is_chinese()
    {
        var error = describer.DuplicateUserName("admin");

        Assert.Equal("DuplicateUserName", error.Code);
        Assert.Contains("admin", error.Description);
        Assert.Contains("已被使用", error.Description);
    }

    [Theory]
    [InlineData(4)]
    public void PasswordTooShort_includes_length(int length)
    {
        var error = describer.PasswordTooShort(length);

        Assert.Equal("PasswordTooShort", error.Code);
        Assert.Contains($"{length}", error.Description);
        Assert.Contains("密码长度", error.Description);
    }

    [Fact]
    public void Password_rules_are_chinese()
    {
        Assert.Contains("特殊字符", describer.PasswordRequiresNonAlphanumeric().Description);
        Assert.Contains("数字", describer.PasswordRequiresDigit().Description);
        Assert.Contains("小写字母", describer.PasswordRequiresLower().Description);
        Assert.Contains("大写字母", describer.PasswordRequiresUpper().Description);
        Assert.Contains("不同字符", describer.PasswordRequiresUniqueChars(3).Description);
    }
}
