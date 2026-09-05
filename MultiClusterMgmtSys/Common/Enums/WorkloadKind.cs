namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 工作负载类型(apps/v1)。操作可用性由 <see cref="WorkloadCapabilities"/> 定义,
/// 服务层以"方法缺席"表达不可用操作(如 DaemonSet 无扩缩容)。
/// </summary>
public enum WorkloadKind
{
    Deployment = 0,
    StatefulSet = 1,
    DaemonSet = 2,
    ReplicaSet = 3
}

/// <summary>工作负载的类型级能力矩阵:扩缩容适用 Deployment/StatefulSet/ReplicaSet,滚动重启适用 Deployment/StatefulSet/DaemonSet。</summary>
public static class WorkloadCapabilities
{
    public static bool SupportsScale(this WorkloadKind kind) => kind
        is WorkloadKind.Deployment
        or WorkloadKind.StatefulSet
        or WorkloadKind.ReplicaSet;

    public static bool SupportsRestart(this WorkloadKind kind) => kind
        is WorkloadKind.Deployment
        or WorkloadKind.StatefulSet
        or WorkloadKind.DaemonSet;
}

/// <summary>工作负载类型的中文显示名与路由段。</summary>
public static class WorkloadKindExtensions
{
    public static string ToDisplayText(this WorkloadKind kind) => kind switch
    {
        WorkloadKind.Deployment => "部署",
        WorkloadKind.StatefulSet => "有状态应用",
        WorkloadKind.DaemonSet => "守护进程",
        WorkloadKind.ReplicaSet => "副本集",
        _ => kind.ToString()
    };

    public static string ToRouteSegment(this WorkloadKind kind) => kind switch
    {
        WorkloadKind.Deployment => "deployments",
        WorkloadKind.StatefulSet => "statefulsets",
        WorkloadKind.DaemonSet => "daemonsets",
        WorkloadKind.ReplicaSet => "replicasets",
        _ => kind.ToString().ToLowerInvariant()
    };
}
