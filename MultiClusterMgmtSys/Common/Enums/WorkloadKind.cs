namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 工作负载类型(apps/v1)。操作可用性由 <see cref="WorkloadCapabilities"/> 定义,
/// 服务层以"方法缺席"表达不可用操作(如 DaemonSet 无扩缩容)。
/// </summary>
public enum WorkloadKind
{
    /// <summary>无状态部署(Deployment)。</summary>
    Deployment = 0,

    /// <summary>有状态应用(StatefulSet)。</summary>
    StatefulSet = 1,

    /// <summary>守护进程集(DaemonSet)。</summary>
    DaemonSet = 2,

    /// <summary>副本集(ReplicaSet)。</summary>
    ReplicaSet = 3
}

/// <summary>工作负载的类型级能力矩阵:扩缩容适用 Deployment/StatefulSet/ReplicaSet,滚动重启适用 Deployment/StatefulSet/DaemonSet。</summary>
public static class WorkloadCapabilities
{
    /// <summary>该工作负载类型是否支持扩缩容(Deployment/StatefulSet/ReplicaSet 支持)。</summary>
    public static bool SupportsScale(this WorkloadKind kind) => kind
        is WorkloadKind.Deployment
        or WorkloadKind.StatefulSet
        or WorkloadKind.ReplicaSet;

    /// <summary>该工作负载类型是否支持滚动重启(Deployment/StatefulSet/DaemonSet 支持)。</summary>
    public static bool SupportsRestart(this WorkloadKind kind) => kind
        is WorkloadKind.Deployment
        or WorkloadKind.StatefulSet
        or WorkloadKind.DaemonSet;
}

/// <summary>工作负载类型的中文显示名与路由段。</summary>
public static class WorkloadKindExtensions
{
    /// <summary>工作负载类型的中文显示名(部署/有状态应用/守护进程/副本集)。</summary>
    public static string ToDisplayText(this WorkloadKind kind) => kind switch
    {
        WorkloadKind.Deployment => "部署",
        WorkloadKind.StatefulSet => "有状态应用",
        WorkloadKind.DaemonSet => "守护进程",
        WorkloadKind.ReplicaSet => "副本集",
        _ => kind.ToString()
    };

    /// <summary>工作负载类型对应的 URL 路由段(复数小写形式)。</summary>
    public static string ToRouteSegment(this WorkloadKind kind) => kind switch
    {
        WorkloadKind.Deployment => "deployments",
        WorkloadKind.StatefulSet => "statefulsets",
        WorkloadKind.DaemonSet => "daemonsets",
        WorkloadKind.ReplicaSet => "replicasets",
        _ => kind.ToString().ToLowerInvariant()
    };
}
