# namespace-management

## Why

系统已覆盖集群、节点、配置(ConfigMap)、工作负载与服务的统一管理,但 Kubernetes Namespace 本身只能在各页面的命名空间下拉里被动"看到",无法查看状态、创建或清理。命名空间是资源归属与隔离的第一入口,当前只能去各集群手敲 kubectl,与"多集群统一管控"的定位不符。

## What Changes

- 新增「命名空间管理」功能:侧边导航顶层入口(位于「节点管理」之后),列表页 `/namespaces`(含 `/namespaces/{ClusterId:int}`),详情页 `/namespaces/{ClusterId:int}/{Name}`,沿用集群选择侧栏与 `ClusterSelectionState` 会话内集群记忆。
- 列表页:集群状态徽章、刷新、名称客户端搜索、Admin 可见的「新建命名空间」;表格列为名称(`.link-primary` 进详情)、状态徽章(Active=在线、Terminating=未知、其余=未知)、标签数、创建时间、操作(详情/删除)。
- 详情页:工具栏 + `MudTabs`(YAML 只读视图 | 标签与注解只读表),遵循 `detail-page-tabs` 与 `ui-theme` 契约;标签数与注解数保持只读展示。
- 创建:Admin 通过 YAML 对话框创建,模板外置为 `wwwroot/templates/namespace/default.yaml`;提交前反序列化为 `V1Namespace` 并校验 `metadata.name` 必填,K8s 同名冲突(409)提示「同名命名空间已存在」。
- 删除:Admin;确认对话框提示将连带删除其中所有资源;服务端硬保护 `default` 与 `kube-` 前缀命名空间,直接抛中文 `ValidationException`,UI 同步禁用这些行的删除入口。
- 审计:新增 `AuditCategory.Namespace` 枚举值,命名空间创建/删除成功写审计(目标描述含名称与集群名)。
- 有意不做:准入资源计数(需要逐命名空间额外 API 调用)、ResourceQuota/LimitRange、标签/注解编辑、删除时的 finalizer 处理。
- 不改动 ConfigMapService/WorkloadService/SvcService 中既有的 `GetNamespacesAsync` 及其页面与测试(收敛重复代码留作后续独立改动)。

## Capabilities

### New Capabilities

- `namespace-management`:K8s Namespace 的跨集群管理能力——导航入口、列表(状态/标签数/创建时间 + 名称搜索)、详情(YAML 与标签/注解只读 tab)、YAML 创建(模板 + name 校验 + 冲突提示)、删除(系统命名空间硬保护 + 确认对话框 + 审计记录)。

### Modified Capabilities

- `audit-log`:审计事件写入与审计记录内容两条需求的枚举扩展——类别新增「命名空间」,事件新增命名空间创建、删除。

## Impact

- **新增**:`Services/NamespaceService.cs`(服务层)、`Requests/NamespaceKeyRequest.cs`、`Requests/NamespaceCreateRequest.cs`、`ViewModels/NamespaceListViewModel.cs`、`ViewModels/NamespaceDetailViewModel.cs` + `ViewModels/Mappings/NamespaceMappingExtensions.cs`、`Components/Namespaces/Pages|Shared`(列表/详情页与共享组件)、`wwwroot/templates/namespace/default.yaml`、`Common/Enums/AuditCategory.cs`(新枚举值 `Namespace = 8`)。
- **修改**:`Components/Layout/Drawer.razor`(顶层「命名空间管理」入口)、`Program.cs`(注册 `NamespaceService`)。
- **测试**:`MultiClusterMgmtSys.Tests` 新增 `NamespaceServiceTests`(列表/详情/创建/删除/保护/异常翻译)、`K8sMocks` 扩展(Read/Create/Delete Namespace 的 setup 与 throws)、页面 bUnit 测试(接线契约 + 侧栏/详情 tab/写操作门控)。
- 不涉及数据库 schema 变更(`EnsureCreated` 无需重建,新增枚举值不影响存储);不涉及新增 NuGet 依赖(KubernetesClient 19 已提供 CoreV1 命名空间 API)。
