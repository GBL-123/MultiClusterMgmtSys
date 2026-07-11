## Context

系统已建立成熟的 k8s 资源管理模式（ConfigMap、Node），包含 Service → ViewModel → 页面（双栏列表 + 详情 + 对话框 + YAML 编辑）四层架构。NetworkPolicy 管理将完全遵循此模式。

k8s `V1NetworkPolicy` 模型位于 `k8s.Models` 命名空间，核心字段：
- `Metadata`（名称、命名空间、UID、创建时间）
- `Spec.PodSelector`（LabelSelector，选择应用此策略的 Pod）
- `Spec.PolicyTypes`（`List<string>`，值为 `"Ingress"` 或 `"Egress"`）
- `Spec.Ingress`（`List<V1NetworkPolicyIngressRule>`，含 `Ports` + `From` peers）
- `Spec.Egress`（`List<V1NetworkPolicyEgressRule>`，含 `Ports` + `To` peers）

与 ConfigMap 的简单 key-value 不同，NetworkPolicy 的嵌套规则结构（Port → Peers → NamespaceSelector/PodSelector/IpBlock）层级较深，需合理设计 ViewModel 以平衡可读性与完整性。

## Goals / Non-Goals

**Goals:**
- 列表页展示 NetworkPolicy 核心概要（名称、命名空间、策略类型、规则数量）
- 详情页完整展示 Ingress/Egress 规则树（端口、对等体选择器、IP 块）
- 创建和编辑通过表单输入（命名空间、Pod 选择器、规则列表），支持动态添加/删除规则和端口
- YAML 编辑页支持直接修改原始 YAML 并应用
- Admin 角色的增删改操作、Guest 只读访问
- 遵循现有 ConfigMap/Node 的可达性检查模式

**Non-Goals:**
- 不实现图形化网络拓扑可视化（超出当前页面能力）
- 不实现 NetworkPolicy 的批量导入/导出
- 不校验 NetworkPolicy 规则间的语义冲突（由 k8s API Server 校验）
- 不新增数据库表或实体模型

## Decisions

### D1: 遵循 ConfigMapService 模式实现 NetworkPolicyService

**选择：** `NetworkPolicyService` 使用 primary constructor 注入 `ClusterRepository`，复制 `BuildConfig` 私有方法（已知技术债），不注入 `ILogger`，不做 try/catch。方法名与 `ConfigMapService` 对齐：`ListNetworkPoliciesAsync`、`GetNetworkPolicyAsync`、`CreateNetworkPolicyAsync`、`UpdateNetworkPolicyAsync`、`DeleteNetworkPolicyAsync`、`UpdateNetworkPolicyFromYamlAsync`。

**理由：** 保持一致性，页面层负责错误处理（try/catch + Snackbar），service 层仅做纯 k8s API 调用。

### D2: ViewModel 分层设计——列表简洁、详情完整

**列表 VM (`NetworkPolicyListViewModel`)**：包含 `Name`、`Namespace`、`PolicyTypes`（逗号拼接字符串，如 `"Ingress, Egress"`）、`IngressRuleCount`、`EgressRuleCount`、`CreatedAt`。保持轻量，适合表格展示。

**详情 VM (`NetworkPolicyDetailViewModel`)**：包含完整规则树 + YAML 序列化内容 + 集群元数据（`ClusterId`、`ClusterName`）。

**规则子 VM (`NetworkPolicyRuleViewModel`、`NetworkPolicyPortViewModel`、`NetworkPolicyPeerViewModel`)**：镜像 k8s 模型结构，用于详情页和编辑表单的递归渲染。

**创建/更新 VM (`NetworkPolicyCreateViewModel`、`NetworkPolicyUpdateViewModel`)**：表单可编辑字段，含 `ClusterId`、`Name`、`Namespace`、`PodSelector`（key-value 字典）、`PolicyTypes`（多选列表）、`IngressRules` / `EgressRules`（可动态增删的规则列表）。

### D3: 页面布局复用双栏模式 + 详情页 + 对话框 + YAML 编辑

**选择：** 完全复用 ConfigMaps 的页面布局模式。
- 列表页：左侧集群树 + 右侧表格（`/networkpolicies` + `{ClusterId:int}`）
- 详情页：独立页面（`/networkpolicies/{ClusterId:int}/{Namespace}/{Name}`）
- 创建：`MudDialog` 对话框（`CreateNetworkPolicyDialog.razor` colocated）
- YAML 编辑：独立页面（`/networkpolicies/{ClusterId:int}/{Namespace}/{Name}/yaml`）
- 编辑表单：不单独实现——详情页内嵌编辑模式或复用 YAML 编辑覆盖
- 删除：列表页内联操作（`DialogService.ShowMessageBoxAsync` 确认后调 service）

**理由：** 保持用户一致体验，降低学习成本。ConfigMap 提供了最完整的参考实现。

### D4: PodSelector 在创建/编辑表单中简化为 key-value 字典输入

**选择：** 使用 `MudTable` 或 `MudChipSet` 输入 key-value 对组成 `matchLabels`。不支持复杂的 `matchExpressions`（如 `In`、`NotIn`、`Exists` 操作符）。

**理由：** `matchLabels` 覆盖 90% 的使用场景，`matchExpressions` 可通过 YAML 编辑页补充。避免表单过度复杂化。

**替代方案（已拒绝）：**
- **完整的 LabelSelector 构建器**：需实现嵌套的条件编辑 UI，复杂度高且使用频率低。
- **纯 YAML 编辑**：对非专业用户门槛过高。

### D5: 策略类型使用 MudSelect 多选

**选择：** `MudSelect` 的 `MultiSelection="true"` 选择 `"Ingress"` 和/或 `"Egress"`。默认选中两项。

**理由：** 简洁直观，k8s PolicyTypes 值为固定枚举。

## Risks / Trade-offs

- **[复杂规则输入体验]** → 创建/编辑表单的规则列表为可扩展的行，每行含端口选择 + 对等体选择器。对于复杂规则（多端口、多 peer），表单可能冗长。YAML 编辑作为补充方案。
- **[BuildConfig 重复]** → 与 `ClusterService` / `ClusterNodeService` / `ConfigMapService` 一样复制 `BuildConfig` 方法。此为已知技术债，后续统一抽取 `KubernetesClientFactory` 解决，但现在继续复制。
- **[无规则语法校验]** → 创建时仅校验必填字段（Name、Namespace），不校验规则语法。k8s API Server 返回错误由 Snackbar 展示。不影响安全——无效规则被 API Server 拒绝。
