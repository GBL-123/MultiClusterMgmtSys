# workload-management

## Why

系统目前对 Kubernetes 工作负载(Deployment / StatefulSet / DaemonSet / ReplicaSet)没有任何管理能力——已覆盖的资源管理页只有集群、节点、配置三类。日常运维中最高频的动作(扩缩容、滚动重启、查看滚动进度)只能通过外部 kubectl 或 YAML 工具完成,管理闭环缺失。

## What Changes

- 新增「工作负载」侧边导航组(MudNavGroup),下拉展开 4 个独立管理页:
  - `/workloads/deployments`(部署管理)
  - `/workloads/statefulsets`(有状态应用)
  - `/workloads/daemonsets`(守护进程)
  - `/workloads/replicasets`(副本集)
- 每个列表页:集群侧栏选择 + 命名空间/名称过滤 + 统一列表行(Name 链接、就绪 n/m、滚动状态、创建时间、操作列)
- 列表行内操作:`[扩缩容]` `[重启]` `[⋯→删除]`(按类型可用性矩阵条件渲染);name 链接进入详情
- 每种类型一个详情页:YAML 视图/编辑、扩缩容、重启、删除(低频动作住详情 toolbar)
- 滚动指示:就绪 / 滚动中 / 异常 三态,数据取自各类型 status 字段
- 新增 `WorkloadService`:per-kind 的 list / get / create-from-yaml / update-from-yaml / delete / scale / restart;复用 `BuildConfig`、`K8sExceptionMapper`、`AuditService` 现有模式
- YAML 编辑冲突策略(方案 A):保存时读最新对象,只覆盖 `spec`,replace 携带最新 resourceVersion;`status` 永不触碰
- 审计扩展:`AuditCategory.Workload`、`AuditAction.Scale` / `AuditAction.Restart`
- 权限:所有写操作 Admin 门控,查看对所有登录用户开放

## Capabilities

### New Capabilities

- `workload-management`: 工作负载(apps/v1 四件套)的管理页与服务:列表、详情、YAML 新建/编辑、删除、扩缩容、滚动重启、滚动指示、按类型的操作可用性矩阵

### Modified Capabilities

- `audit-log`: 「审计事件写入」需求新增工作负载操作(创建、修改、删除、扩缩容、重启);「审计记录内容」需求的类别枚举增加"工作负载",操作枚举增加"扩缩容"、"重启"

## Impact

- **新增**: `Services/WorkloadService.cs`、`ViewModels/Workload*.cs` + `ViewModels/Mappings/WorkloadMappingExtensions.cs`、`Requests/`(如需)、`Components/Workloads/Pages/*`(4 列表页 + 4 详情页)、`Components/Workloads/Shared/*`(FilterBar / ListTable / ScaleDialog / CreateDialog / YAML 卡片 / Toolbar)
- **修改**: `Components/Layout/Drawer.razor`(新增 NavGroup)、`Common/Enums/AuditCategory.cs`(+Workload)、`Common/Enums/AuditAction.cs`(+Scale/Restart)
- **无 DB schema 变更**:AuditLog 的 Category/Action 以 int 枚举存储,扩展枚举值不需要迁移
- **测试**: `MultiClusterMgmtSys.Tests` 新增服务层测试(K8s 异常翻译链、状态算法)+ bUnit 接线契约测试
- K8s 客户端 API 面:`AppsV1` 的 Deployment/StatefulSet/DaemonSet/ReplicaSet 读写 + scale 子资源 + pod-template 注解 patch
