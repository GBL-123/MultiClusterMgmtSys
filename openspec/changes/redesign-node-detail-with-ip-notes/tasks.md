## 1. Data layer — NodeIpRemark entity

> Note: the project has NO EF migrations — `Program.cs` uses `EnsureCreated()`. Adding this entity requires deleting the local `MultiClusterMgmtSys.db` (+ `-shm`/`-wal`) before the next run so the schema regenerates (task 8.2).

- [x] 1.1 Create `Data/Entities/NodeIpRemark.cs` — namespace `MultiClusterMgmtSys.Data.Entities`, mirroring `ClusterEndpoint`'s terse style: `Id`, `ClusterId`, `NodeName` (`string`, default `""`), `Address` (`string`, default `""`), `Note` (`string?`), `Cluster` navigation property.
- [x] 1.2 In `Data/Entities/ClusterInfo.cs` add `public ICollection<NodeIpRemark> NodeIpRemarks { get; set; } = new List<NodeIpRemark>();` (mirror the existing `Endpoints` navigation).
- [x] 1.3 In `Data/ApplicationDbContext.cs` add `public DbSet<NodeIpRemark> NodeIpRemarks => Set<NodeIpRemark>();` and a `modelBuilder.Entity<NodeIpRemark>` block: `Note` `.HasMaxLength(64)`, unique index on `(ClusterId, NodeName, Address)`, and `HasOne(e => e.Cluster).WithMany(c => c.NodeIpRemarks).HasForeignKey(e => e.ClusterId).OnDelete(DeleteBehavior.Cascade)` (copy the `ClusterEndpoint` block shape at lines 37-46).

## 2. View models

- [x] 2.1 Create `Components/Nodes/ViewModels/NodeIpViewModel.cs` — namespace `MultiClusterMgmtSys.Components.Nodes.ViewModels` (matches sibling `NodeAddressViewModel.cs`; do NOT guess from path): `Address` (`string`, default `""`), `Note` (`string?`).
- [x] 2.2 `Components/Clusters/ViewModels/ClusterNodeViewModel.cs` — namespace `MultiClusterMgmtSys.Components.Clusters.ViewModels` (existing quirk, preserve): **remove** `InternalIP`, **add** `public List<NodeIpViewModel> IpAddresses { get; set; } = new();` plus `using MultiClusterMgmtSys.Components.Nodes.ViewModels;` (precedent: `ClusterNodeDetailViewModel.cs` line 1).
- [x] 2.3 `Components/Clusters/ViewModels/ClusterNodeDetailViewModel.cs` — **remove** `Uid`.
- [x] 2.4 `Components/Nodes/ViewModels/NodeAddressViewModel.cs` — add `public string? Note { get; set; }`.

## 3. Service layer — remark merge + upsert

- [x] 3.1 `Components/Nodes/Services/ClusterNodeService.cs`: delete `ComputeInternalIP` (lines 108-111) and its two call sites (lines 85, 123); delete the `Uid = node.Metadata?.Uid ?? ""` mapping line (line 126).
- [x] 3.2 Add a private remark-lookup helper: fetch `ClusterInfo.NodeIpRemarks` for the cluster once (via `repo`), build a dictionary keyed `(NodeName, Address)`.
- [x] 3.3 `GetClusterNodesAsync`: build each `ClusterNodeViewModel.IpAddresses` from ALL `node.Status.Addresses` rows whose `Type` is `InternalIP` or `ExternalIP` (preserve address order), merging `Note` from the remark lookup (null/absent → no note). Log enter/done with `logger.LogInformation` per service convention.
- [x] 3.4 `GetNodeDetailAsync` / `MapNodeDetail`: stop setting `InternalIP`; merge `Note` into each IP-class `NodeAddressViewModel` row from the remark lookup.
- [x] 3.5 Add `public async Task UpdateNodeIpNotesAsync(int clusterId, string nodeName, List<NodeIpNoteEditItem> items)` — load the cluster, whole-list diff upsert scoped to `(clusterId, nodeName)`: insert new `(Address, Note)` keys, update changed notes, delete rows whose key is absent from the submitted list (null note = delete). Validate `Note` length ≤ 64. `logger.LogInformation` at enter/done, `logger.LogWarning` on missing cluster (mirror `UpdateClusterEndpointsAsync` at `ClusterService.cs:147-160`). If a `NodeIpNoteEditItem` type does not already exist, define it as a small record in `Components/Nodes/Requests/` (namespace `MultiClusterMgmtSys.Components.Nodes.Requests`).

## 4. Detail shared components — consolidation

