using System.Text;
using k8s.Models;
using MultiClusterMgmtSys.Application.Common.Secrets;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Tests.Application.Common.Secrets;

public class SecretRedactionTests
{
    [Theory]
    [InlineData("password", "<REDACTED:password>")]
    [InlineData("tls.crt", "<REDACTED:tls.crt>")]
    public void Placeholder_wraps_key(string key, string expected)
    {
        Assert.Equal(expected, SecretRedaction.Placeholder(key));
    }

    [Theory]
    [InlineData("<REDACTED:password>", true)]
    [InlineData("<REDACTED:k8s.io/tls>", true)]
    [InlineData("<REDACTED:", false)]
    [InlineData("REDACTED:password>", false)]
    [InlineData("super-secret", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPlaceholder_matches_exact_shape(string? value, bool expected)
    {
        Assert.Equal(expected, SecretRedaction.IsPlaceholder(value));
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("", true)]
    [InlineData("中文明文", true)]
    [InlineData("\u00e9\u00e8 caf\u00e9", true)]
    public void IsUtf8Text_accepts_valid_utf8(string text, bool expected)
    {
        Assert.Equal(expected, SecretRedaction.IsUtf8Text(Encoding.UTF8.GetBytes(text)));
    }

    [Fact]
    public void IsUtf8Text_rejects_binary_bytes()
    {
        Assert.False(SecretRedaction.IsUtf8Text([0xFF, 0xFE, 0x00, 0xC3, 0x28]));
    }

    [Fact]
    public void ApplyPlaceholders_replaces_every_data_value_and_keeps_metadata()
    {
        var secret = new V1Secret
        {
            Metadata = new V1ObjectMeta { Name = "db-auth", NamespaceProperty = "prod" },
            Type = "Opaque",
            Data = new Dictionary<string, byte[]>
            {
                ["password"] = Encoding.UTF8.GetBytes("plaintext-pw"),
                ["tls.crt"] = [0xFF, 0xFE]
            }
        };

        var body = SecretRedaction.ApplyPlaceholders(secret);

        Assert.Equal("v1", body.ApiVersion);
        Assert.Equal("Secret", body.Kind);
        Assert.Equal("db-auth", body.Metadata!.Name);
        Assert.Equal("Opaque", body.Type);
        Assert.Null(body.StringData);
        Assert.NotNull(body.Data);
        Assert.Equal("<REDACTED:password>", body.Data!["password"]);
        Assert.Equal("<REDACTED:tls.crt>", body.Data["tls.crt"]);
    }

    [Fact]
    public void ApplyPlaceholders_without_data_leaves_data_null()
    {
        var body = SecretRedaction.ApplyPlaceholders(new V1Secret { Metadata = new V1ObjectMeta() });

        Assert.Null(body.Data);
    }

    [Fact]
    public void MergeSubmitted_keeps_server_value_for_placeholders()
    {
        var server = Server(password: "old-pw");
        var submitted = new SecretYamlBody
        {
            Metadata = new V1ObjectMeta { Name = "db-auth" },
            Data = new Dictionary<string, string> { ["password"] = "<REDACTED:password>" }
        };

        var merged = SecretRedaction.MergeSubmitted(submitted, server);

        Assert.Equal(Encoding.UTF8.GetBytes("old-pw"), merged["password"]);
    }

    [Fact]
    public void MergeSubmitted_applies_new_base64_value()
    {
        var server = Server(password: "old-pw");
        var submitted = new SecretYamlBody
        {
            Metadata = new V1ObjectMeta { Name = "db-auth" },
            Data = new Dictionary<string, string> { ["password"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("new-pw")) }
        };

        var merged = SecretRedaction.MergeSubmitted(submitted, server);

        Assert.Equal(Encoding.UTF8.GetBytes("new-pw"), merged["password"]);
    }

    [Fact]
    public void MergeSubmitted_applies_stringData_as_utf8()
    {
        var submitted = new SecretYamlBody
        {
            Metadata = new V1ObjectMeta { Name = "db-auth" },
            StringData = new Dictionary<string, string> { ["password"] = "plain-new-pw" }
        };

        var merged = SecretRedaction.MergeSubmitted(submitted, Server());

        Assert.Equal(Encoding.UTF8.GetBytes("plain-new-pw"), merged["password"]);
    }

    [Fact]
    public void MergeSubmitted_deletes_keys_absent_from_submission()
    {
        var server = Server(password: "old-pw", "legacy-key");
        var submitted = new SecretYamlBody
        {
            Metadata = new V1ObjectMeta { Name = "db-auth" },
            Data = new Dictionary<string, string> { ["password"] = "<REDACTED:password>" }
        };

        var merged = SecretRedaction.MergeSubmitted(submitted, server);

        Assert.Single(merged);
        Assert.Equal(Encoding.UTF8.GetBytes("old-pw"), merged["password"]);
    }

    [Fact]
    public void MergeSubmitted_rejects_placeholder_without_server_value()
    {
        var submitted = new SecretYamlBody
        {
            Metadata = new V1ObjectMeta { Name = "db-auth" },
            Data = new Dictionary<string, string> { ["ghost"] = "<REDACTED:ghost>" }
        };

        var ex = Assert.Throws<ValidationException>(() => SecretRedaction.MergeSubmitted(submitted, Server()));

        Assert.Equal("「ghost」的占位符已失效，请刷新后重试", ex.UserMessage);
    }

    [Fact]
    public void MergeSubmitted_rejects_invalid_base64()
    {
        var submitted = new SecretYamlBody
        {
            Metadata = new V1ObjectMeta { Name = "db-auth" },
            Data = new Dictionary<string, string> { ["password"] = "not-base64!!" }
        };

        var ex = Assert.Throws<ValidationException>(() => SecretRedaction.MergeSubmitted(submitted, Server()));

        Assert.Equal("「password」的值不是有效的 base64 编码", ex.UserMessage);
    }

    [Fact]
    public void MergeSubmitted_with_empty_submission_clears_all_data()
    {
        var server = Server(password: "old-pw");
        var submitted = new SecretYamlBody { Metadata = new V1ObjectMeta { Name = "db-auth" } };

        var merged = SecretRedaction.MergeSubmitted(submitted, server);

        Assert.Empty(merged);
    }

    private static V1Secret Server(string password = "seed", params string[] extraKeys)
    {
        var data = new Dictionary<string, byte[]> { ["password"] = Encoding.UTF8.GetBytes(password) };
        foreach (var key in extraKeys)
        {
            data[key] = Encoding.UTF8.GetBytes($"{key}-value");
        }

        return new V1Secret
        {
            Metadata = new V1ObjectMeta { Name = "db-auth", NamespaceProperty = "prod" },
            Type = "Opaque",
            Data = data
        };
    }
}
