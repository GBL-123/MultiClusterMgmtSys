using k8s;
using k8s.Models;

namespace MultiClusterMgmtSys.Application.ViewModels.Mappings;

/// <summary>
/// V1 存储三类(PVC/PV/StorageClass) ⇄ 存储展示模型的映射(契约见 storage-management)。
/// </summary>
public static class StorageMappingExtensions
{
    /// <summary>把 <see cref="V1PersistentVolumeClaim"/> 映射为 PVC 列表展示数据。</summary>
    public static StorageClaimListViewModel ToClaimListViewModel(this V1PersistentVolumeClaim claim)
    {
        return new StorageClaimListViewModel
        {
            Name = claim.Metadata?.Name ?? "",
            Namespace = claim.Metadata?.NamespaceProperty ?? "",
            Phase = claim.Status?.Phase ?? "",
            Capacity = ClaimRequestedStorage(claim),
            StorageClass = claim.Spec?.StorageClassName ?? "",
            CreatedAt = claim.Metadata?.CreationTimestamp
        };
    }

    /// <summary>把 <see cref="V1PersistentVolumeClaim"/> 与其挂载 Pod 映射为详情展示数据。</summary>
    public static StorageClaimDetailViewModel ToClaimDetailViewModel(this V1PersistentVolumeClaim claim, List<StorageMountedPodViewModel> mountedPods)
    {
        return new StorageClaimDetailViewModel
        {
            Name = claim.Metadata?.Name ?? "",
            Namespace = claim.Metadata?.NamespaceProperty ?? "",
            Uid = claim.Metadata?.Uid ?? "",
            Phase = claim.Status?.Phase ?? "",
            Capacity = ClaimRequestedStorage(claim),
            AccessModes = [.. claim.Spec?.AccessModes ?? []],
            StorageClass = claim.Spec?.StorageClassName ?? "",
            CreatedAt = claim.Metadata?.CreationTimestamp,
            Yaml = KubernetesYaml.Serialize(claim),
            CanOperate = false,
            MountedPods = mountedPods
        };
    }

    /// <summary>把挂载声明的 <see cref="V1Pod"/> 映射为挂载 Pod 行。</summary>
    public static StorageMountedPodViewModel ToMountedPodViewModel(this V1Pod pod)
    {
        return new StorageMountedPodViewModel
        {
            Name = pod.Metadata?.Name ?? "",
            Phase = pod.Status?.Phase ?? "",
            StartedAt = StartOrDefaultStartedAt(pod)
        };
    }

    /// <summary>把 <see cref="V1PersistentVolume"/> 映射为 PV 列表展示数据。</summary>
    public static StorageVolumeListViewModel ToVolumeListViewModel(this V1PersistentVolume volume)
    {
        return new StorageVolumeListViewModel
        {
            Name = volume.Metadata?.Name ?? "",
            Phase = volume.Status?.Phase ?? "",
            Capacity = VolumeCapacity(volume),
            ReclaimPolicy = volume.Spec?.PersistentVolumeReclaimPolicy ?? "",
            BoundClaim = VolumeBoundClaim(volume),
            StorageClassName = volume.Spec?.StorageClassName ?? "",
            CreatedAt = volume.Metadata?.CreationTimestamp
        };
    }

    /// <summary>把 <see cref="V1PersistentVolume"/> 映射为详情展示数据。</summary>
    public static StorageVolumeDetailViewModel ToVolumeDetailViewModel(this V1PersistentVolume volume)
    {
        return new StorageVolumeDetailViewModel
        {
            Name = volume.Metadata?.Name ?? "",
            Phase = volume.Status?.Phase ?? "",
            Capacity = VolumeCapacity(volume),
            ReclaimPolicy = volume.Spec?.PersistentVolumeReclaimPolicy ?? "",
            BoundClaim = VolumeBoundClaim(volume),
            StorageClassName = volume.Spec?.StorageClassName ?? "",
            SourceText = VolumeSourceText(volume),
            CreatedAt = volume.Metadata?.CreationTimestamp,
            Yaml = KubernetesYaml.Serialize(volume)
        };
    }

    /// <summary>把 <see cref="V1StorageClass"/> 映射为存储类列表展示数据。</summary>
    public static StorageClassListViewModel ToClassListViewModel(this V1StorageClass storageClass)
    {
        return new StorageClassListViewModel
        {
            Name = storageClass.Metadata?.Name ?? "",
            Provisioner = storageClass.Provisioner ?? "",
            ReclaimPolicy = storageClass.ReclaimPolicy ?? "",
            VolumeBindingMode = storageClass.VolumeBindingMode ?? "",
            AllowVolumeExpansion = storageClass.AllowVolumeExpansion,
            CreatedAt = storageClass.Metadata?.CreationTimestamp
        };
    }

    /// <summary>取 PVC 请求容量(spec.resources.requests[storage])。</summary>
    private static string ClaimRequestedStorage(V1PersistentVolumeClaim claim)
    {
        var requests = claim.Spec?.Resources?.Requests;
        if (requests is not null && requests.TryGetValue("storage", out var quantity))
        {
            return quantity?.ToString() ?? "";
        }

        return "";
    }

    /// <summary>取 PV 容量(spec.capacity[storage])。</summary>
    private static string VolumeCapacity(V1PersistentVolume volume)
    {
        var capacity = volume.Spec?.Capacity;
        if (capacity is not null && capacity.TryGetValue("storage", out var quantity))
        {
            return quantity?.ToString() ?? "";
        }

        return "";
    }

    /// <summary>取 PV 绑定的 claim(namespace/name);未绑定为 null。</summary>
    private static string? VolumeBoundClaim(V1PersistentVolume volume)
    {
        var refNamespace = volume.Spec?.ClaimRef?.NamespaceProperty;
        var refName = volume.Spec?.ClaimRef?.Name;
        if (string.IsNullOrEmpty(refName))
        {
            return null;
        }

        return string.IsNullOrEmpty(refNamespace) ? refName : $"{refNamespace}/{refName}";
    }

    /// <summary>取 PV 来源概述(Local/HostPath/NFS/CSI);无法判定为 null,由页面回退到 YAML。</summary>
    private static string? VolumeSourceText(V1PersistentVolume volume)
    {
        var source = volume.Spec;
        if (source is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(source.Local?.Path))
        {
            return $"Local:{source.Local.Path}";
        }

        if (!string.IsNullOrEmpty(source.HostPath?.Path))
        {
            return $"HostPath:{source.HostPath.Path}";
        }

        if (!string.IsNullOrEmpty(source.Nfs?.Server))
        {
            return $"NFS:{source.Nfs.Server}{source.Nfs.Path}";
        }

        if (!string.IsNullOrEmpty(source.Csi?.Driver))
        {
            return $"CSI:{source.Csi.Driver}";
        }

        return null;
    }

    /// <summary>取 Pod 开始时间(status.startTime 或首个运行中容器的 startedAt)。</summary>
    private static DateTime? StartOrDefaultStartedAt(V1Pod pod)
    {
        var startAt = pod.Status?.StartTime;
        if (startAt is not null)
        {
            return startAt;
        }

        var runningStartAt = pod.Status?.ContainerStatuses?
            .Select(cs => cs.State?.Running?.StartedAt)
            .FirstOrDefault(started => started is not null);
        if (runningStartAt is not null)
        {
            return runningStartAt;
        }

        return null;
    }
}
