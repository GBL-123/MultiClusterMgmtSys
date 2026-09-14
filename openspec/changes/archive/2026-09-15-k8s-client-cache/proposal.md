# K8s 客户端缓存

## Why

当前每个服务方法在每次 K8s 调用时都执行 `KubernetesClientConfig.Build` + 工厂新建 `IKubernetes`(`ConfigMapService.cs` 等 50 处调用点),意味着每次页面交互都要付一次完整的 TCP+TLS 握手(100~300ms),且连接池无法复用;`using var client` 在方法末尾即释放。引入按集群缓存客户端后,同一集群的后续调用零握手直取连接池,是全站 K8s 交互延迟收益最大的一次改动。

## What Changes

- 新增 singleton `IClusterClientCache`(实体传入、同步签名):按集群 Id 缓存 `IKubernetes`,miss 时走现有 `Func<KubernetesClientConfiguration, IKubernetes>` 工厂创建(工厂契约与注册原样保留)。
- 失效采用**凭据指纹自愈**:每次调用对实体的 `ConnectionType / KubeConfig / ApiServer / Token / SkipTlsVerify` 求哈希指纹,指纹变化即重建客户端并替换缓存条目——不依赖任何编辑路径显式通知(含 `ProbeAsync` 回填 `ApiServer` 这类隐性变更)。
- 生命周期:`IMemoryCache` 承载,滑动过期(10~15 分钟)+ 驱逐回调中 dispose 释放连接池;容量上限远超实际集群规模,压力驱逐在现实规模下不触发。
- 全部 K8s 服务(`ConfigMapService`、`SvcService`、`WorkloadService`、`NamespaceService`、`ClusterNodeService`)及 `ClusterService.ProbeAsync` 的客户端获取行由 `using var client = clientFactory(config)` 机械替换为 `clientCache.GetOrCreate(entity)`。
- 超时契约不变:miss 路径仍经 `KubernetesClientConfig.Build` 施加 10s 超时(`kubernetes-call-timeout` spec 不受影响)。

## Capabilities

### New Capabilities
- `k8s-client-cache`: 按集群复用 Kubernetes 客户端的缓存契约——客户端创建与复用时机、凭据指纹失效语义、生命周期与释放、探测路径复用、工厂契约保留与测试兼容。

### Modified Capabilities

## Impact

- **代码**:`Services/ClusterClientCache.cs`(新增,约 80 行)+ 6 个 K8s Service 的客户端获取行机械替换 + `Program.cs` 一行注册;不改动 `KubernetesClientConfig`、工厂注册与全部 K8s mock 基建。
- **测试**:现有 600 个测试预期不动(mock 工厂即 miss 路径);新增缓存自身单测(命中/指纹重建/驱逐释放/探测并发复用),实现后以全量回归为硬验证。
- **运维语义**:同集群并发调用共享同一客户端(线程安全);集群删除后条目自然空闲过期;驱逐竞态窗口在滑动过期 + 10s 请求超时下实际为 0,即使命中也走既有 `K8sExceptionMapper` → `ExceptionPresenter` 降级链路,无新增故障类别。
- **范围外**(各自另列):凭据加密落盘(#5)、`GetByIdAsync` 凭据投影裁剪、SQLite WAL/审计保留(#4)。
