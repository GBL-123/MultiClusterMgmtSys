namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 审计日志列表展示数据。
/// </summary>
public class AuditLogViewModel
{
    /// <summary>日志主键。</summary>
    public int Id { get; set; }

    /// <summary>操作人用户名;系统操作无身份时为空字符串。</summary>
    public string UserName { get; set; } = "";

    /// <summary>审计类别中文展示名(认证/账号/集群等)。</summary>
    public string CategoryName { get; set; } = "";

    /// <summary>审计动作中文展示名(登录/创建/删除等)。</summary>
    public string ActionName { get; set; } = "";

    /// <summary>操作对象描述。</summary>
    public string Target { get; set; } = "";

    /// <summary>发生时间。</summary>
    public DateTime CreatedAt { get; set; }
}
