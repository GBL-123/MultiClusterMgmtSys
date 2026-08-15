## Context

Current node detail page (`redesign-nodes-page`, implemented and awaiting archive) composes 10 cards:

```
NodeOverviewCard   全宽   概要 5 字段 + chip
NodeSchedulingCard 全宽   调度信息 3 字段       ← 过薄
NodeMetadataCard   全宽   元数据 2 字段 (Uid)   ← 过薄
NodeResourcesCard  全宽   资源容量 2 表
Addresses|Conditions   md=6 并排   条件 6 列表格被挤
Taints   |Labels       md=6 并排   标签几十对 KV 被挤
Annotations|SystemInfo md=6 并排   系统信息 10 字段被挤
```

Real nodes expose multiple IPs (`node.Status.Addresses` array, e.g. 2× `InternalIP` + 1× `ExternalIP` with different roles), but the list VM only keeps the first `InternalIP` (`ComputeInternalIP` → `FirstOrDefault`), and nothing carries a per-IP remark. `ClusterService.GetClusterDetailAsync` fills `ClusterDetailViewModel.Nodes` via the same `ClusterNodeService.GetClusterNodesAsync` call as the nodes page — so both list surfaces share one data source and must change together.

Precedent for admin-maintained non-K8s metadata: `ClusterEndpoint` (entity + `ClusterInfo.Endpoints` navigation + cascade delete + Admin-gated manage dialog via `ClusterEndpointsDialog` + whole-list diff persistence in `ClusterMappingExtensions.ApplyEndpoints`). This change reuses that shape, with one deliberate divergence: node IP remarks are keyed to the live K8s address list instead of free-form entries.

## Goals / Non-Goals

**Goals:**
- Consolidate the detail page to 5 balanced cards; thin cards (调度信息/元数据/污点/地址) merge into 基本信息; content-heavy cards (条件/系统信息) get full width.
- Add admin-maintained remarks per node IP, keyed to the live address so they can never refer to a non-existent IP.
- Surface all IPs + remarks on both the nodes list page and the cluster-detail nodes card (shared VM/data source), renaming the column to "IP 地址".
- Remove `Uid` (no consumer value) and the dead first-IP-only `InternalIP` fields.

**Non-Goals:**
- No node write operations against K8s (no cordon/drain/uncordon).
- No changes to 资源容量 / 标签 / 注解 / 条件 content semantics — layout width only.
- No remark feature for `Hostname`/DNS address types (display-only).
- No free-form remark rows (no admin-entered IPs).
- The `NodeListFilterBar` live-filter defect tracked in `redesign-nodes-page` task 9.1 is NOT addressed here.

## Decisions

### Decision 1: Detail page 5-card composition

```
NodeDetailToolbar
┌ 基本信息 (NodeOverviewCard, 吸收四卡) ─────────────────────┐
│ 名称 | 状态chip | 角色 | Kubelet 版本 | 操作系统             │
│ 调度性chip | Phase | PodCIDR | 创建时间                    │
│ ── 地址 ── (全部地址行: 类型 + IP + 备注)                  │
│ ── 污点 ── (仅 node.Taints 非空时渲染)                    │
├ 资源容量 ─────────────────────────────────────────────────┤
├ 条件 (全宽 6 列表格) ─────────────────────────────────────┤
├ 标签 | 注解 (md=6 并排, 不变) ────────────────────────────┤
└ 系统信息 (独立全宽, 10 字段 xs=12 sm=6 md=4 网格) ────────┘
```

- `NodeOverviewCard` grows to absorb 调度信息 (Unschedulable chip / Phase / PodCIDR), 元数据 (创建时间 only — Uid dropped), 地址 section, and conditional 污点 section.
- `NodeSchedulingCard.razor`, `NodeMetadataCard.razor`, `NodeAddressesCard.razor`, `NodeTaintsCard.razor` are deleted (their content fully absorbed).
- `NodeConditionsCard` / `NodeSystemInfoCard`: remove the `md=6` pairing → direct full-width children of the outer `MudStack` (composition mirrors `redesign-nodes-page` spec order minus the deleted cards).
- **Alternatives considered:** (a) MudTabs to collapse content — rejected: hides info on a read-only detail page, adds interaction cost; (b) keep 10 cards — the very problem being fixed.

### Decision 2: `NodeIpRemark` entity — keyed to live addresses

```csharp
public class NodeIpRemark
{
    public int Id { get; set; }
    public int ClusterId { get; set; }          // FK → ClusterInfo, cascade delete
    public string NodeName { get; set; } = "";  // k8s node name
    public string Address { get; set; } = "";   // IP string
    public string? Note { get; set; }           // max 64
    public ClusterInfo? Cluster { get; set; }
}
// unique index (ClusterId, NodeName, Address)
```

- Stored on `ClusterInfo.NodeIpRemarks` navigation (mirrors `Endpoints`); cluster deletion cascades remarks.
- **Why keyed to (node, IP) rather than free-form like `ClusterEndpoint`:** the edit surface is the node's live `node.Status.Addresses` — admin fills a note per real IP, save upserts by the unique key. A remark can never dangle on an IP that no longer exists, and no stale rows accumulate when node IPs change.
- **Schema impact (documented, must execute during implementation):** the project has NO EF migrations — `Program.cs` uses `db.Database.EnsureCreated()`. Adding the entity requires deleting the local `MultiClusterMgmtSys.db` so the schema regenerates on next start. `*.db` is gitignored (runtime artifact); the admin seed is re-created automatically by `AccountService.CreateAdminAsync()`.

