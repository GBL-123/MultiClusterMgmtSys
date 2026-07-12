## Why

当前集群管理页面展示的字段几乎全部由 k8s 探测自动回填，缺少运维手工补充的「外部可达信息」。在多集群纳管场景中，集群与节点的 IP 语义远比单一 ApiServer 复杂：

- **集群级**：管理节点虚拟 IP、业务节点虚拟 IP、集群通信虚拟 IP、管理看板公网 IP、业务配置公网 IP——同一集群有多个不同用途的 VIP/公网 IP。
- **节点级**：每个节点可能有多张网卡，分别承载 k8s IP、业务 IP、集群通信 IP；节点网卡数量不固定（1 个或多个）。

当前模型没有承载这些结构化 IP 的能力，运维只能靠外部表格维护，信息割裂。需要以「固定分类 + 键值对子表」的方式持久化集群级与节点级 IP，并在页面合并展示手工录入与 k8s 自动获取的地址。

## What Changes

- 新增 `ClusterIp` 实体（子表）：`ClusterId` + `Category`（固定枚举 `ClusterIpCategory`：MgmtVip/BizVip/CommVip/MgmtPublicIp/BizPublicIp）+ `Ip` + `Remark`，一个集群可有多条。
- 新增 `ClusterDomain` 实体（子表）：`ClusterId` + `Domain`（string, Required）+ `Remark`（string?），一个集群可有多条访问域名（无固定类别，自由录入）。
- 新增 `ClusterNodeIp` 实体（子表）：`ClusterId` + `NodeName` + `Category`（固定枚举 `NodeIpCategory`：K8sIp/BizIp/CommIp）+ `Ip` + `Remark`，一个节点可有多条。
- 在 `ClusterInfo` 上保留 `Remark`（集群备注）标量字段——单值通用注释，不进子表。
- `AppDbContext` 新增三个 DbSet + OnModelCreating 配置（FK + 级联删除 + 索引）。
- 新增 `ClusterIpRepository`（或扩展 `ClusterRepository`）+ `ClusterService` 的集群 IP CRUD 方法。
- 新增节点 IP 服务方法（`ClusterNodeService` 或独立 `ClusterNodeIpService`），节点 IP 纯持久化 CRUD，不调 k8s。
- 添加/编辑集群对话框新增「集群 IP（可选）」分区，支持动态增删多行（类别下拉 + IP 输入 + 备注）。
- 集群详情页新增「集群 IP」卡片，按类别分组展示。
- 节点详情页新增「手工 IP」卡片，展示该节点持久化的 `ClusterNodeIp`，并提供 Admin 录入/编辑入口（对话框）。
- 节点列表页「内网 IP」列合并展示 k8s InternalIP 与手工录入 IP（手工 IP 优先或并列）。
- **BREAKING**：`ClusterInfo` 模型变更 + 新增两表，按仓库约定需删除 `clusters.db*` 后重跑 `EnsureCreated()` 重建 schema。
- **BREAKING**：节点管理页从「只读观测」变为「Admin 可手工录入节点 IP」，`node-management` spec 的只读约束解除。

## Capabilities

### New Capabilities

（无新增能力，本变更扩展现有集群管理与节点管理能力）

### Modified Capabilities

- `cluster-management`: 新增集群级结构化 IP（固定分类子表）的录入与展示；保留集群访问域名、备注两个标量字段；添加/编辑对话框、详情页、列表页同步调整。
- `node-management`: 节点管理页从只读变为 Admin 可手工录入节点级 IP（固定分类子表）；节点详情页与节点列表页合并展示 k8s 自动获取地址与手工录入 IP。

## Impact

- **数据模型**：`Models/` 新增 `ClusterIp`、`ClusterNodeIp` 实体 + `ClusterIpCategory`、`NodeIpCategory` 枚举；`ClusterInfo` 新增 `AccessDomain`、`Remark` 标量字段与 `ClusterIps` 导航属性；`AppDbContext` 新增 2 个 DbSet + OnModelCreating FK/级联/索引配置；需删除 `clusters.db*` 重建库。
- **Daos**：新增 `ClusterIpRepository`（或扩展 `ClusterRepository`）处理集群 IP、集群域名与节点 IP 的 CRUD。
- **Services**：`ClusterService` 新增集群 IP 与集群域名的保存/查询方法（添加/编辑集群时一并保存）；`ClusterNodeService`（或新 `ClusterNodeIpService`）新增节点 IP 的 CRUD。
- **ViewModels**：新增 `ClusterIpViewModel`、`ClusterDomainViewModel`、`ClusterNodeIpViewModel`；`ClusterCreateViewModel`/`ClusterEditViewModel`/`ClusterDetailViewModel` 携带 `List<ClusterIpViewModel>` 与 `List<ClusterDomainViewModel>`；`ClusterNodeDetailViewModel` 携带 `List<ClusterNodeIpViewModel>`；`ClusterNodeViewModel` 携带合并后的 IP 展示信息。
- **Mappings**：`ClusterMappingExtensions` 同步映射 IP 与域名集合；新增 IP/域名实体 ↔ ViewModel 映射。
- **UI**：
  - `Pages/Clusters/AddClusterDialog.razor`、`EditClusterDialog.razor` 新增「集群 IP」与「访问域名」动态增删分区 + 备注（标量）字段。
  - `Pages/Clusters/ClusterDetail.razor` 新增「集群 IP」卡片（按类别分组）+ 「访问域名」卡片 + 基本信息卡片备注项。
  - `Pages/Clusters/Clusters.razor` 列表表格新增「公网 IP」列（展示 MgmtPublicIp 第一条）。
  - `Pages/Nodes/NodeDetail.razor` 新增「手工 IP」卡片（按类别分组）+ Admin「编辑 IP」按钮入口。
  - `Pages/Nodes/Nodes.razor` 节点列表「内网 IP」列改为「节点 IP」列，合并展示手工 IP 与 k8s InternalIP。
  - 新增 `Pages/Nodes/NodeIpEditDialog.razor`（节点 IP 编辑对话框，colocate 于 Nodes 子目录，动态增删多行）。
  - `_Imports.razor` 确认 Nodes 子目录 `@using` 存在。
- **依赖/外部系统**：无新增依赖，节点 IP 服务不调 k8s。