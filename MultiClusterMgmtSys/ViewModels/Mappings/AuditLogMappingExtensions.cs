using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

public static class AuditLogMappingExtensions
{
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
            _ => category.ToString()
        };
    }

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
            _ => action.ToString()
        };
    }
}
