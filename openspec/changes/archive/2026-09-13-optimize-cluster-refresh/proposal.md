# optimize-cluster-refresh

## Why

集群刷新在遇到不可达(连接黑洞)集群时极慢:每个 Kubernetes REST 调用默认有 100 秒超时,整轮刷新又是逐个串行探测,最坏时长约为「不可达集群数 × 100 秒」。默认同步间隔只有 5 分钟,轮次经常追不上间隔,手动「刷新所有集群」还会被全局互斥锁堵在后台轮次后面。此外,后台停机信号(`stoppingToken`)在进入整轮刷新后不再生效,停机要等当前集群探测完成。

## What Changes

- 所有经客户端工厂创建的 Kubernetes REST 调用统一施加 **10 秒超时上限**(覆盖集群探测、节点/工作负载/ConfigMap/Service/命名空间等全部读写),超时按「集群不可达」语义降级;6 处重复的客户端配置构建收拢为一处。
- 「刷新所有集群」由串行探测改为**有界并发**(最多同时 4 个):先取数、再并发探测、最后串行落库与写审计;单集群失败隔离、进度回调、`succeeded` 语义与审计文案保持不变。单集群手动刷新行为不变。
- 「刷新所有集群」支持**取消**:后台停机时透传 `stoppingToken`,取消后停止发起新探测、快速退出;取消不得把集群误标为 `Offline`,也不得写状态翻转审计。
- 不新增 NuGet 依赖、不改数据库 schema、不改页面路由与交互。

## Capabilities

### New Capabilities

- `kubernetes-call-timeout`:所有 Kubernetes API 调用的统一超时上限与降级语义——单次调用不超过 10 秒,超时在探测场景置 `Offline`、在页面数据加载场景表现为集群不可达业务异常。

### Modified Capabilities

- `cluster-scheduled-sync`:后台定时刷新的「逐个执行探测」改为有界并发;新增「整轮同步支持停机取消」需求(取消区别于探测失败,不落库、不写审计)。

## Impact

- **修改**:`Services/ClusterService.cs`(刷新编排三段式 + 并发 + 取消;探测区分取消与超时)、`Services/ClusterSyncBackgroundService.cs`(透传 `stoppingToken`)、`Data/Repositories/ClusterRepository.cs`(新增全量同步取数方法)、6 个服务的私有客户端配置构建(ClusterService/ClusterNodeService/ConfigMapService/NamespaceService/SvcService/WorkloadService)收拢为公共配置工厂。
- **测试**:`MultiClusterMgmtSys.Tests/Services/ClusterServiceTests.cs`(超时值断言、并发证明、取消语义)、`ClusterSyncBackgroundServiceTests.cs`(透传与取消)、各服务测试构造函数不变。
- 不涉及数据库 schema、NuGet 依赖、请求/响应契约;新建/编辑集群的即时探测与所有列表页加载自动受益于统一超时,无需单独改动。
