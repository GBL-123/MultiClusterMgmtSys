namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 节点资源容量行展示数据:容量与可分配的格式化文本、原始 quantity 与可分配占比。
/// </summary>
public class NodeResourceViewModel
{
    /// <summary>资源键(原始 K8s 名称,如 cpu、memory、example.com/fpga)。</summary>
    public string Key { get; set; } = "";

    /// <summary>中文展示名;未知资源与 <see cref="Key"/> 相同。</summary>
    public string Label { get; set; } = "";

    /// <summary>容量原始 quantity 字符串;资源仅出现在可分配侧时为 null。</summary>
    public string? CapacityRaw { get; set; }

    /// <summary>容量的人类可读文本;缺失时为「—」,无法换算时回退原始串。</summary>
    public string CapacityText { get; set; } = "";

    /// <summary>可分配原始 quantity 字符串;资源仅出现在容量侧时为 null。</summary>
    public string? AllocatableRaw { get; set; }

    /// <summary>可分配的人类可读文本;缺失时为「—」,无法换算时回退原始串。</summary>
    public string AllocatableText { get; set; } = "";

    /// <summary>可分配占容量的百分比(0-100,保留一位小数);容量缺失或为 0 时为 null。</summary>
    public double? AllocatablePercent { get; set; }
}
