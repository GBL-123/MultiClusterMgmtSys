using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

/// <summary>
/// apps/v1 四型工作负载 → 统一展示模型的映射。
/// 就绪度与滚动三态逐型取数,页面层只消费统一字段(design D1/D7)。
/// </summary>
public static class WorkloadMappingExtensions
{
    public static WorkloadListViewModel ToWorkloadListViewModel(this V1Deployment dep)
    {
        var desired = dep.Spec?.Replicas ?? 0;
        var ready = dep.Status?.ReadyReplicas ?? 0;
        var updated = dep.Status?.UpdatedReplicas ?? 0;
        var rolling = updated < desired
            || (dep.Metadata?.Generation ?? 0) > (dep.Status?.ObservedGeneration ?? 0);
        return new WorkloadListViewModel
        {
            Name = dep.Metadata?.Name ?? "",
            Namespace = dep.Metadata?.NamespaceProperty ?? "",
            Kind = WorkloadKind.Deployment,
            DesiredCount = desired,
            ReadyCount = ready,
            RolloutState = ComputeState(rolling, ready, desired, updated)
        };
    }

    public static WorkloadListViewModel ToWorkloadListViewModel(this V1StatefulSet sts)
    {
        var desired = sts.Spec?.Replicas ?? 0;
        var ready = sts.Status?.ReadyReplicas ?? 0;
        var updated = sts.Status?.UpdatedReplicas ?? 0;
        var currentRevision = sts.Status?.CurrentRevision;
        var updateRevision = sts.Status?.UpdateRevision;
        var revisionGap = currentRevision != updateRevision;
        var rolling = updated < desired || revisionGap;
        return new WorkloadListViewModel
        {
            Name = sts.Metadata?.Name ?? "",
            Namespace = sts.Metadata?.NamespaceProperty ?? "",
            Kind = WorkloadKind.StatefulSet,
            DesiredCount = desired,
            ReadyCount = ready,
            RolloutState = ComputeState(rolling, ready, desired, updated)
        };
    }

    public static WorkloadListViewModel ToWorkloadListViewModel(this V1DaemonSet ds)
    {
        var desired = ds.Status?.DesiredNumberScheduled ?? 0;
        var ready = ds.Status?.NumberReady ?? 0;
        var updated = ds.Status?.UpdatedNumberScheduled ?? 0;
        var rolling = updated < desired;
        return new WorkloadListViewModel
        {
            Name = ds.Metadata?.Name ?? "",
            Namespace = ds.Metadata?.NamespaceProperty ?? "",
            Kind = WorkloadKind.DaemonSet,
            DesiredCount = desired,
            ReadyCount = ready,
            RolloutState = ComputeState(rolling, ready, desired, updated)
        };
    }

    public static WorkloadListViewModel ToWorkloadListViewModel(this V1ReplicaSet rs)
    {
        var desired = rs.Spec?.Replicas ?? 0;
        var ready = rs.Status?.ReadyReplicas ?? 0;
        var rolling = (rs.Metadata?.Generation ?? 0) > (rs.Status?.ObservedGeneration ?? 0);
        return new WorkloadListViewModel
        {
            Name = rs.Metadata?.Name ?? "",
            Namespace = rs.Metadata?.NamespaceProperty ?? "",
            Kind = WorkloadKind.ReplicaSet,
            DesiredCount = desired,
            ReadyCount = ready,
            RolloutState = ComputeState(rolling, ready, desired, updated: null)
        };
    }

    public static WorkloadDetailViewModel ToWorkloadDetailViewModel(this V1Deployment dep)
    {
        var list = dep.ToWorkloadListViewModel();
        return new WorkloadDetailViewModel
        {
            Name = list.Name,
            Namespace = list.Namespace,
            Uid = dep.Metadata?.Uid ?? "",
            Kind = WorkloadKind.Deployment,
            RolloutState = list.RolloutState,
            DesiredCount = list.DesiredCount,
            ReadyCount = list.ReadyCount,
            UpdatedCount = dep.Status?.UpdatedReplicas ?? 0,
            Selector = FormatSelector(dep.Spec?.Selector?.MatchLabels),
            Conditions = [.. (dep.Status?.Conditions ?? []).Select(c => new WorkloadConditionViewModel
            {
                Type = c.Type ?? "",
                Status = c.Status ?? "",
                Reason = c.Reason ?? "",
                Message = c.Message ?? "",
                LastTransitionAt = c.LastTransitionTime
            })],
            CreatedAt = dep.Metadata?.CreationTimestamp,
            Yaml = KubernetesYaml.Serialize(dep)
        };
    }

