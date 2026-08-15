## Why

The node detail page (`/nodes/{ClusterId}/{NodeName}`, built in the `redesign-nodes-page` change) renders 10 stacked cards, but several are too thin to justify their own card (调度信息 = 3 fields, 元数据 = 2 fields, 污点 often empty), while content-heavy sections (系统信息 10 fields, 条件 6 columns, 标签/注解 dozens of KV pairs) are squeezed into half-width `md=6` slots. Meanwhile the user's real-world nodes expose up to 3 IPs with distinct roles (management / data / egress), and the current UI shows only the first `InternalIP` — losing the rest entirely. This change consolidates the detail page into a balanced 5-card layout and adds admin-maintained per-IP remarks so multi-IP nodes become fully legible, on both the detail page and the node list.

## What Changes

- **Detail page 5-card layout** — replace the 10-card composition with: `NodeOverviewCard` expanded into a "基本信息" card (absorbs 概要 + 调度信息 + 元数据 + 地址 + 污点), 资源容量 (unchanged), 条件 (full width), 标签|注解 (unchanged pair), 系统信息 (own full-width card). Delete `NodeSchedulingCard.razor`, `NodeMetadataCard.razor`, `NodeAddressesCard.razor`, `NodeTaintsCard.razor`.
- **Uid removed** — `ClusterNodeDetailViewModel.Uid` and its mapper line are deleted; the 基本信息 card drops the Uid field (configmaps' own `Uid` is unrelated and untouched).
- **污点 empty-hidden** — the taints block renders only when the node has taints.
- **New `NodeIpRemark` entity** — admin-maintained remarks keyed by `(ClusterId, NodeName, Address)` with a unique index and cascade delete on cluster. Only IP-class address types (`InternalIP`, `ExternalIP`) can carry remarks; `Hostname`/DNS rows are display-only. **BREAKING (schema)**: adding an entity under `EnsureCreated()` (no EF migrations) requires deleting/regenerating `MultiClusterMgmtSys.db`.
- **IP remarks on the detail page** — the 基本信息 card's address section lists all addresses with type + note; Admin gets a "管理" dialog that shows the node's current live IPs (from K8s) and upserts remarks by `(ClusterId, NodeName, Address)` — remarks never drift from reality.
- **IP remarks on list surfaces** — `ClusterNodeViewModel.InternalIP` (first-IP-only string) is replaced by `List<NodeIpViewModel> { Address, Note }`; the "内网 IP" column header becomes "IP 地址" and renders multiple IPs each with its note. Both consumers change together: `NodeListTable.razor` (nodes page) and `ClusterNodesCard.razor` (cluster detail, which shares the same `GetClusterNodesAsync` data source). `ClusterNodeDetailViewModel.InternalIP` is likewise replaced by the address list in 基本信息.
- **Service merge** — `ClusterNodeService.GetClusterNodesAsync` / `GetNodeDetailAsync` join stored remarks into the address data; a new `UpdateNodeIpNotesAsync(clusterId, nodeName, items)` upserts remarks for a node.

## Capabilities

### New Capabilities

- `node-detail-layout`: The consolidated 5-card node detail page — card composition order, full-width/half-width assignments, Uid removal, empty-hidden taints.
- `node-ip-notes`: Admin-maintained per-IP remarks for nodes — the `NodeIpRemark` persistence contract, remark merge into read paths, the manage dialog surface, and the multi-IP "IP 地址" column on both list surfaces.

### Modified Capabilities

- (none — no existing main specs under `openspec/specs/` are affected; the nodes-page delta spec of the in-flight `redesign-nodes-page` change is superseded by `node-detail-layout`)

## Impact

- **Code**:
  - `Data/Entities/NodeIpRemark.cs` — new entity (+ navigation on `ClusterInfo`).
  - `Data/ApplicationDbContext.cs` — `DbSet<NodeIpRemark>` + unique index `(ClusterId, NodeName, Address)` + cascade delete.
  - `Components/Nodes/Services/ClusterNodeService.cs` — merge remarks into `GetClusterNodesAsync` / `GetNodeDetailAsync`; add `UpdateNodeIpNotesAsync`; remove `Uid` mapping.
  - `Components/Clusters/ViewModels/ClusterNodeViewModel.cs` — **BREAKING**: remove `InternalIP`, add `List<NodeIpViewModel> IpAddresses`.
  - `Components/Clusters/ViewModels/ClusterNodeDetailViewModel.cs` — remove `Uid`; `InternalIP` consumed by 基本信息 card only.
  - `Components/Nodes/ViewModels/NodeAddressViewModel.cs` — add `Note`; new `NodeIpViewModel` (or reuse `NodeAddressViewModel` shape for list rows).
  - `Components/Nodes/Pages/NodeDetail.razor` — new card composition.
  - `Components/Nodes/Shared/` — `NodeOverviewCard` absorbs scheduling/metadata/address/taints; delete `NodeSchedulingCard`, `NodeMetadataCard`, `NodeAddressesCard`, `NodeTaintsCard`; `NodeConditionsCard` / `NodeSystemInfoCard` full width; new `NodeIpNotesDialog`.
  - `Components/Nodes/Shared/NodeListTable.razor` — column header "IP 地址", multi-IP + note rows.
  - `Components/Clusters/Shared/ClusterNodesCard.razor` — same IP column treatment.
  - `Components/Nodes/Shared/NodeListFilterBar.razor` — untouched (filter workstream from `redesign-nodes-page` task 9.1 remains separate).
- **Services/APIs**: `ClusterNodeService` gains one public method; existing method signatures otherwise unchanged.
- **Database**: new `NodeIpRemarks` table; `EnsureCreated()` means deleting `MultiClusterMgmtSys.db` regenerates the schema (gitignored, local only).
- **Dependencies**: none new (MudBlazor only).
- **Routes**: unchanged — `/nodes`, `/nodes/{ClusterId}`, `/nodes/{ClusterId}/{NodeName}`.
- **Tests**: no test project exists; verified via `dotnet build MultiClusterMgmtSys.slnx` + manual smoke.
