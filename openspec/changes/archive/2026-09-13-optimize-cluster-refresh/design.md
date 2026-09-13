# optimize-cluster-refresh — Design

## Context

现状(见 proposal.md - Why):KubernetesClient 的 REST 默认超时为 100 秒(`AbstractKubernetes.HttpClientTimeout`);`RefreshAllClustersStatusAsync` 逐个 `foreach` 探测(`ClusterService.cs:211`),单轮最坏 ≈ 不可达集群数 × 100 秒;后台停机信号在 `RunOnceAsync` 处断链(`ClusterSyncBackgroundService.cs:49`,参数从未使用)。

约束:

- `ClusterService` 是 scoped,持有一个 `ApplicationDbContext`(经 `ClusterRepository`);EF Core 上下文不支持并发使用,并发探测不能让多个任务同时碰仓储/审计。
- 客户端配置构建在 6 个服务里各有一份私有 `BuildConfig`(ClusterService/ClusterNodeService/ConfigMapService/NamespaceService/SvcService/WorkloadService)。
- 服务单元测试直接 `new XxxService(repo, audit, logger, clientFactory)` 构造,构造函数加参数会波及 7 个测试文件。
- `ProbeAsync` 目前 `catch (Exception)` 一律置 `Offline`;超时与外部取消都表现为 `OperationCanceledException` 派生类,必须区分。
- 测试基线 513,历史用例覆盖失败隔离与状态翻转审计,语义不能破。

## Goals / Non-Goals

**Goals:**

- 单集群探测最坏时长从 100 秒降到约 10 秒,并统一作用于所有 K8s 调用。
- 整轮刷新时长与集群数解耦(有界并发),挂起/不可达集群不拖慢其余集群。
- 停机可取消整轮刷新,且取消不被误判为探测失败。
- 保持现有对外行为:刷新结果语义、审计文案、进度回调、页面契约不变。

**Non-Goals:**

- 不做离线退避/逐集群调度(留待后续,N 很大时再考虑)。
- 不把超时做成运行时配置(本轮写死 10 秒;将来需要再加配置入口)。
- 不改单集群手动刷新的交互、不改 UI、不改 DB schema。
- 不重构 6 个服务的公开契约(仅收拢私有配置构建)。

## Decisions

### 超时落点:公共静态配置工厂 + 10 秒常量

新增 `Services/KubernetesClientConfig.cs`(静态类,`internal` 或 public + 中文 XML 注释),提供「由 `ClusterInfo` 构建 `KubernetesClientConfiguration` 并设 `HttpClientTimeout = 10s`」的唯一入口;6 个服务的私有 `BuildConfig` 全部删除并改调它。

- 备选 A2(IOptions/appsettings 注入 6 个服务):构造函数与 7 个测试文件全要改,本轮收益不足,否决。
- 备选 A3(只在 `Program.cs` 工厂改 config):服务测试捕获不到超时值,6 份重复代码保留,行为藏在 DI 注册里,否决。
- 同时否决 `FirstMessageHandlerSetup` 里单独设 `ConnectTimeout`:10 秒总上限已覆盖黑洞场景,额外区分连接/响应超时没有行为收益。

### 并发编排:取数 → 并发探测 → 串行落库/审计

`RefreshAllClustersStatusAsync` 改为三段:

1. **取数(串行,DbContext)**:新增 `ClusterRepository.GetAllForSyncAsync()`(跟踪查询、无需 Include,刷新只读写标量字段),并记录每个实体的 `previousStatus`。
2. **探测(有界并发,不碰 DbContext)**:`Parallel.ForEachAsync(entities, new ParallelOptions { MaxDegreeOfParallelism = 4 }, ...)` 调用现有 `ProbeAsync`;`clientFactory` 是单例、每个集群独立建 client,无共享可变状态;进度用 `Interlocked.Increment` 计数后 `progress?.Report`。
3. **落库与审计(串行)**:对每个已探测实体 `repo.UpdateAsync`(与现状一致,逐条 SaveChanges);仅当 `previousStatus != Status` 时写审计(文案不变)。

- 并发度固定 4:全量刷新入口在 scoped 服务,无法像后台任务那样为每个探测开新 DI scope;固定 4 即可覆盖典型集群规模,又限制 kubeconfig `exec` 凭据插件的并发进程数。
- 备选「每个探测独立 DI scope」:能并发用仓储但要重构刷新入口的作用域管理,复杂度不成比例,否决。
- 备选「`Task.WhenAll` + `SemaphoreSlim`」:与 `Parallel.ForEachAsync` 等价,后者自带调度与取消传播,采用后者。
- 单集群 `RefreshClusterStatusAsync` 与新建/编辑集群的即时探测保持原逻辑,只受益于统一超时。

### 取消语义:区分外部取消与超时

`RefreshAllClustersStatusAsync` 增加可选 `CancellationToken`(默认 `None`,UI 调用不受影响);`ProbeAsync` 同步接收该 token,并在常规 `catch (Exception)` 之前加一条:

```csharp
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    throw; // 停机取消:不置 Offline、不更新时间戳
}
```

超时(客户端内部取消)不满足 `when`,仍落入原有 catch,按失败置 `Offline`。`Parallel.ForEachAsync` 在取消时停止调度新探测并抛出 `OperationCanceledException`;服务在 `finally` 释放 `syncGate` 前先持久化**已完成**的探测结果,然后按原样上抛取消。`ClusterSyncBackgroundService.RunOnceAsync` 把 `stoppingToken` 透传给服务。

### 测试策略

- **超时**:服务测试注入捕获 `KubernetesClientConfiguration` 的工厂,断言 kubeconfig/Token 两种路径都得到 `HttpClientTimeout == 10s`(不需要真实等待超时,避免计时脆弱)。
- **并发**:用 `TaskCompletionSource` 门闩做确定性证明——两个集群的 mock 探测都要求「对方已开始」才放行;串行实现会死锁(测试超时失败),并发实现能双双通过。另加最大在途计数断言 ≤ 4。
- **取消**:传入已取消的 token,断言未发起任何 K8s 探测、状态与审计保持原样;后台服务测试断言 `RunOnceAsync` 把 token 传下去。
- 既有「失败隔离/翻转审计/succeeded 计数」用例保持通过,不调整断言语义。

## Risks / Trade-offs

- [10 秒对「慢但活着」的链路(跨公网 VPN + 大列表)可能误判 `Offline`] → 单次 REST 调用而非页面聚合,10 秒余量充足;如后续出现误判,再引入配置化(已作为 Non-Goal 记录)。
- [并发探测可能同时触发 kubeconfig `exec` 凭据进程] → 并发度固定 4,可控;绝大多数登记方式为 Token/kubeconfig 静态凭据。
- [取消时部分集群本轮未刷新] → spec 明确该行为,且下一轮会补齐;取消不算失败,不写审计。
- [并发下 `Progress<T>` 回调线程变化] → Blazor 端 `Progress<T>` 捕获渲染器同步上下文,回调仍被调度回 UI 线程,行为不变。
- [收拢 6 处 `BuildConfig` 引入回归] → 改动是等价搬迁 + 加一行超时设置,由服务测试与既有 K8s 失败链路用例兜底。
