using k8s;
using k8s.Autorest;
using k8s.Models;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Common.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>资源拓扑只读聚合服务:按中心资源(K8s 对象)读取其引用/归属/选择关系,输出节点与边供拓扑图渲染;不写审计、不改任何资源。</summary>
public class TopologyService(
    IClusterRepository repo,
    ILogger<TopologyService> logger,
    IClusterClientCache clientCache)
{
    private static readonly HashSet<string> SupportedKinds = new(StringComparer.Ordinal)
    {
        "Pod", "Service", "ConfigMap", "Secret", "PersistentVolumeClaim",
        "Deployment", "StatefulSet", "DaemonSet", "Job"
    };

    /// <summary>按中心资源查询拓扑:中心节点 Layer=0,邻居节点 Layer=1;邻居查询失败按类别丢弃(降级),不破坏整图。</summary>
    /// <param name="request">集群、命名空间、资源 Kind 与名称。</param>
    public async Task<TopologyViewModel> GetTopologyAsync(TopologyQueryRequest request)
    {
        var cluster = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        if (!SupportedKinds.Contains(request.Kind))
        {
            throw new ValidationException($"不支持的拓扑中心资源类型：{request.Kind}");
        }

        var client = clientCache.GetOrCreate(cluster);
        try
        {
            var view = request.Kind switch
            {
                "Pod" => await BuildPodTopologyAsync(client, request.Namespace, request.Name),
                "Service" => await BuildServiceTopologyAsync(client, request.Namespace, request.Name),
                "ConfigMap" => await BuildConfigObjectTopologyAsync(client, request.Kind, request.Namespace, request.Name),
                "Secret" => await BuildConfigObjectTopologyAsync(client, request.Kind, request.Namespace, request.Name),
                "PersistentVolumeClaim" => await BuildClaimTopologyAsync(client, request.Namespace, request.Name),
                _ => await BuildWorkloadTopologyAsync(client, request.Kind, request.Namespace, request.Name),
            };
            TopologyLayout.Apply(view.Nodes);
            return view;
        }
        catch (Exception ex) when (ex is not BusinessException)
        {
            logger.LogWarning(ex, "加载拓扑失败 clusterId={ClusterId} kind={Kind} namespace={Namespace} name={Name}",
                request.ClusterId, request.Kind, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载拓扑");
        }
    }

    private async Task<TopologyViewModel> BuildPodTopologyAsync(IKubernetes client, string ns, string name)
    {
        var pod = await client.CoreV1.ReadNamespacedPodAsync(name, ns);
        var center = MakeNode("Pod", name, ns, isCenter: true, statusHint: pod.Status?.Phase);
        var view = new TopologyViewModel { Nodes = [center], Edges = [] };
        var podId = center.Id;

        var nodeName = pod.Spec?.NodeName;
        if (!string.IsNullOrEmpty(nodeName))
        {
            try
            {
                var node = await client.CoreV1.ReadNodeAsync(nodeName);
                AddNode(view, MakeNode("Node", nodeName, "", statusHint: NodeReadyHint(node)));
                AddEdge(view, podId, $"Node/{nodeName}", "调度");
            }
            catch (Exception ex) when (IsDegradable(ex))
            {
                logger.LogWarning(ex, "拓扑邻居查询失败，节点 {NodeName} 相关边已丢弃", nodeName);
                if (ex is KubernetesException { Status.Code: 404 })
                {
                    AddNode(view, MakeNode("Node", nodeName, "", isMissing: true));
                    AddEdge(view, podId, $"Node/{nodeName}", "调度");
                }
            }
        }

        var owner = pod.Metadata?.OwnerReferences?.FirstOrDefault();
        if (owner is not null)
        {
            await AddOwnerChainAsync(client, view, pod, podId);
        }

        var services = await TryListAsync<V1Service, V1ServiceList>(
            () => client.CoreV1.ListNamespacedServiceAsync(ns), "Service", ns);
        if (services is not null)
        {
            foreach (var svc in services)
            {
                if (Matches(svc.Spec?.Selector, pod))
                {
                    AddNode(view, MakeNode("Service", svc.Metadata?.Name ?? "", ns));
                    AddEdge(view, $"Service/{svc.Metadata?.Name}", podId, "选择");
                }
            }
        }

        var cms = await TryListAsync<V1ConfigMap, V1ConfigMapList>(
            () => client.CoreV1.ListNamespacedConfigMapAsync(ns), "ConfigMap", ns);
        var secrets = await TryListAsync<V1Secret, V1SecretList>(
            () => client.CoreV1.ListNamespacedSecretAsync(ns), "Secret", ns);
        var claims = await TryListAsync<V1PersistentVolumeClaim, V1PersistentVolumeClaimList>(
            () => client.CoreV1.ListNamespacedPersistentVolumeClaimAsync(ns), "PersistentVolumeClaim", ns);
        AddPodMountsAndRefs(view, podId, pod, cms, secrets, claims);

        return view;
    }

    private async Task<TopologyViewModel> BuildServiceTopologyAsync(IKubernetes client, string ns, string name)
    {
        var svc = await client.CoreV1.ReadNamespacedServiceAsync(name, ns);
        var center = MakeNode("Service", name, ns, isCenter: true);
        var view = new TopologyViewModel { Nodes = [center], Edges = [] };
        var svcId = center.Id;

        var pods = await TryListAsync<V1Pod, V1PodList>(
            () => client.CoreV1.ListNamespacedPodAsync(ns), "Pod", ns);
        if (pods is not null)
        {
            foreach (var pod in pods)
            {
                if (Matches(svc.Spec?.Selector, pod))
                {
                    AddNode(view, MakeNode("Pod", pod.Metadata?.Name ?? "", ns, statusHint: pod.Status?.Phase));
                    AddEdge(view, svcId, $"Pod/{pod.Metadata?.Name}", "选择");
                }
            }
        }

        var ingresses = await TryListAsync<V1Ingress, V1IngressList>(
            () => client.NetworkingV1.ListNamespacedIngressAsync(ns), "Ingress", ns);
        if (ingresses is not null)
        {
            foreach (var ingress in ingresses)
            {
                if (RoutesService(ingress, name))
                {
                    AddNode(view, MakeNode("Ingress", ingress.Metadata?.Name ?? "", ns));
                    AddEdge(view, $"Ingress/{ingress.Metadata?.Name}", svcId, "路由");
                }
            }
        }

        return view;
    }

    private async Task<TopologyViewModel> BuildConfigObjectTopologyAsync(IKubernetes client, string kind, string ns, string name)
    {
        if (kind == "Secret")
        {
            await client.CoreV1.ReadNamespacedSecretAsync(name, ns);
        }
        else
        {
            await client.CoreV1.ReadNamespacedConfigMapAsync(name, ns);
        }

        var view = new TopologyViewModel
        {
            Nodes = [MakeNode(kind, name, ns, isCenter: true)],
            Edges = []
        };
        var centerId = $"{kind}/{name}";

        var pods = await TryListAsync<V1Pod, V1PodList>(
            () => client.CoreV1.ListNamespacedPodAsync(ns), "Pod", ns);
        if (pods is not null)
        {
            foreach (var pod in pods)
            {
                foreach (var relation in RelationsOf(pod, kind, name))
                {
                    AddNode(view, MakeNode("Pod", pod.Metadata?.Name ?? "", ns, statusHint: pod.Status?.Phase));
                    AddEdge(view, $"Pod/{pod.Metadata?.Name}", centerId, relation);
                }
            }
        }

        return view;
    }

    private async Task<TopologyViewModel> BuildClaimTopologyAsync(IKubernetes client, string ns, string name)
    {
        var claim = await client.CoreV1.ReadNamespacedPersistentVolumeClaimAsync(name, ns);
        var center = MakeNode("PersistentVolumeClaim", name, ns, isCenter: true, statusHint: claim.Status?.Phase);
        var view = new TopologyViewModel { Nodes = [center], Edges = [] };
        var claimId = center.Id;

        var pods = await TryListAsync<V1Pod, V1PodList>(
            () => client.CoreV1.ListNamespacedPodAsync(ns), "Pod", ns);
        if (pods is not null)
        {
            foreach (var pod in pods)
            {
                foreach (var relation in RelationsOf(pod, "PersistentVolumeClaim", name))
                {
                    AddNode(view, MakeNode("Pod", pod.Metadata?.Name ?? "", ns, statusHint: pod.Status?.Phase));
                    AddEdge(view, $"Pod/{pod.Metadata?.Name}", claimId, relation);
                }
            }
        }

        var volumeName = claim.Spec?.VolumeName;
        if (!string.IsNullOrEmpty(volumeName))
        {
            try
            {
                await client.CoreV1.ReadPersistentVolumeAsync(volumeName);
                AddNode(view, MakeNode("PersistentVolume", volumeName, ""));
                AddEdge(view, $"PersistentVolume/{volumeName}", claimId, "供给");
            }
            catch (Exception ex) when (IsDegradable(ex))
            {
                logger.LogWarning(ex, "拓扑邻居查询失败，PV {VolumeName} 供给边已丢弃", volumeName);
            }
        }

        return view;
    }

    private async Task<TopologyViewModel> BuildWorkloadTopologyAsync(IKubernetes client, string kind, string ns, string name)
    {
        V1LabelSelector? selector = kind switch
        {
            "Deployment" => (await client.AppsV1.ReadNamespacedDeploymentAsync(name, ns))?.Spec?.Selector,
            "StatefulSet" => (await client.AppsV1.ReadNamespacedStatefulSetAsync(name, ns))?.Spec?.Selector,
            "DaemonSet" => (await client.AppsV1.ReadNamespacedDaemonSetAsync(name, ns))?.Spec?.Selector,
            _ => (await client.BatchV1.ReadNamespacedJobAsync(name, ns))?.Spec?.Selector,
        };

        var view = new TopologyViewModel
        {
            Nodes = [MakeNode(kind, name, ns, isCenter: true)],
            Edges = []
        };
        var workloadId = $"{kind}/{name}";

        var pods = await TryListAsync<V1Pod, V1PodList>(
            () => client.CoreV1.ListNamespacedPodAsync(ns), "Pod", ns);
        if (pods is not null)
        {
            foreach (var pod in pods)
            {
                if (Matches(selector, pod))
                {
                    AddNode(view, MakeNode("Pod", pod.Metadata?.Name ?? "", ns, statusHint: pod.Status?.Phase));
                    AddEdge(view, workloadId, $"Pod/{pod.Metadata?.Name}", "拥有");
                }
            }
        }

        return view;
    }

    private async Task AddOwnerChainAsync(IKubernetes client, TopologyViewModel view, V1Pod pod, string podId)
    {
        var owner = pod.Metadata?.OwnerReferences?.FirstOrDefault();
        if (owner is null)
        {
            return;
        }

        var ns = pod.Metadata?.NamespaceProperty ?? "";
        var ownerId = $"{owner.Kind}/{owner.Name}";

        if (owner.Kind == "ReplicaSet")
        {
            try
            {
                var rs = await client.AppsV1.ReadNamespacedReplicaSetAsync(owner.Name, ns);
                AddNode(view, MakeNode("ReplicaSet", owner.Name, ns));
                AddEdge(view, ownerId, podId, "拥有");

                var parent = rs.Metadata?.OwnerReferences?.FirstOrDefault();
                if (parent is not null)
                {
                    var parentNodeId = await AddOwnerNodeAsync(client, view, parent, ns);
                    if (parentNodeId is not null)
                    {
                        AddEdge(view, parentNodeId, ownerId, "拥有");
                    }
                }
            }
            catch (Exception ex) when (IsDegradable(ex))
            {
                logger.LogWarning(ex, "拓扑属主查询失败，副本集 {Name} 相关边已丢弃", owner.Name);
                if (ex is KubernetesException { Status.Code: 404 })
                {
                    AddNode(view, MakeNode("ReplicaSet", owner.Name, ns, isMissing: true));
                    AddEdge(view, ownerId, podId, "拥有");
                }
            }
            return;
        }

        await AddOwnerNodeAsync(client, view, owner, ns, childId: podId);
    }

    /// <summary>读取属主对象并加入节点与「拥有」边;返回属主节点 Id(查询失败且非 404 时返回 null 表示丢弃)。</summary>
    private async Task<string?> AddOwnerNodeAsync(IKubernetes client, TopologyViewModel view, V1OwnerReference owner, string ns, string? childId = null)
    {
        var ownerId = $"{owner.Kind}/{owner.Name}";
        try
        {
            switch (owner.Kind)
            {
                case "Deployment":
                    await client.AppsV1.ReadNamespacedDeploymentAsync(owner.Name, ns);
                    break;
                case "StatefulSet":
                    await client.AppsV1.ReadNamespacedStatefulSetAsync(owner.Name, ns);
                    break;
                case "DaemonSet":
                    await client.AppsV1.ReadNamespacedDaemonSetAsync(owner.Name, ns);
                    break;
                case "Job":
                    await client.BatchV1.ReadNamespacedJobAsync(owner.Name, ns);
                    break;
                default:
                    break;
            }
            AddNode(view, MakeNode(owner.Kind, owner.Name, ns));
            if (childId is not null)
            {
                AddEdge(view, ownerId, childId, "拥有");
            }
            return ownerId;
        }
        catch (Exception ex) when (IsDegradable(ex))
        {
            logger.LogWarning(ex, "拓扑属主查询失败，{Kind}/{Name} 相关边已丢弃", owner.Kind, owner.Name);
            if (ex is KubernetesException { Status.Code: 404 })
            {
                AddNode(view, MakeNode(owner.Kind, owner.Name, ns, isMissing: true));
                if (childId is not null)
                {
                    AddEdge(view, ownerId, childId, "拥有");
                }
                return ownerId;
            }
            return null;
        }
    }

    private void AddPodMountsAndRefs(
        TopologyViewModel view,
        string podId,
        V1Pod pod,
        IList<V1ConfigMap>? cms,
        IList<V1Secret>? secrets,
        IList<V1PersistentVolumeClaim>? claims)
    {
        var ns = pod.Metadata?.NamespaceProperty ?? "";
        var cmNames = cms?.Select(x => x.Metadata?.Name).ToHashSet(StringComparer.Ordinal);
        var secretNames = secrets?.Select(x => x.Metadata?.Name).ToHashSet(StringComparer.Ordinal);
        var claimNames = claims?.Select(x => x.Metadata?.Name).ToHashSet(StringComparer.Ordinal);
        var claimPhases = claims?.Where(x => !string.IsNullOrEmpty(x.Metadata?.Name))
            .ToDictionary(x => x.Metadata!.Name, x => x.Status?.Phase ?? "", StringComparer.Ordinal);

        foreach (var (kind, refName, relation) in MountsAndRefsOf(pod))
        {
            var exists = kind switch
            {
                "ConfigMap" => cmNames?.Contains(refName),
                "Secret" => secretNames?.Contains(refName),
                _ => claimNames?.Contains(refName),
            };
            if (exists is null)
            {
                continue;
            }

            string? phase = null;
            if (kind == "PersistentVolumeClaim" && claimPhases?.TryGetValue(refName, out var claimPhase) == true)
            {
                phase = claimPhase;
            }

            if (exists.Value)
            {
                AddNode(view, MakeNode(kind, refName, ns, statusHint: phase));
            }
            else
            {
                AddNode(view, MakeNode(kind, refName, ns, isMissing: true));
            }

            AddEdge(view, podId, $"{kind}/{refName}", relation);
        }
    }

    private async Task<IList<T>?> TryListAsync<T, TList>(Func<Task<TList?>> fetch, string what, string ns)
        where TList : IItems<T>
    {
        try
        {
            var list = await fetch();
            return list?.Items;
        }
        catch (Exception ex) when (IsDegradable(ex))
        {
            logger.LogWarning(ex, "拓扑邻居列表查询失败，丢弃 {What} 相关边 namespace={Namespace}", what, ns);
            return null;
        }
    }

    private static TopologyNodeViewModel MakeNode(
        string kind, string name, string ns, bool isCenter = false, bool isMissing = false, string? statusHint = null)
        => new()
        {
            Id = $"{kind}/{name}",
            Kind = kind,
            Name = name,
            Namespace = ns,
            IsCenter = isCenter,
            IsMissing = isMissing,
            StatusHint = statusHint
        };

    private static void AddNode(TopologyViewModel view, TopologyNodeViewModel node)
    {
        if (view.Nodes.Any(n => n.Id == node.Id))
        {
            return;
        }

        view.Nodes.Add(node);
    }

    private static void AddEdge(TopologyViewModel view, string from, string to, string relation)
    {
        if (from == to || view.Edges.Any(e => e.From == from && e.To == to && e.Relation == relation))
        {
            return;
        }

        view.Edges.Add(new TopologyEdgeViewModel { From = from, To = to, Relation = relation });
    }

    private static string NodeReadyHint(V1Node node)
    {
        var ready = node.Status?.Conditions?.FirstOrDefault(c => c.Type == "Ready")?.Status;
        return ready == "True" ? "Ready" : "NotReady";
    }

    private static bool RoutesService(V1Ingress ingress, string serviceName)
    {
        foreach (var rule in ingress.Spec?.Rules ?? [])
        {
            foreach (var path in rule.Http?.Paths ?? [])
            {
                if (path.Backend?.Service?.Name == serviceName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool Matches(IDictionary<string, string>? selector, V1Pod pod)
    {
        if (selector is null || selector.Count == 0)
        {
            return false;
        }

        var labels = pod.Metadata?.Labels;
        if (labels is null)
        {
            return false;
        }

        return selector.All(kv => labels.TryGetValue(kv.Key, out var value) && value == kv.Value);
    }

    private static bool Matches(V1LabelSelector? selector, V1Pod pod)
    {
        var matchLabels = selector?.MatchLabels;
        if (matchLabels is null || matchLabels.Count == 0)
        {
            return false;
        }

        var labels = pod.Metadata?.Labels;
        if (labels is null)
        {
            return false;
        }

        return matchLabels.All(kv => labels.TryGetValue(kv.Key, out var value) && value == kv.Value);
    }

    private static IEnumerable<string> RelationsOf(V1Pod pod, string kind, string name)
    {
        foreach (var volume in pod.Spec?.Volumes ?? [])
        {
            var refName = kind switch
            {
                "ConfigMap" => volume.ConfigMap?.Name,
                "Secret" => volume.Secret?.SecretName,
                _ => volume.PersistentVolumeClaim?.ClaimName,
            };
            if (refName == name)
            {
                yield return "挂载";
            }
        }

        if (kind == "PersistentVolumeClaim")
        {
            yield break;
        }

        foreach (var container in PodContainers(pod))
        {
            foreach (var env in container.Env ?? [])
            {
                var refName = kind == "ConfigMap"
                    ? env.ValueFrom?.ConfigMapKeyRef?.Name
                    : env.ValueFrom?.SecretKeyRef?.Name;
                if (refName == name)
                {
                    yield return "引用";
                }
            }

            foreach (var envFrom in container.EnvFrom ?? [])
            {
                var refName = kind == "ConfigMap"
                    ? envFrom.ConfigMapRef?.Name
                    : envFrom.SecretRef?.Name;
                if (refName == name)
                {
                    yield return "引用";
                }
            }
        }
    }

    private static IEnumerable<(string Kind, string Name, string Relation)> MountsAndRefsOf(V1Pod pod)
    {
        foreach (var volume in pod.Spec?.Volumes ?? [])
        {
            if (!string.IsNullOrEmpty(volume.ConfigMap?.Name))
            {
                yield return ("ConfigMap", volume.ConfigMap.Name, "挂载");
            }

            if (!string.IsNullOrEmpty(volume.Secret?.SecretName))
            {
                yield return ("Secret", volume.Secret.SecretName, "挂载");
            }

            if (!string.IsNullOrEmpty(volume.PersistentVolumeClaim?.ClaimName))
            {
                yield return ("PersistentVolumeClaim", volume.PersistentVolumeClaim.ClaimName, "挂载");
            }
        }

        foreach (var container in PodContainers(pod))
        {
            foreach (var env in container.Env ?? [])
            {
                if (!string.IsNullOrEmpty(env.ValueFrom?.ConfigMapKeyRef?.Name))
                {
                    yield return ("ConfigMap", env.ValueFrom.ConfigMapKeyRef.Name, "引用");
                }

                if (!string.IsNullOrEmpty(env.ValueFrom?.SecretKeyRef?.Name))
                {
                    yield return ("Secret", env.ValueFrom.SecretKeyRef.Name, "引用");
                }
            }

            foreach (var envFrom in container.EnvFrom ?? [])
            {
                if (!string.IsNullOrEmpty(envFrom.ConfigMapRef?.Name))
                {
                    yield return ("ConfigMap", envFrom.ConfigMapRef.Name, "引用");
                }

                if (!string.IsNullOrEmpty(envFrom.SecretRef?.Name))
                {
                    yield return ("Secret", envFrom.SecretRef.Name, "引用");
                }
            }
        }
    }

    private static IEnumerable<V1Container> PodContainers(V1Pod pod)
    {
        foreach (var container in pod.Spec?.Containers ?? [])
        {
            yield return container;
        }

        foreach (var container in pod.Spec?.InitContainers ?? [])
        {
            yield return container;
        }

        foreach (var container in pod.Spec?.EphemeralContainers ?? [])
        {
            yield return new V1Container
            {
                Env = container.Env,
                EnvFrom = container.EnvFrom
            };
        }
    }

    private static bool IsDegradable(Exception ex) =>
        ex is KubernetesException or HttpOperationException or TaskCanceledException
            or OperationCanceledException or HttpRequestException;
}
