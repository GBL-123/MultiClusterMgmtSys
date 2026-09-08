using k8s;
using k8s.Autorest;
using k8s.Models;
using Moq;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public static class K8sMocks
{
    public static Mock<IKubernetes> Create(MockBehavior behavior = MockBehavior.Loose)
        => new(behavior);

    public static Func<KubernetesClientConfiguration, IKubernetes> Factory(Mock<IKubernetes> mock)
        => _ => mock.Object;

    public static (Mock<IKubernetes> Client, Func<KubernetesClientConfiguration, IKubernetes> Factory) LazyFailing()
    {
        var mock = new Mock<IKubernetes>(MockBehavior.Strict);
        return (mock, Factory(mock));
    }

    public static KubernetesException K8sError(int code, string? message = null)
        => new(new V1Status { Code = code, Message = message ?? $"api-error-{code}" });

    public static void SetupListNodes(this Mock<IKubernetes> mock, params V1Node[] nodes)
        => mock.Setup(x => x.CoreV1.ListNodeWithHttpMessagesAsync(
                It.IsAny<bool?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1NodeList>
            {
                Body = new V1NodeList { Items = nodes.ToList() }
            });

    public static void SetupListNodesThrows(this Mock<IKubernetes> mock, Exception ex)
        => mock.Setup(x => x.CoreV1.ListNodeWithHttpMessagesAsync(
                It.IsAny<bool?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupGetVersion(this Mock<IKubernetes> mock, string gitVersion)
        => mock.Setup(x => x.Version.GetCodeWithHttpMessagesAsync(
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<VersionInfo>
            {
                Body = new VersionInfo { GitVersion = gitVersion }
            });

    public static void SetupGetVersionThrows(this Mock<IKubernetes> mock, Exception ex)
        => mock.Setup(x => x.Version.GetCodeWithHttpMessagesAsync(
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupReadNode(this Mock<IKubernetes> mock, string nodeName, V1Node node)
        => mock.Setup(x => x.CoreV1.ReadNodeWithHttpMessagesAsync(
                It.Is<string>(n => n == nodeName),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Node> { Body = node });

    public static void SetupReadNodeThrows(this Mock<IKubernetes> mock, string nodeName, Exception ex)
        => mock.Setup(x => x.CoreV1.ReadNodeWithHttpMessagesAsync(
                It.Is<string>(n => n == nodeName),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupListNamespaces(this Mock<IKubernetes> mock, params string[] namespaces)
        => mock.Setup(x => x.CoreV1.ListNamespaceWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1NamespaceList>
            {
                Body = new V1NamespaceList
                {
                    Items = namespaces.Select(n => new V1Namespace { Metadata = new V1ObjectMeta { Name = n } }).ToList()
                }
            });

    public static void SetupListConfigMaps(this Mock<IKubernetes> mock, params V1ConfigMap[] items)
        => mock.Setup(x => x.CoreV1.ListConfigMapForAllNamespacesWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMapList>
            {
                Body = new V1ConfigMapList { Items = items.ToList() }
            });

    public static void SetupListNamespacedConfigMaps(this Mock<IKubernetes> mock, string ns, params V1ConfigMap[] items)
        => mock.Setup(x => x.CoreV1.ListNamespacedConfigMapWithHttpMessagesAsync(
                It.Is<string>(n => n == ns),
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMapList>
            {
                Body = new V1ConfigMapList { Items = items.ToList() }
            });

    public static void SetupListNamespacesThrows(this Mock<IKubernetes> mock, Exception ex)
        => mock.Setup(x => x.CoreV1.ListNamespaceWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupListConfigMapsThrows(this Mock<IKubernetes> mock, Exception ex)
        => mock.Setup(x => x.CoreV1.ListConfigMapForAllNamespacesWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupReadConfigMap(this Mock<IKubernetes> mock, string name, string ns, V1ConfigMap configMap)
        => mock.Setup(x => x.CoreV1.ReadNamespacedConfigMapWithHttpMessagesAsync(
                It.Is<string>(n => n == name),
                It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMap> { Body = configMap });

    public static void SetupReadConfigMapThrows(this Mock<IKubernetes> mock, string name, string ns, Exception ex)
        => mock.Setup(x => x.CoreV1.ReadNamespacedConfigMapWithHttpMessagesAsync(
                It.Is<string>(n => n == name),
                It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupDeleteConfigMap(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.CoreV1.DeleteNamespacedConfigMapWithHttpMessagesAsync(
                It.Is<string>(n => n == name),
                It.Is<string>(n => n == ns),
                It.IsAny<V1DeleteOptions?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Status> { Body = new V1Status() });

    public static void SetupDeleteConfigMapThrows(this Mock<IKubernetes> mock, string name, string ns, Exception ex)
        => mock.Setup(x => x.CoreV1.DeleteNamespacedConfigMapWithHttpMessagesAsync(
                It.Is<string>(n => n == name),
                It.Is<string>(n => n == ns),
                It.IsAny<V1DeleteOptions?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupCreateConfigMap(this Mock<IKubernetes> mock, string ns, V1ConfigMap? returnBody = null)
        => mock.Setup(x => x.CoreV1.CreateNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<V1ConfigMap>(),
                It.Is<string>(n => n == ns),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMap> { Body = returnBody ?? new V1ConfigMap() });

    public static void SetupCreateConfigMapThrows(this Mock<IKubernetes> mock, string ns, Exception ex)
        => mock.Setup(x => x.CoreV1.CreateNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<V1ConfigMap>(),
                It.Is<string>(n => n == ns),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupReplaceConfigMap(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.CoreV1.ReplaceNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<V1ConfigMap>(),
                It.Is<string>(n => n == name),
                It.Is<string>(n => n == ns),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMap> { Body = new V1ConfigMap() });

    public static void SetupReplaceConfigMapThrows(this Mock<IKubernetes> mock, string name, string ns, Exception ex)
        => mock.Setup(x => x.CoreV1.ReplaceNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<V1ConfigMap>(),
                It.Is<string>(n => n == name),
                It.Is<string>(n => n == ns),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    // ---- apps/v1: Deployment ----

    public static void SetupListDeployments(this Mock<IKubernetes> mock, params V1Deployment[] items)
        => mock.Setup(x => x.AppsV1.ListDeploymentForAllNamespacesWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1DeploymentList>
            {
                Body = new V1DeploymentList { Items = items.ToList() }
            });

    public static void SetupListNamespacedDeployments(this Mock<IKubernetes> mock, string ns, params V1Deployment[] items)
        => mock.Setup(x => x.AppsV1.ListNamespacedDeploymentWithHttpMessagesAsync(
                It.Is<string>(n => n == ns),
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1DeploymentList>
            {
                Body = new V1DeploymentList { Items = items.ToList() }
            });

    public static void SetupListDeploymentsThrows(this Mock<IKubernetes> mock, Exception ex)
        => mock.Setup(x => x.AppsV1.ListDeploymentForAllNamespacesWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupReadDeployment(this Mock<IKubernetes> mock, string name, string ns, V1Deployment dep)
        => mock.Setup(x => x.AppsV1.ReadNamespacedDeploymentWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Deployment> { Body = dep });

    public static void SetupReadDeploymentThrows(this Mock<IKubernetes> mock, string name, string ns, Exception ex)
        => mock.Setup(x => x.AppsV1.ReadNamespacedDeploymentWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupCreateDeployment(this Mock<IKubernetes> mock, string ns)
        => mock.Setup(x => x.AppsV1.CreateNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Deployment>(), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Deployment> { Body = new V1Deployment() });

    public static void SetupCreateDeploymentThrows(this Mock<IKubernetes> mock, string ns, Exception ex)
        => mock.Setup(x => x.AppsV1.CreateNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Deployment>(), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupReplaceDeployment(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.ReplaceNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Deployment>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Deployment> { Body = new V1Deployment() });

    public static void SetupReplaceDeploymentThrows(this Mock<IKubernetes> mock, string name, string ns, Exception ex)
        => mock.Setup(x => x.AppsV1.ReplaceNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Deployment>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupDeleteDeployment(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.DeleteNamespacedDeploymentWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<V1DeleteOptions?>(), It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Status> { Body = new V1Status() });

    public static void SetupDeleteDeploymentThrows(this Mock<IKubernetes> mock, string name, string ns, Exception ex)
        => mock.Setup(x => x.AppsV1.DeleteNamespacedDeploymentWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<V1DeleteOptions?>(), It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupReadDeploymentScale(this Mock<IKubernetes> mock, string name, string ns, int currentReplicas)
        => mock.Setup(x => x.AppsV1.ReadNamespacedDeploymentScaleWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Scale>
            {
                Body = new V1Scale { Spec = new V1ScaleSpec { Replicas = currentReplicas } }
            });

    public static void SetupReplaceDeploymentScale(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.ReplaceNamespacedDeploymentScaleWithHttpMessagesAsync(
                It.IsAny<V1Scale>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Scale> { Body = new V1Scale() });

    public static void SetupReplaceDeploymentScaleThrows(this Mock<IKubernetes> mock, string name, string ns, Exception ex)
        => mock.Setup(x => x.AppsV1.ReplaceNamespacedDeploymentScaleWithHttpMessagesAsync(
                It.IsAny<V1Scale>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    public static void SetupPatchDeployment(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.PatchNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Patch>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Deployment> { Body = new V1Deployment() });

    public static void SetupPatchDeploymentThrows(this Mock<IKubernetes> mock, string name, string ns, Exception ex)
        => mock.Setup(x => x.AppsV1.PatchNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Patch>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    // ---- apps/v1: StatefulSet ----

    public static void SetupListStatefulSets(this Mock<IKubernetes> mock, params V1StatefulSet[] items)
        => mock.Setup(x => x.AppsV1.ListStatefulSetForAllNamespacesWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1StatefulSetList>
            {
                Body = new V1StatefulSetList { Items = items.ToList() }
            });

    public static void SetupReadStatefulSet(this Mock<IKubernetes> mock, string name, string ns, V1StatefulSet sts)
        => mock.Setup(x => x.AppsV1.ReadNamespacedStatefulSetWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1StatefulSet> { Body = sts });

    public static void SetupCreateStatefulSet(this Mock<IKubernetes> mock, string ns)
        => mock.Setup(x => x.AppsV1.CreateNamespacedStatefulSetWithHttpMessagesAsync(
                It.IsAny<V1StatefulSet>(), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1StatefulSet> { Body = new V1StatefulSet() });

    public static void SetupReplaceStatefulSetScale(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.ReplaceNamespacedStatefulSetScaleWithHttpMessagesAsync(
                It.IsAny<V1Scale>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Scale> { Body = new V1Scale() });

    public static void SetupReadStatefulSetScale(this Mock<IKubernetes> mock, string name, string ns, int currentReplicas)
        => mock.Setup(x => x.AppsV1.ReadNamespacedStatefulSetScaleWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Scale>
            {
                Body = new V1Scale { Spec = new V1ScaleSpec { Replicas = currentReplicas } }
            });

    public static void SetupPatchStatefulSet(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.PatchNamespacedStatefulSetWithHttpMessagesAsync(
                It.IsAny<V1Patch>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1StatefulSet> { Body = new V1StatefulSet() });

    public static void SetupDeleteStatefulSet(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.DeleteNamespacedStatefulSetWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<V1DeleteOptions?>(), It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Status> { Body = new V1Status() });

    // ---- apps/v1: DaemonSet ----

    public static void SetupListDaemonSets(this Mock<IKubernetes> mock, params V1DaemonSet[] items)
        => mock.Setup(x => x.AppsV1.ListDaemonSetForAllNamespacesWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1DaemonSetList>
            {
                Body = new V1DaemonSetList { Items = items.ToList() }
            });

    public static void SetupReadDaemonSet(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.ReadNamespacedDaemonSetWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1DaemonSet>
            {
                Body = new V1DaemonSet { Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns } }
            });

    public static void SetupCreateDaemonSet(this Mock<IKubernetes> mock, string ns)
        => mock.Setup(x => x.AppsV1.CreateNamespacedDaemonSetWithHttpMessagesAsync(
                It.IsAny<V1DaemonSet>(), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1DaemonSet> { Body = new V1DaemonSet() });

    public static void SetupPatchDaemonSet(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.PatchNamespacedDaemonSetWithHttpMessagesAsync(
                It.IsAny<V1Patch>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1DaemonSet> { Body = new V1DaemonSet() });

    public static void SetupDeleteDaemonSet(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.DeleteNamespacedDaemonSetWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<V1DeleteOptions?>(), It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Status> { Body = new V1Status() });

    // ---- apps/v1: ReplicaSet ----

    public static void SetupListReplicaSets(this Mock<IKubernetes> mock, params V1ReplicaSet[] items)
        => mock.Setup(x => x.AppsV1.ListReplicaSetForAllNamespacesWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1ReplicaSetList>
            {
                Body = new V1ReplicaSetList { Items = items.ToList() }
            });

    public static void SetupReadReplicaSet(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.ReadNamespacedReplicaSetWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1ReplicaSet>
            {
                Body = new V1ReplicaSet { Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns } }
            });

    public static void SetupCreateReplicaSet(this Mock<IKubernetes> mock, string ns)
        => mock.Setup(x => x.AppsV1.CreateNamespacedReplicaSetWithHttpMessagesAsync(
                It.IsAny<V1ReplicaSet>(), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1ReplicaSet> { Body = new V1ReplicaSet() });

    public static void SetupReadReplicaSetScale(this Mock<IKubernetes> mock, string name, string ns, int currentReplicas)
        => mock.Setup(x => x.AppsV1.ReadNamespacedReplicaSetScaleWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Scale>
            {
                Body = new V1Scale { Spec = new V1ScaleSpec { Replicas = currentReplicas } }
            });

    public static void SetupReplaceReplicaSetScale(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.ReplaceNamespacedReplicaSetScaleWithHttpMessagesAsync(
                It.IsAny<V1Scale>(), It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Scale> { Body = new V1Scale() });

    public static void SetupDeleteReplicaSet(this Mock<IKubernetes> mock, string name, string ns)
        => mock.Setup(x => x.AppsV1.DeleteNamespacedReplicaSetWithHttpMessagesAsync(
                It.Is<string>(n => n == name), It.Is<string>(n => n == ns),
                It.IsAny<V1DeleteOptions?>(), It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Status> { Body = new V1Status() });
}
