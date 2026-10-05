namespace MultiClusterMgmtSys.Application.Enums;

/// <summary>
/// 跨集群对照支持的资源族(五个):四类工作负载 + ConfigMap,
/// 由 <see cref="MultiClusterMgmtSys.Application.Services.ClusterCompareService"/> 消费。
/// </summary>
public enum CompareKind
{
    /// <summary>配置(ConfigMap)。</summary>
    ConfigMap = 0,

    /// <summary>部署(Deployment)。</summary>
    Deployment = 1,

    /// <summary>有状态应用(StatefulSet)。</summary>
    StatefulSet = 2,

    /// <summary>守护进程集(DaemonSet)。</summary>
    DaemonSet = 3,

    /// <summary>副本集(ReplicaSet)。</summary>
    ReplicaSet = 4
}

/// <summary>资源族与中文显示名映射。</summary>
public static class CompareKindExtensions
{
    /// <summary>资源族的中文显示名(配置/部署/有状态应用/守护进程/副本集)。</summary>
    public static string ToDisplayText(this CompareKind kind) => kind switch
    {
        CompareKind.ConfigMap => "配置",
        CompareKind.Deployment => "部署",
        CompareKind.StatefulSet => "有状态应用",
        CompareKind.DaemonSet => "守护进程",
        CompareKind.ReplicaSet => "副本集",
        _ => kind.ToString()
    };
}
