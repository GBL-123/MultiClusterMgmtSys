# Proposal

## Why

节点是全站唯一没有任何写操作的核心 K8s 资源:管理员面对一台需要维护的节点(重启机器、升级内核、换磁盘)只能离开系统去 kubectl 敲 `cordon/drain`,而节点页其实已经读出了 `Unschedulable` 字段只是无人能改。补齐「封锁/解封/排空」三个标准的节点维护操作,让节点维修的完整生命周期在系统内闭环。

## What Changes

- 新增三个节点维护操作,入口在节点列表行操作列与节点详情工具栏:
  - **封锁(cordon)**:PATCH `spec.unschedulable=true`,节点不再接受新 Pod 调度,现有 Pod 原地不动
  - **解封(uncordon)**:PATCH `spec.unschedulable=false`,恢复调度
  - **排空(drain)**:预检列出该节点全部 Pod → 强确认(明示将迁移的负载)→ 自动封锁自身 → 逐 Pod 发 policy/v1 Eviction,按尽力迁移语义汇总「成功 n / 跳过 m / 阻塞 k」
- drain 语义边界:DaemonSet Pod 自动跳过(原地重生无意义);无控制器裸 Pod 默认拒绝驱逐(第一版不提供 force);被 PDB 拦截(429)的 Pod 记为「被策略阻塞」不中断后续;不等节点 Pod 清零,驱逐请求结果即时返回
- 节点状态可见化:列表与详情对 `Unschedulable=true` 的节点展示「已封锁」小徽标(不动 Ready 三态徽章语义)
- 权限:节点为集群级资源,不适用 `k8s-resource-ownership`;三操作一律 Admin 门控(与命名空间建删同级),服务层强制
- 审计:类别「节点」,新增操作枚举「封锁/解封/排空」,目标含节点名与集群;排空描述带计数
- 排空/封锁过程中显示进度(已完成/总数,复用「刷新全部」模式);不进本期:节点污点(taints)编辑

## Capabilities

### New Capabilities

- `node-maintenance`:节点封锁/解封/排空三个维护操作的服务契约与页面行为:K8s 调用口径(PATCH/Eviction)、drain 预检与逐 Pod 进度语义、PDB/裸 Pod/DaemonSet 边界、Admin 门控与审计

### Modified Capabilities

- `nodes-page`:节点列表行新增维护操作列扩展与「已封锁」徽标展示;节点详情工具栏增加维护操作入口
- `audit-log`:审计操作枚举新增「封锁/解封/排空」与节点 операций记录口径

## Impact

- **Application**:`ClusterNodeService`(或新 `NodeMaintenanceService`)新增 Cordon/Uncordon/Drain 方法,`Requests/` 增加 drain 请求对象;`Domain/Enums/AuditAction.cs` 增加三个操作枚举
- **Infrastructure**:经既有 `IClusterClientCache` 取客户端,eviction 走 policy/v1 API(需确认 KubernetesClient 19 的 `CreateNamespacedEviction*WithHttpMessagesAsync` 签名)
- **Web**:`Components/Nodes/` 列表行/详情工具栏加操作入口 + 确认对话框 + 逐 Pod 进度;`Shared/` 状态徽标组件
- **测试**:`ClusterNodeServiceTests` 扩展 + 新服务测试 + bUnit 节点页操作/徽标接线;K8sMocks 增加 eviction mock 段
- 不涉及数据库 schema、无新表;审计沿用 `AuditLog`(类别节点已存在)