- [x] 4.1 Rewrite `Components/Nodes/Shared/NodeOverviewCard.razor` into the 基本信息 card: existing fields (名称 / 状态chip in header / 角色 / Kubelet 版本 / 操作系统) + merged 调度信息 (调度性 chip Warning/Success, Phase, PodCIDR) + 创建时间 (`yyyy-MM-dd HH:mm`, NO `Uid`) + 地址 section (all `Node.Addresses` rows: 类型 + IP monospace + 备注) + 污点 section rendered ONLY when `Node.Taints.Count > 0` (键 / 值 / 效果 table, "无污点" empty-state text NOT needed since section is hidden when empty). Keep the `mb-4` card + `h6 "基本信息"` header convention.
- [x] 4.2 Delete `Components/Nodes/Shared/NodeSchedulingCard.razor`, `NodeMetadataCard.razor`, `NodeAddressesCard.razor`, `NodeTaintsCard.razor` (content fully absorbed by 4.1).
- [x] 4.3 `NodeConditionsCard.razor` — no content change; remove the page-side `md=6` pairing by moving it out of the grid in `NodeDetail.razor` (task 5.1). If any card-internal full-width tweak is needed, keep columns 类型/状态/原因/消息/最近心跳/最近转换.
- [x] 4.4 `NodeSystemInfoCard.razor` — no content change; becomes a full-width direct child (task 5.1).

## 5. Detail page composition + manage dialog

- [x] 5.1 `Components/Nodes/Pages/NodeDetail.razor` — rewrite the card composition to: `NodeDetailToolbar` → `NodeOverviewCard` (full width) → `NodeResourcesCard` (full width) → `NodeConditionsCard` (full width) → one `MudGrid` paired row `NodeLabelsCard` + `NodeAnnotationsCard` (`xs=12 md=6`) → `NodeSystemInfoCard` (full width). Remove the `NodeSchedulingCard`/`NodeMetadataCard`/`NodeAddressesCard`/`NodeTaintsCard` references.
- [x] 5.2 Add the Admin-gated "管理" button to the 地址 section of `NodeOverviewCard` (`<AuthorizeView Roles="Admin">`), opening `NodeIpNotesDialog` (mirror `ClusterEndpointsCard.OpenManageDialog` at `ClusterEndpointsCard.razor:89-102`); on dialog success, invoke the page's reload (`EventCallback` up to `NodeDetail.razor` → `LoadAsync`).
- [x] 5.3 Create `Components/Nodes/Shared/NodeIpNotesDialog.razor` — params: `ClusterId`, `NodeName`, live `List<NodeAddressViewModel> Addresses`; render one row per IP-class address (monospace IP + type + note `MudTextField` with 64-char `MaxLength` validation); "保存" calls `ClusterNodeService.UpdateNodeIpNotesAsync` with the edited items and returns non-cancelled; "取消" returns cancelled without writing.

## 6. List surfaces — IP column

- [x] 6.1 `Components/Nodes/Shared/NodeListTable.razor` — change header "内网 IP" → "IP 地址"; render `context.IpAddresses` as stacked lines (`Address` + `Note` in secondary text), `—` when empty.
- [x] 6.2 `Components/Clusters/Shared/ClusterNodesCard.razor` — same header + cell change (keep `Take(5)` truncation and "查看全部" → `/nodes/{Cluster.Id}`).
- [x] 6.3 Grep for any remaining `.InternalIP` references under `MultiClusterMgmtSys/` — none should survive (spec: no dead consumers of the removed field).

## 7. Build

- [x] 7.1 `dotnet build MultiClusterMgmtSys.slnx` — clean (0 errors); confirm no consumer of removed `InternalIP` / `Uid` remains and no orphaned card reference compiles.

## 8. Schema rebuild + verification

- [x] 8.1 **Delete `MultiClusterMgmtSys.db` (+ `-shm`/`-wal`) if present** so `EnsureCreated()` regenerates the schema with `NodeIpRemarks` (gitignored runtime artifact; admin seed re-created automatically at startup — verify `admin` user exists after first run). — Done: DB deleted; app started clean, admin seed "Create admin account succeeded", listening on :5021.
- [ ] 8.2 `dotnet run --project MultiClusterMgmtSys` and smoke-test (manual browser session):
  - Detail page renders 5 cards in spec order; 基本信息 shows 调度/创建时间/地址(+备注)/污点-when-present; no Uid anywhere
  - Node with 3 IPs: all three render in 基本信息; Admin "管理" dialog lists live IPs, saving a note persists it (reload shows it), clearing it removes it; Member sees no 管理 button
  - Nodes list page + cluster detail nodes card: "IP 地址" column shows all IPs stacked with notes; `ClusterNodesCard` still truncates to 5 with working "查看全部"
  - Empty-taints node hides the 污点 section; a node with taints shows them
- [x] 8.3 Confirm no edits leaked outside this change's scope (`git diff --name-only` review). — Verified: all M/?? entries are within this change's file list (Nodes.razor + redesign-nodes-page/* edits are from the earlier spec-reconciliation session, not this change).

## 9. Out-of-scope reaffirmation

- [x] 9.1 (Documentation-only) Not added: remark rows for Hostname/DNS, free-form IP entry, remark cleanup job, K8s write ops (cordon/drain), and the `NodeListFilterBar` live-filter defect (tracked as task 9.1 of `redesign-nodes-page`).
