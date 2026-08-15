namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 审计日志的操作类型。新增操作时在此处扩展枚举值即可。
/// </summary>
public enum AuditAction
{
    Login = 0,
    Logout = 1,
    Register = 2,
    Create = 3,
    Update = 4,
    Delete = 5,
    Move = 6,
    Rename = 7
}