### Decision 3: Remark scope — IP-class address types only

Only `node.Status.Addresses` rows whose `Type` is `InternalIP` or `ExternalIP` are remark-editable. `Hostname` / `InternalDNS` / `ExternalDNS` rows render in 基本信息 (type + value) but with no note input. **Why:** the user's requirement is about IPs with functional roles (管理口/数据口/出口); hostname/DNS are identity, not endpoints, and the K8s type taxonomy doesn't classify them as IPs.

### Decision 4: List VM — `InternalIP` removed, `IpAddresses` list added

```csharp
// Components/Clusters/ViewModels/ClusterNodeViewModel.cs  (namespace: Components.Clusters.ViewModels — existing quirk, preserve)
public List<NodeIpViewModel> IpAddresses { get; set; } = new();
// removes: public string InternalIP { get; set; } = "";
```

- New `NodeIpViewModel { Address, Note }` lives in `Components/Nodes/ViewModels/` (`MultiClusterMgmtSys.Components.Nodes.ViewModels`) — matches where `NodeAddressViewModel` already lives (AGENTS.md namespace rules: don't assume path==namespace).
- **Why remove `InternalIP` entirely:** after this change all three consumers (`NodeListTable`, `ClusterNodesCard`, `NodeOverviewCard`) switch to the address list; keeping the field would leave dead code. Column header changes 内网 IP → "IP 地址".
- Both list surfaces update in lockstep: `NodeListTable.razor` and `ClusterNodesCard.razor` render `IpAddresses` as stacked rows (`IP` + note in secondary text). `ClusterNodesCard` keeps its `Take(5)` truncation + "查看全部".

### Decision 5: Remark read/write path

- **Read:** `ClusterNodeService` fetches the cluster's remarks in ONE query (`repo` via `ClusterInfo.NodeIpRemarks`), builds a lookup keyed `(nodeName, address)`, and merges into:
  - `GetClusterNodesAsync` → each `ClusterNodeViewModel.IpAddresses` (IP-class rows only).
  - `GetNodeDetailAsync` → `ClusterNodeDetailViewModel.Addresses` rows gain `Note` (`NodeAddressViewModel` + `Note`).
- **Write:** new `UpdateNodeIpNotesAsync(int clusterId, string nodeName, List<NodeIpNoteEditItem> items)` — whole-list diff upsert: insert new keys, update changed notes, delete removed keys (same `ApplyEndpoints` pattern, scoped to one node). Logs enter/done with `logger.LogInformation` / `logger.LogWarning` per established service conventions.
- **Why one query:** avoids N queries per node on fleets; remarks are small (≤ a few per node).

### Decision 6: Manage dialog mirrors `ClusterEndpointsDialog`

- New `Components/Nodes/Shared/NodeIpNotesDialog.razor`: receives `ClusterId` + the node's live `Addresses` (passed from the page); renders one row per IP-class address: IP (monospace) + type + note `MudTextField`; "保存" invokes the service, returns changed-result to the card.
- Entry: Admin-only "管理" button in 基本信息 card's 地址 section header (wrapped in `<AuthorizeView Roles="Admin">`), exactly like `ClusterEndpointsCard`.
- Member/read-only users see the remarks as plain text with no manage affordance.

## Risks / Trade-offs

- **[R1] DB rebuild required** (no migrations, `EnsureCreated`) → Mitigation: documented in tasks; local dev DB only, gitignored; admin seed auto-recreated at startup. Sequence the drop as an explicit task step before first run.
- **[R2] VM change ripples to cluster detail page** → Mitigation: both consumers changed in the same task set; the shared `GetClusterNodesAsync` source means the data is consistent everywhere by construction. `dotnet build` + smoke both routes.
- **[R3] Remarks orphaned when a node is deleted/renamed** → Mitigation: rows keyed `(ClusterId, NodeName, Address)` — when the node disappears from K8s, its remarks simply don't match any live row and stop rendering; harmless residue until node name re-use (acceptable; a cleanup job is out of scope).
- **[R4] `ComputeInternalIP` removal touches `MapNode`/`MapNodeDetail` internals** → Mitigation: `ComputeInternalIP` helper deleted with the field; single service file, compile-time enforcement via build.
- **[R5] 基本信息 card grows long** → Trade-off accepted: it groups naturally related fields (the pre-merge cards were 2–3 fields each); addresses/taints are conditional blocks that shrink when empty.
- **[R6] 条件/系统信息 full width adds vertical scroll** → Accepted: 6-column tables and 10-field grids genuinely need the width; matches cluster detail page's simple vertical rhythm.

## Migration Plan

1. Implementation order (tasks.md enforces): entity + DbContext → VM/mapping changes → service merge + upsert → shared card consolidation + dialog → both list tables → page composition → build → manual smoke.
2. Local dev: delete `MultiClusterMgmtSys.db` (+ `-shm`/`-wal`) before first run of the new schema; admin/`admin` seed is recreated automatically.
3. Rollback: `git revert` the change commit; DB schema is regenerated anyway (EnsureCreated), so no forward-migration leftover.

## Open Questions

- (none remaining — remark scope, keying, list column, and change packaging all resolved with the user)
