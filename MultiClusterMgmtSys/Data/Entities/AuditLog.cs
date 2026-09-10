using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Data.Entities;

/// <summary>
/// 审计日志:记录用户执行的关键操作(登录、增删改、扩缩容等),由 AuditService 统一写入,
/// 写失败静默降级不阻断业务。CreatedAt 建有索引,审计页按时间排序分页可走索引。
/// </summary>
public class AuditLog
{
    /// <summary>自增主键。</summary>
    public int Id { get; set; }

    /// <summary>操作者用户名;无登录上下文(如未认证场景)时可为空。</summary>
    public string? UserName { get; set; }

    /// <summary>操作类别(认证/账号/集群/分组等),见 <see cref="AuditCategory"/>。</summary>
    public AuditCategory Category { get; set; }

    /// <summary>具体操作(登录/创建/删除等),见 <see cref="AuditAction"/>。</summary>
    public AuditAction Action { get; set; }

    /// <summary>操作对象描述(必填,通常为资源名称或中文说明)。</summary>
    public string Target { get; set; } = "";

    /// <summary>操作发生时间(UTC);建有索引,列表页按此列倒序分页。</summary>
    public DateTime CreatedAt { get; set; }
}
