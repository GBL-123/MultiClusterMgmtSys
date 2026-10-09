namespace MultiClusterMgmtSys.Application.Enums;

/// <summary>
/// 舰队模板单集群的判定/执行状态:预览为四态判定(新建 / 更新 / 一致 / 获取失败),
/// 下发执行复用同一枚举表达实际动作或跳过原因(由 <see cref="MultiClusterMgmtSys.Application.Services.FleetTemplateService"/> 消费)。
/// </summary>
public enum FleetTemplateAction
{
    /// <summary>目标对象不存在,将走既有创建路径新建。</summary>
    Create = 0,

    /// <summary>目标对象存在且与渲染值存在差异,将走既有编辑路径全量替换。</summary>
    Update = 1,

    /// <summary>目标对象存在且与渲染值(剥管后)一致,下发时跳过且不写任何内容。</summary>
    Identical = 2,

    /// <summary>预取现值失败(如集群不可达),该集群预览/下发被隔离标注。</summary>
    FetchFailed = 3
}

/// <summary>舰队模板判定状态与中文显示名映射。</summary>
public static class FleetTemplateActionExtensions
{
    /// <summary>判定状态的中文显示名(新建/更新/一致/获取失败)。</summary>
    public static string ToDisplayText(this FleetTemplateAction action) => action switch
    {
        FleetTemplateAction.Create => "新建",
        FleetTemplateAction.Update => "更新",
        FleetTemplateAction.Identical => "一致",
        FleetTemplateAction.FetchFailed => "获取失败",
        _ => action.ToString()
    };
}
