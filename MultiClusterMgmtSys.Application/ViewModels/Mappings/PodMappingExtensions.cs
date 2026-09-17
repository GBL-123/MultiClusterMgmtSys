using k8s.Models;
using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Application.ViewModels.Mappings;

/// <summary>
/// core/v1 Pod → 展示 ViewModel 的映射扩展:「容器信号优先于 phase」的徽章判定集中在此,
/// 列表与详情共用同一口径,UI 不重复判定。
/// </summary>
public static class PodMappingExtensions
{
    /// <summary>将 <see cref="V1Pod"/> 映射为 Pod 列表展示数据(轻投影:不含条件与容器明细)。</summary>
    /// <param name="pod">Kubernetes core/v1 Pod 对象。</param>
    /// <param name="clusterId">当前集群 Id,用于生成详情路由。</param>
    public static PodListViewModel ToPodListViewModel(this V1Pod pod, int clusterId)
    {
        var status = pod.Status;
        return new PodListViewModel
        {
            ClusterId = clusterId,
            Name = pod.Metadata?.Name ?? "",
            Namespace = pod.Metadata?.NamespaceProperty ?? "",
            NodeName = pod.Spec?.NodeName ?? "",
            PodIp = status?.PodIP ?? "",
            Phase = status?.Phase ?? "",
            ContainerReason = ResolveContainerSignal(status),
            Restarts = ResolveRestarts(status),
            StartedAt = status?.StartTime
        };
    }

    /// <summary>将 <see cref="V1Pod"/> 映射为 Pod 详情展示数据(基本信息、条件与容器状态)。</summary>
    /// <param name="pod">Kubernetes core/v1 Pod 对象。</param>
    /// <param name="clusterId">当前集群 Id。</param>
    public static PodDetailViewModel ToPodDetailViewModel(this V1Pod pod, int clusterId)
    {
        var status = pod.Status;
        return new PodDetailViewModel
        {
            Name = pod.Metadata?.Name ?? "",
            Namespace = pod.Metadata?.NamespaceProperty ?? "",
            NodeName = pod.Spec?.NodeName ?? "",
            PodIp = status?.PodIP ?? "",
            Phase = status?.Phase ?? "",
            ContainerReason = ResolveContainerSignal(status),
            QosClass = status?.QosClass ?? "",
            StartedAt = status?.StartTime,
            Conditions = status?.Conditions?
                .Select(c => new PodConditionViewModel
                {
                    Type = c.Type ?? "",
                    Status = c.Status ?? "",
                    LastTransitionAt = c.LastTransitionTime
                })
                .ToList() ?? [],
            Containers = ResolveContainerRows(status)
        };
    }

    /// <summary>从 Pod 状态解析容器级异常信号:常规容器优先、init 容器兜底,跳过初始化/创建中的正常等待原因。</summary>
    private static string? ResolveContainerSignal(V1PodStatus? status)
    {
        foreach (var cs in AllStatuses(status))
        {
            var waiting = cs.State?.Waiting;
            if (!string.IsNullOrWhiteSpace(waiting?.Reason)
                && waiting.Reason is not ("PodInitializing" or "ContainerCreating"))
            {
                return waiting.Reason;
            }

            var terminated = cs.State?.Terminated;
            if (!string.IsNullOrWhiteSpace(terminated?.Reason) && terminated.Reason != "Completed")
            {
                return terminated.Reason;
            }
        }

        return null;
    }

    private static int ResolveRestarts(V1PodStatus? status)
        => status?.ContainerStatuses?.Sum(c => c.RestartCount) ?? 0;
    private static List<PodContainerViewModel> ResolveContainerRows(V1PodStatus? status)
        => AllStatuses(status)
            .Select(cs =>
            {
                var state = cs.State;
                var lastTerminated = cs.LastState?.Terminated;
                return new PodContainerViewModel
                {
                    Name = cs.Name ?? "",
                    Ready = cs.Ready,
                    RestartCount = cs.RestartCount,
                    Image = cs.Image ?? "",
                    State = ResolveState(state),
                    WaitingReason = state?.Waiting?.Reason ?? "",
                    TerminatedReason = state?.Terminated?.Reason ?? "",
                    LastTerminatedReason = lastTerminated?.Reason ?? "",
                    LastTerminatedAt = lastTerminated?.FinishedAt
                };
            })
            .ToList();

    private static string ResolveState(V1ContainerState? state)
        => state?.Running is not null ? "Running"
            : state?.Waiting is not null ? "Waiting"
            : state?.Terminated is not null ? "Terminated"
            : "";

    private static IEnumerable<V1ContainerStatus> AllStatuses(V1PodStatus? status)
        => (status?.ContainerStatuses ?? []).Concat(status?.InitContainerStatuses ?? []);
}
