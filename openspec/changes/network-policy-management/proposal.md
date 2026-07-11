## Why

当前系统已支持集群、节点、ConfigMap 三种 k8s 资源类型的管理，但缺少 NetworkPolicy 管理能力。NetworkPolicy 是控制 Pod 间网络流量的关键安全资源，运维人员需要集中可视化管理多集群中的网络策略规则（Ingress/Egress），勿需在每个集群执行 `kubectl`。新增此功能补齐安全资源管理空白。

## What Changes

- 新增 `NetworkPolicyService`，复用现有 `BuildConfig` 模式连接 k8s，提供 NetworkPolicy 的列举、详情查看、创建、更新、删除操作。
- 新增 ViewModel 层：列表视图（名称、命名空间、策略类型、规则数量）、详情视图（Ingress/Egress 规则完整展示 + YAML 原始内容）、创建/更新视图（表单式输入 PodSelector + Ingress/Egress 规则 + YAML 直接编辑）。
- 新增双栏列表页 `/networkpolicies` + 详情页 `/networkpolicies/{ClusterId:int}/{Namespace}/{Name}`，遵循与 Nodes、ConfigMaps 一致的交互模式。
- 新增创建对话框 `CreateNetworkPolicyDialog`（colocated），支持表单填写，包括命名空间选择、Pod 选择器、策略类型、规则列表。
- 新增 YAML 编辑页 `EditNetworkPolicyYaml`，支持直接编辑并应用 YAML 内容。
- 添加左侧导航入口 `Drawer.razor` 与 `_Imports.razor` 命名空间引用。

## Capabilities

### New Capabilities

- `network-policy-management`: 多集群 NetworkPolicy 资源的列表查看、详情浏览、创建、更新（表单及 YAML 两种方式）、删除。

### Modified Capabilities

无。

## Impact

- **新增文件**：`Services/NetworkPolicyService.cs`、ViewModels（`NetworkPolicyListViewModel`、`NetworkPolicyDetailViewModel`、`NetworkPolicyCreateViewModel`、`NetworkPolicyUpdateViewModel`、`NetworkPolicyRuleViewModel`、`NetworkPolicyPortViewModel`、`NetworkPolicyPeerViewModel`）、`ViewModels/Mappings/NetworkPolicyMappingExtensions.cs`、页面（`NetworkPolicies.razor`、`NetworkPolicyDetail.razor`、`EditNetworkPolicyYaml.razor`、`CreateNetworkPolicyDialog.razor`）。
- **修改文件**：`Program.cs`（注册 `NetworkPolicyService`）、`Components/_Imports.razor`（添加 namespace）、`Components/Layout/Drawer.razor`（添加导航项）。
- **不改文件**：`AppDbContext`、`Models/`、`Daos/`、`ClusterService`、`AccountService`。
- **数据库**：无变更（NetworkPolicy 为实时 k8s 资源，不持久化到 SQLite）。
- **依赖**：`KubernetesClient` 19.0.2（已安装，使用内置 `k8s.Models.V1NetworkPolicy` 等类型）。