    public static WorkloadDetailViewModel ToWorkloadDetailViewModel(this V1StatefulSet sts)
    {
        var list = sts.ToWorkloadListViewModel();
        return new WorkloadDetailViewModel
        {
            Name = list.Name,
            Namespace = list.Namespace,
            Uid = sts.Metadata?.Uid ?? "",
            Kind = WorkloadKind.StatefulSet,
            RolloutState = list.RolloutState,
            DesiredCount = list.DesiredCount,
            ReadyCount = list.ReadyCount,
            UpdatedCount = sts.Status?.UpdatedReplicas ?? 0,
            Selector = FormatSelector(sts.Spec?.Selector?.MatchLabels),
            Conditions = [.. (sts.Status?.Conditions ?? []).Select(c => new WorkloadConditionViewModel
            {
                Type = c.Type ?? "",
                Status = c.Status ?? "",
                Reason = c.Reason ?? "",
                Message = c.Message ?? "",
                LastTransitionAt = c.LastTransitionTime
            })],
            CreatedAt = sts.Metadata?.CreationTimestamp,
            Yaml = KubernetesYaml.Serialize(sts)
        };
    }

    public static WorkloadDetailViewModel ToWorkloadDetailViewModel(this V1DaemonSet ds)
    {
        var list = ds.ToWorkloadListViewModel();
        return new WorkloadDetailViewModel
        {
            Name = list.Name,
            Namespace = list.Namespace,
            Uid = ds.Metadata?.Uid ?? "",
            Kind = WorkloadKind.DaemonSet,
            RolloutState = list.RolloutState,
            DesiredCount = list.DesiredCount,
            ReadyCount = list.ReadyCount,
            UpdatedCount = ds.Status?.UpdatedNumberScheduled ?? 0,
            Selector = FormatSelector(ds.Spec?.Selector?.MatchLabels),
            Conditions = (ds.Status?.Conditions ?? []).Select(c => new WorkloadConditionViewModel
            {
                Type = c.Type ?? "",
                Status = c.Status ?? "",
                Reason = c.Reason ?? "",
                Message = c.Message ?? "",
                LastTransitionAt = c.LastTransitionTime
            }).ToList(),
            CreatedAt = ds.Metadata?.CreationTimestamp,
            Yaml = KubernetesYaml.Serialize(ds)
        };
    }

    public static WorkloadDetailViewModel ToWorkloadDetailViewModel(this V1ReplicaSet rs)
    {
        var list = rs.ToWorkloadListViewModel();
        return new WorkloadDetailViewModel
        {
            Name = list.Name,
            Namespace = list.Namespace,
            Uid = rs.Metadata?.Uid ?? "",
            Kind = WorkloadKind.ReplicaSet,
            RolloutState = list.RolloutState,
            DesiredCount = list.DesiredCount,
            ReadyCount = list.ReadyCount,
            UpdatedCount = 0,
            Selector = FormatSelector(rs.Spec?.Selector?.MatchLabels),
            Conditions = [],
            CreatedAt = rs.Metadata?.CreationTimestamp,
            Yaml = KubernetesYaml.Serialize(rs)
        };
    }

    /// <summary>三态判定:滚动中优先,其次就绪,其余为未就绪。</summary>
    private static WorkloadRolloutState ComputeState(bool rolling, int ready, int desired, int? updated)
    {
        if (rolling)
        {
            return WorkloadRolloutState.Rolling;
        }

        var updatedSatisfied = updated is null || updated == desired;
        if (ready == desired && updatedSatisfied)
        {
            return WorkloadRolloutState.Ready;
        }

        return WorkloadRolloutState.NotReady;
    }

    private static string FormatSelector(IDictionary<string, string>? matchLabels)
    {
        if (matchLabels is null || matchLabels.Count == 0)
        {
            return "";
        }

        return string.Join(", ", matchLabels.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }
}
