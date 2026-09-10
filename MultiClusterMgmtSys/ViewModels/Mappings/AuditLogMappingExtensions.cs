using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

/// <summary>
/// 审计日志实体与类别/动作枚举 → 中文展示文本的映射。
/// </summary>
public static class AuditLogMappingExtensions
{
    /// <summary>审计日志实体 → 审计日志展示 ViewModel 映射(类别与动作转中文名)。</summary>
    public static AuditLogViewModel ToAuditLogViewModel(this AuditLog log)
    {
        return new AuditLogViewModel
        {
            Id = log.Id,
            UserName = log.UserName ?? "",
            CategoryName = log.Category.ToDisplayName(),
            ActionName = log.Action.ToDisplayName(),
            Target = log.Target,
            CreatedAt = log.CreatedAt
        };
    }

    /// <summary>审计类别枚举 → 中文展示名。</summary>
    public static string ToDisplayName(this AuditCategory category)
    {
        return category switch
        {
            AuditCategory.Authentication => "认证",
            AuditCategory.Account => "账号",
            AuditCategory.Cluster => "集群",
            AuditCategory.Group => "分组",
            AuditCategory.Configmap => "配置",
            AuditCategory.Node => "节点",
            AuditCategory.Workload => "工作负载",
            _ => category.ToString()
        };
    }

    /// <summary>审计动作枚举 → 中文展示名。</summary>
    public static string ToDisplayName(this AuditAction action)
    {
        return action switch
        {
            AuditAction.Login => "登录",
            AuditAction.Logout => "登出",
            AuditAction.Register => "注册",
            AuditAction.Create => "创建",
            AuditAction.Update => "修改",
            AuditAction.Delete => "删除",
            AuditAction.Move => "移动",
            AuditAction.Rename => "重命名",
            AuditAction.Scale => "扩缩容",
            AuditAction.Restart => "重启",
            _ => action.ToString()
        };
    }
}
