using System.Text;
using k8s.Models;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Common.Secrets;

/// <summary>
/// Secret 值掩码纯函数集(契约见 secrets-page):编辑回显占位符、提交合并与二进制资格判定,
/// 不做任何 I/O 与 K8s 调用,由 <c>SecretService</c> 在读→归属判定→合并→替换的同一请求内使用。
/// </summary>
public static class SecretRedaction
{
    /// <summary>占位符前缀。</summary>
    public const string PlaceholderPrefix = "<REDACTED:";

    /// <summary>占位符后缀。</summary>
    public const string PlaceholderSuffix = ">";

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>生成指定 key 的占位符值(形如 <c>&lt;REDACTED:password&gt;</c>)。</summary>
    /// <param name="key">Secret 键名。</param>
    public static string Placeholder(string key) => $"{PlaceholderPrefix}{key}{PlaceholderSuffix}";

    /// <summary>判断给定值是否为占位符;null 不算占位符。</summary>
    /// <param name="value">待判断的值。</param>
    public static bool IsPlaceholder(string? value)
        => value is not null
            && value.StartsWith(PlaceholderPrefix, StringComparison.Ordinal)
            && value.EndsWith(PlaceholderSuffix, StringComparison.Ordinal);

    /// <summary>判断 base64 解码结果是否为有效 UTF-8 文本(仅文本键可「查看明文」,二进制键不提供揭示)。</summary>
    /// <param name="bytes">解码后的原始字节。</param>
    public static bool IsUtf8Text(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return true;
        }

        try
        {
            _ = StrictUtf8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>把服务器 Secret 投影为带回显占位符的编辑载体:data 全部键值替换为占位符(防明文/base64 回显),metadata/Type 原样承载。</summary>
    /// <param name="secret">服务器侧 Secret。</param>
    public static SecretYamlBody ApplyPlaceholders(V1Secret secret)
    {
        var body = new SecretYamlBody
        {
            ApiVersion = "v1",
            Kind = "Secret",
            Metadata = secret.Metadata ?? new V1ObjectMeta(),
            Type = secret.Type
        };

        if ((secret.Data?.Count ?? 0) > 0)
        {
            body.Data = secret.Data!.ToDictionary(kv => kv.Key, kv => Placeholder(kv.Key), StringComparer.Ordinal);
        }

        return body;
    }

    /// <summary>
    /// 合并提交的 YAML 与服务器现值:占位符保留服务器现值、data 新值按 base64 校验、stringData 新值按 UTF-8 明文编码;
    /// 服务器既有键未出现在提交(data ∪ stringData)中即删除。stringData 统一预编码进 data,替换体不再携带 stringData。
    /// 已知边界:用户真实新值恰为占位符字符串时按占位符处理(保留现值,fail-safe)。
    /// </summary>
    /// <param name="submitted">用户提交的 YAML 解析结果。</param>
    /// <param name="server">服务器侧当前 Secret(提供占位符对应的现值与既有键集合)。</param>
    public static Dictionary<string, byte[]> MergeSubmitted(SecretYamlBody submitted, V1Secret server)
    {
        var serverData = server.Data ?? new Dictionary<string, byte[]>();
        var final = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var (key, value) in submitted.Data ?? new Dictionary<string, string>())
        {
            final[key] = IsPlaceholder(value) ? TakeServerValue(serverData, key) : DecodeBase64(key, value);
        }

        foreach (var (key, value) in submitted.StringData ?? new Dictionary<string, string>())
        {
            final[key] = IsPlaceholder(value) ? TakeServerValue(serverData, key) : Encoding.UTF8.GetBytes(value);
        }

        return final;
    }

    private static byte[] TakeServerValue(IDictionary<string, byte[]> serverData, string key)
    {
        if (!serverData.TryGetValue(key, out var current))
        {
            throw new ValidationException($"「{key}」的占位符已失效，请刷新后重试");
        }

        return current;
    }

    private static byte[] DecodeBase64(string key, string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new ValidationException($"「{key}」的值不是有效的 base64 编码");
        }
    }
}
