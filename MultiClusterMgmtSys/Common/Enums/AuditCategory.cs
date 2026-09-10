namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 审计日志的类别。新增类别时在此处扩展枚举值即可。
/// </summary>
public enum AuditCategory
{
    /// <summary>认证(登录/登出/注册)。</summary>
    Authentication = 0,

    /// <summary>账号管理(增删改、角色调整、密码)。</summary>
    Account = 1,

    /// <summary>集群。</summary>
    Cluster = 2,

    /// <summary>集群分组。</summary>
    Group = 3,

    /// <summary>ConfigMap。</summary>
    Configmap = 4,

    /// <summary>节点(IP 备注等)。</summary>
    Node = 5,

    /// <summary>工作负载(部署/有状态集/守护进程/副本集)。</summary>
    Workload = 6,

    /// <summary>服务(Svc)。</summary>
    Service = 7
}
