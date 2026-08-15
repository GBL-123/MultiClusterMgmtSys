namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 审计日志的类别。新增类别时在此处扩展枚举值即可。
/// </summary>
public enum AuditCategory
{
    Authentication = 0,
    Account = 1,
    Cluster = 2,
    Group = 3,
    Configmap = 4,
    Node = 5
}
