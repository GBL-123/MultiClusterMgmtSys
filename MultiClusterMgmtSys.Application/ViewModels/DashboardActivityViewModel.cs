namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 看板最近操作项:一行审计记录的紧凑展示形态。
/// </summary>
public class DashboardActivityViewModel
{
    /// <summary>操作发生时间(UTC)。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>操作人用户名;无身份的系统操作为空字符串。</summary>
    public string UserName { get; set; } = "";

    /// <summary>审计类别中文名(认证/账号/集群等)。</summary>
    public string CategoryName { get; set; } = "";

    /// <summary>审计动作中文名(登录/创建/修改等)。</summary>
    public string ActionName { get; set; } = "";

    /// <summary>操作对象描述。</summary>
    public string Target { get; set; } = "";
}
