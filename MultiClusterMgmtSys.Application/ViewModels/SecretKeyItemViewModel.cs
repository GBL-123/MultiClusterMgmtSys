namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// Secret 详情页的单键条目:data 中 base64 解码后的展示形态。
/// </summary>
public class SecretKeyItemViewModel
{
    /// <summary>键名。</summary>
    public string Key { get; set; } = "";

    /// <summary>解码后的字节数。</summary>
    public int ByteCount { get; set; }

    /// <summary>是否为有效 UTF-8 文本(仅文本键可「查看明文」,二进制键展示 base64 原文)。</summary>
    public bool IsText { get; set; }

    /// <summary>base64 原文;仅二进制键填充,文本键恒为空串(避免绕过掩码)。</summary>
    public string Base64 { get; set; } = "";

    /// <summary>揭示后的明文;null = 尚未揭示(掩码态)。</summary>
    public string? Value { get; set; } = null;
}
