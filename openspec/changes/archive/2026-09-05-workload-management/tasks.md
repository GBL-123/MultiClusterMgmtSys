# tasks — workload-management

## 1. 枚举与审计基础

- [x] 1.1 新增 `Common/Enums/WorkloadKind.cs`(Deployment/StatefulSet/DaemonSet/ReplicaSet)及能力矩阵常量(扩缩容适用前三者中 Dep/STS/RS、重启适用 Dep/STS/DS,ReplicaSet 无重启、DaemonSet 无扩缩)
- [x] 1.2 扩展 `AuditCategory.Workload`、`AuditAction.Scale`、`AuditAction.Restart`,并同步审计日志页类别/操作的中文显示映射(枚举 → 中文 switch 补分支)

## 2. ViewModels 与映射(状态算法核心)

- [x] 2.1 新增 `ViewModels/WorkloadListViewModel.cs`:Name、Namespace、Kind、就绪度(Ready/Desired 计数与 `n/m` 文本)、滚动状态(RolloutState:Ready/Rolling/NotReady)、CreatedAt
- [x] 2.2 新增 `ViewModels/WorkloadDetailViewModel.cs`:Name、Namespace、Uid、CreatedAt、Yaml、逐型状态摘要(副本/更新/条件)
- [x] 2.3 新增 `ViewModels/Mappings/WorkloadMappingExtensions.cs`:四型 `ToWorkloadListViewModel()` + `ToWorkloadDetailViewModel()`,实现逐型三态与就绪度算法(D5/D7 的字段映射表)

## 3. WorkloadService

- [x] 3.1 新增 `Services/WorkloadService.cs` 列表与详情:四型 `List*Async(clusterId, ns?)` / `Get*Async(clusterId, ns, name)`,复用 `BuildConfig`、`K8sExceptionMapper.Translate`、LogWarning 模式
- [x] 3.2 YAML 新建/编辑/删除:逐型 `Create*FromYamlAsync`(反序列化 + `metadata.namespace` 必填校验)、`Update*FromYamlAsync`(方案 A:读最新对象,仅覆盖 `spec`,带最新 resourceVersion replace)、`Delete*Async`;解析失败抛 `ValidationException("YAML 格式错误:…")`
- [x] 3.3 扩缩容与滚动重启:`Scale*Async`(scale 子资源 Read→改 Replicas→Replace;仅 Dep/STS/RS)、`Restart*Async`(StrategicMergePatch 写 `spec.template.metadata.annotations["kubectl.kubernetes.io/restartedAt"]`;仅 Dep/STS/DS)
- [x] 3.4 审计接线:成功路径逐方法 `AuditService.LogAsync`(类别 Workload;操作 Create/Update/Delete/Scale/Restart,目标含 `ns/name @ 集群名`,扩缩容含 `→ n`)

## 4. 共享组件

- [x] 4.1 `Components/Workloads/Shared/WorkloadListFilterBar.razor`:命名空间下拉 + 名称搜索 + 查询/重置(对齐 ConfigMapListFilterBar 交互)
- [x] 4.2 `Components/Workloads/Shared/WorkloadListTable.razor`:统一行渲染(name `.link-primary`、`n/m` `.font-mono`、`status-badge` 三态、创建时间),操作列按能力矩阵条件渲染([扩缩][重启][⋯→删除]),空态/加载态遵循设计系统
- [x] 4.3 `Components/Workloads/Shared/WorkloadScaleDialog.razor`(当前副本数预填)与 `CreateWorkloadDialog.razor`(逐型 YAML 模板)
- [x] 4.4 `Components/Workloads/Shared/WorkloadDetailToolbar.razor` 与 YAML 视图卡(复用 ConfigMap YAML 卡片模式,plain `textarea.yaml-textarea`)

## 5. 页面与导航

- [x] 5.1 四个列表页 `Components/Workloads/Pages/`:Deployments / StatefulSets / DaemonSets / ReplicaSets,路由 `/workloads/{kind}` + `/workloads/{kind}/{ClusterId:int}`,接线 ClusterSelectionState、过滤、行内操作、Admin 新建按钮、不可达/未选集群态
- [x] 5.2 四个详情页:路由 `/workloads/{kind}/{ClusterId:int}/{Namespace}/{Name}`,YAML 视图 + 信息卡 + toolbar(刷新/编辑 YAML/扩缩/重启/删除,按矩阵渲染),不存在空态
- [x] 5.3 共享 YAML 编辑页:`/workloads/{kind}/{ClusterId:int}/{Namespace}/{Name}/yaml`,按 kind 路由参数分派对应 service 方法(方案 A 保存)
- [x] 5.4 `Components/Layout/Drawer.razor` 新增「工作负载」`MudNavGroup`(Prefix match),含四个子 NavLink

## 6. 测试与验证

- [x] 6.1 服务层 UT(YAML 链路):解析失败 → `ValidationException` 中文消息;新建缺 namespace → 校验异常;编辑仅覆盖 spec(mock 断言 replace 载荷不含 status/metadata 覆盖)
- [x] 6.2 服务层 UT(异常翻译):`TestServices.ThrowingFactory()` + `KubernetesException(V1Status{Code=404/409/403})` → NotFoundException/ConflictException/PermissionException,含中文操作上下文
- [x] 6.3 服务层 UT(scale/restart/审计):scale 走 scale 子资源调用形态、restart patch 载荷含 restartedAt 注解、成功后 `AuditService.LogAsync` 参数断言
- [x] 6.4 三态/就绪度算法 UT:构造假 `V1Deployment`/`V1StatefulSet`/`V1DaemonSet`/`V1ReplicaSet`,覆盖就绪/滚动中/未就绪与 generation 差场景
- [x] 6.5 bUnit 接线契约:`WorkloadListTable`(行渲染、事件参数、自有 CSS 类)+ 能力矩阵条件渲染(DaemonSet 行无扩缩、ReplicaSet 行无重启),禁断言 `.mud-*` 内部 DOM
- [x] 6.6 全量验证:`dotnet build MultiClusterMgmtSys.slnx` 0 错误 + `dotnet test MultiClusterMgmtSys.Tests` 全绿
