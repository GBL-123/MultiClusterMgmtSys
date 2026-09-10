namespace MultiClusterMgmtSys.Data.Entities;

/// <summary>
/// 应用设置:以键值对形式持久化的运行时配置,按键唯一;
/// 由 AppSettingRepository 提供读取与「存在即更新」式写入。
/// </summary>
public class AppSetting
{
    /// <summary>自增主键。</summary>
    public int Id { get; set; }

    /// <summary>设置键,必填且唯一,最长 128。</summary>
    public string Key { get; set; } = "";

    /// <summary>设置值,必填,最长 256。</summary>
    public string Value { get; set; } = "";

    /// <summary>最近一次写入时间(UTC)。</summary>
    public DateTime UpdatedAt { get; set; }
}
