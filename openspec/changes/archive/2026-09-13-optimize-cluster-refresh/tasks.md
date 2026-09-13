## 1. 统一 Kubernetes 调用超时

- [x] 1.1 新增公共配置构建(由 `ClusterInfo` 生成 `KubernetesClientConfiguration`,统一设 `HttpClientTimeout = 10s`,含中文 XML 注释),替换 ClusterService/ClusterNodeService/ConfigMapService/NamespaceService/SvcService/WorkloadService 六处私有 `BuildConfig`;验证 = `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 1.2 在服务测试中用捕获 `KubernetesClientConfiguration` 的工厂断言 kubeconfig 与 Token 两种路径的超时值均为 10s;验证 = 新增测试通过
- [x] 1.3 跑既有服务测试确认配置收拢无回归(失败翻译链路、优雅降级分支不变);验证 = 相关测试类全绿

## 2. 有界并发探测

- [x] 2.1 新增 `ClusterRepository.GetAllForSyncAsync()`(跟踪查询、无需 Include,刷新只读写标量字段);验证 = 仓储单测断言返回全部集群且可跟踪更新
- [x] 2.2 重构 `RefreshAllClustersStatusAsync` 为三段式(取数 → `Parallel.ForEachAsync` 并发探测,`MaxDegreeOfParallelism = 4` → 串行 `UpdateAsync` + 状态翻转审计),进度用 `Interlocked` 计数;保持 `succeeded` 语义与审计文案;验证 = 既有 `RefreshAllClustersStatusAsync_counts_and_audits_status_changes` 等用例全绿
- [x] 2.3 新增确定性并发测试(`TaskCompletionSource` 门闩:两个集群互相等待对方已开始,串行实现会超时失败)+ 最大在途数 ≤ 4 断言;验证 = 新增测试通过

## 3. 停机取消

- [x] 3.1 `RefreshAllClustersStatusAsync` 增加 `CancellationToken`(默认 `None`),`ProbeAsync` 接收 token 并在常规 catch 前用 `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` 区分停机取消与客户端超时;取消时先持久化已完成结果再上抛;验证 = 新增「预取消令牌不发起探测、不置 Offline、不写审计」测试通过
- [x] 3.2 `ClusterSyncBackgroundService.RunOnceAsync` 把 `stoppingToken` 透传给刷新服务;验证 = 后台服务测试断言取消令牌到达服务层(或传入已取消令牌时快速返回)
- [x] 3.3 对照 spec 场景复核取消语义(状态/版本/节点数/`LastCheckedAt` 不变、无翻转审计);验证 = 场景对应用例通过

## 4. 回归与验收

- [x] 4.1 运行 `dotnet build MultiClusterMgmtSys.slnx` 与 `dotnet test MultiClusterMgmtSys.Tests`,确认 0 错误、全部测试通过且数量基线不降
- [x] 4.2 对照 `specs/kubernetes-call-timeout/spec.md` 与 `specs/cluster-scheduled-sync/spec.md` 的场景逐条确认覆盖;验证 = 每条场景均有对应实现或测试
