namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 审计日志的操作类型。新增操作时在此处扩展枚举值即可。
/// </summary>
public enum AuditAction
{
    /// <summary>用户登录。</summary>
    Login = 0,

    /// <summary>用户登出。</summary>
    Logout = 1,

    /// <summary>注册新账号。</summary>
    Register = 2,

    /// <summary>创建资源(集群/分组/账号/端点等)。</summary>
    Create = 3,

    /// <summary>更新资源信息。</summary>
    Update = 4,

    /// <summary>删除资源。</summary>
    Delete = 5,

    /// <summary>移动资源(如集群调整所属分组)。</summary>
    Move = 6,

    /// <summary>重命名资源。</summary>
    Rename = 7,

    /// <summary>工作负载扩缩容。</summary>
    Scale = 8,

    /// <summary>工作负载滚动重启。</summary>
    Restart = 9
}
