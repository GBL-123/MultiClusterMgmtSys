## Context

The cluster feature established two visual templates during the in-flight `redesign-cluster-detail-with-endpoints` and `refactor-clusters-group-sidebar-layout` changes:

**Detail page template** (`Components/Clusters/Pages/ClusterDetail.razor`):
```
<MudStack Class="flex-auto">
  <ClusterDetailToolbar .../>      ← MudPaper pa-4 mb-4, header row
  <ClusterOverviewCard .../>        ← MudCard Elevation=1 Class="mb-4"
  <ClusterEndpointsCard .../>       ← MudCard Elevation=1 Class="mb-4"
  <ClusterNodesCard .../>            ← MudCard Elevation=1 Class="mb-4"
</MudStack>
```

**List page template** (`Components/Clusters/Pages/Clusters.razor`):
```
<MudStack Class="d-flex flex-auto">
  <GroupSidebar .../>                ← left nav (cluster-specific)
  <MudStack Class="flex-auto">
    <MudPaper Class="pa-4">          ← toolbar + filter bar
      title row + <ClusterFilterBar/>
    </MudPaper>
    <ClusterTable .../>              ← MudTable ServerData + Pager + NoRecords
  </MudStack>
</MudStack>
```

The node pages were written in the older style (left TreeView cluster picker + right self-contained content column, every page re-loading the cluster list via `ClusterService.GetClustersAsync()`). Both `Nodes.razor` and `NodeDetail.razor` are currently wrapped in `@* ... *@` (418 + 532 dead lines). `ClusterNodesCard.razor`'s "查看全部" button still navigates to `/nodes/{Cluster.Id}`, so the route is live-but-broken.

Service/VM layer is already correct and reused:
- `ClusterNodeService.GetClusterNodesAsync(int id)` → `List<ClusterNodeViewModel>` (6 fields).
- `ClusterNodeService.GetNodeDetailAsync(int clusterId, string nodeName)` → `ClusterNodeDetailViewModel?` (aggregates Addresses/Conditions/Taints/Capacity/Allocatable/Labels/Annotations/SystemInfo).
- `ClusterService.GetClusterDetailAsync(int id)` → `ClusterDetailViewModel` (used for the breadcrumb context chip + `IsReachable` gate).

Namespace rule (from AGENTS.md): `Components/Nodes/**` uses `MultiClusterMgmtSys.Components.Nodes.*` (matches physical path, NOT `Features.*`). The shared VMs `ClusterNodeViewModel` / `ClusterNodeDetailViewModel` live under `MultiClusterMgmtSys.Components.Clusters.ViewModels` (physical `Components/Clusters/ViewModels/`) — this is an existing inconsistency we preserve, not "fix". `Components/_Imports.razor` does not include `Components.Clusters.*` or `Components.Nodes.*`, so each page carries its own `@using` lines (see `ClusterDetail.razor` for the precedent).

## Goals / Non-Goals

**Goals:**
- Bring `/nodes/{ClusterId}` and `/nodes/{ClusterId}/{NodeName}` back online with the project's current visual vocabulary (toolbar + filter bar + paged table; toolbar + stacked MudCards).
- Provide a working destination for the Drawer's "节点管理" entry (`Href="/nodes"`) by adding a parameterless `/nodes` route to the same `Nodes.razor` page component, rendering a "请先选择一个集群" empty-state card with a "前往集群列表" button. This avoids the 404 that surfaced during the post-implementation smoke test (Drawer.razor link → no matching route).
- Decompose each page into small shared components under `Components/Nodes/Shared/`, mirroring `Components/Clusters/Shared/`, so individual cards can be reasoned about and re-styled in isolation.
- Restore `ClusterNodesCard`'s "查看全部" link to a working destination with zero edits to that file.
- Preserve the read-only surface: only Back + Refresh in toolbars; no new K8s write operations.

**Non-Goals:**
- No Cordon / Drain / Uncordon (write actions against `node.Spec.unschedulable`) — explicitly deferred.
- No node persistence to SQLite — nodes remain a live K8s read; `ClusterInfo` is the only persisted entity, unchanged.
- No server-side paging of nodes — K8s `ListNode` returns the whole cluster's nodes in one call (~tens to low hundreds); client-side paging inside `MudTable` is sufficient and avoids a fake `ServerData` adapter that would slice in memory anyway.
- No sidebar on the node list page — nodes belong to exactly one cluster (already chosen via the URL), so there is no equivalent of `GroupSidebar` to show.
- No renaming of existing VMs or moving of `ClusterNodeViewModel` / `ClusterNodeDetailViewModel` out of `Components/Clusters/ViewModels/`. The split is preserved as-is.
- No edits to `ClusterNodeService`, `ClusterService`, `ClusterNodeViewModel`, `ClusterNodeDetailViewModel`, or any child VM. No edits to `_Imports.razor`. No DB schema change. No new NuGet packages.

## Decisions

### Decision 1: Direct overwrite of the two commented Pages.

The fully-commented `Nodes.razor` (418 lines) and `NodeDetail.razor` (532 lines) are overwriten entirely. The commented-out bodies reference dead namespaces (`MultiClusterMgmtSys.Services`, `MultiClusterMgmtSys.Models`, `MultiClusterMgmtSys.ViewModels`) that do not exist in the current codebase, so there is no salvageable logic — only fragments worth copy-adapting are the `GetNodeStatusColor` / `GetConditionColor` helpers and the table column shape, both of which are trivially re-derived.

**Alternative considered:** keep the old files as `.razor.bak` for reference. Rejected — git history already preserves them, and leaving `.bak` files in a feature folder violates the "no loose files" norm.

### Decision 2: New `Components/Nodes/Shared/` directory with 14 components.

Mirror the `Components/Clusters/Shared/` decomposition pattern. Concrete file list:

| File | Mirrors | Purpose |
|---|---|---|
| `NodeDetailToolbar.razor` | `ClusterDetailToolbar.razor` | `MudPaper pa-4 mb-4`: Back button + `node.Name` h4 + Ready chip + Refresh button. Params: `Node` (`ClusterNodeDetailViewModel`), `Processing` (bool), `OnBack`, `OnRefresh` EventCallbacks. |
| `NodeListToolbar.razor` | (top of `Clusters.razor` toolbar) | `MudPaper pa-4 mb-4`顶部: Back-to-cluster-detail button + cluster name h4 + cluster status chip + Refresh button. Params: `Cluster` (`ClusterDetailViewModel`), `Processing`, `OnBack`, `OnRefresh`. |
| `NodeClusterSidebar.razor` | `GroupSidebar.razor` | Left column of the list page. 240px `MudPaper`, header "集群选择", `MudNavMenu` listing clusters grouped by `GroupName` ("未分组" last). Each group is a collapsible section (per-group expand/collapse). Cluster rows: name + status color-dot (`ClusterStatus` → Success/Error/Default), active-link highlight for `SelectedClusterId`, click → `/nodes/{ClusterId}`. NO search box (user exclusion). Params: `Clusters` (`IReadOnlyList<ClusterViewModel>`), `SelectedClusterId` (`int?`), `OnClusterSelected` (`EventCallback<int>`). Data grouped internally (page passes the flat list from `ClusterService.GetClustersAsync()`). |
| `NodeListFilterBar.razor` | `ClusterFilterBar.razor` | `MudStack Row` with four `MudSelect`-style filters: Name (text), Role (`MudSelect<string?>`), Status (`MudSelect<string?>`), Schedulability (`MudSelect<bool?>`). Binds to a `NodeListFilter` record held by the parent page; emits `OnFilterChanged`. |
| `NodeListTable.razor` | `ClusterTable.razor` (Items-mode half) | `MudTable<ClusterNodeViewModel>` with `Items="@FilteredNodes"`, `Dense`, `Hover`, `Pager` (client-side). Six columns matching old page: 名称 (clickable → detail), 状态 (chip), 角色, Kubelet 版本, 操作系统, 内网 IP. `NoRecordsContent` mirrors the empty-state copy. Exposes a row-click handler param. |
| `NodeOverviewCard.razor` | `ClusterOverviewCard.razor` | Overview 6 fields: Name / Status (chip) / Roles / KubeletVersion / OsImage / InternalIP. |
| `NodeSchedulingCard.razor` | (no direct cluster analog — closest is part of Overview) | Unschedulable (chip: 可调度/不可调度 with Warning/Success color), Phase, PodCIDR. |
| `NodeMetadataCard.razor` | (no direct cluster analog) | Uid, CreatedAt (`yyyy-MM-dd HH:mm`). |
| `NodeAddressesCard.razor` | `ClusterEndpointsCard.razor` (table-only block) | `MudTable` of `NodeAddressViewModel` (Type / Address). |
| `NodeConditionsCard.razor` | (no direct cluster analog) | `MudTable` of `NodeConditionViewModel` with status chip colored via `GetConditionColor(type, status)` (copied verbatim from old `NodeDetail.razor:526-531`). |
| `NodeTaintsCard.razor` | (no direct cluster analog) | `MudTable` of `NodeTaintViewModel` (Key / Value / Effect). |
| `NodeLabelsCard.razor` | (no direct cluster analog) | `MudTable` of label KV pairs with ellipsis on the value cell (existing pattern). |
| `NodeAnnotationsCard.razor` | twin of `NodeLabelsCard.razor` | Same layout for annotations. |
| `NodeResourcesCard.razor` | (no direct cluster analog) | Side-by-side `MudTable`s for Capacity and Allocatable KV pairs (mirrors old `NodeDetail.razor:147-201`). |
| `NodeSystemInfoCard.razor` | overview-style grid | 10-field `MudGrid` of `NodeSystemInfoViewModel` (Architecture / BootID / ContainerRuntime / Kernel / KubeProxy / Kubelet / MachineID / OS / OsImage / SystemUUID). |

All shared components follow the same parameter convention as `ClusterNodesCard.razor` / `ClusterOverviewCard.razor`: a `[Parameter] public ClusterNodeDetailViewModel Node { get; set; } = default!;` (or `Cluster` for the toolbar / `ClusterNodeViewModel` context for the list row details where needed), and the parent page owns data loading + snackbar + navigation.

### Decision 3: Detail page layout grid for the cards.

`NodeDetail.razor` arranges cards in a single `MudStack Item="12"` vertical flow using `mb-4` margins on each card, exactly as `ClusterDetail.razor` does. Cards read in this order to match the old logical grouping:

```
NodeOverviewCard         full width       (概要)
NodeSchedulingCard       full width       (调度信息)
NodeMetadataCard         full width       (元数据)
NodeResourcesCard        full width       (资源容量 — Capacity + Allocatable side by side inside the card)
NodeAddressesCard        xs=12 md=6  ┐
NodeConditionsCard       xs=12 md=6  ┤  paired row
NodeTaintsCard           xs=12 md=6  ┐
NodeLabelsCard           xs=12 md=6  ┤  paired row
NodeAnnotationsCard      xs=12 md=6  ┐
NodeSystemInfoCard       xs=12 md=6  ┘  paired row (or SystemInfo full-width — design allows either; choose full-width to match the old 10-field grid)
```

To keep the page composed of pure stacked cards (mirroring `ClusterDetail`'s vertical rhythm) AND allow pairing, the page wraps paired cards in `<MudGrid><MudItem xs=12 md=6>...</MudItem><MudItem xs=12 md=6>...</MudItem></MudGrid>`, while full-width cards stay direct children of the outer `MudStack`. `NodeSystemInfoCard` is full width because the old page put it on a 10-column grid that reads better as one wide block.

### Decision 4: List page layout — two-column shell mirroring `Clusters.razor`.

```
<MudStack Row="true" Class="d-flex flex-auto">
  <NodeClusterSidebar .../>        ← 240px MudPaper: 集群选择 | 分组(可折叠) → 集群行 | 选中高亮 | 状态色点
  <MudStack Class="flex-auto">
    <NodeListToolbar .../>          ← 右栏:返回集群详情 | 集群名 h4 | 状态 chip | 刷新
    <NodeListFilterBar .../>         ← 右栏:MudPaper pa-4 mb-4 四个筛选控件
    <NodeListTable .../>             ← 右栏:MudTable Items=@filteredNodes + Pager + NoRecords
  </MudStack>
</MudStack>
```

The page mirrors the cluster management page's left-nav + right-content pattern. The left column is a new `NodeClusterSidebar` component (see component table below): 240px `MudPaper` styled after `GroupSidebar`, listing clusters grouped by `GroupName` with each group section collapsible (no search box in this iteration — explicit user exclusion). Clicking a cluster navigates to `/nodes/{ClusterId}`; the row whose id matches the URL parameter gets the active-link highlight (primary background + primary-text, same CSS treatment as `GroupSidebar`'s `.active` link). The right pane holds the existing list toolbar, filter bar, and table.

**Layout evolution record (three iterations, kept for archive trace):** (1) original plan had NO sidebar — single-column page, because "the cluster is already fixed by the URL"; (2) after the 404 smoke complaint, a parameterless `/nodes` landing card with auto-redirect was added; (3) the user rejected the landing-card + auto-redirect UX as "怪怪的" and requested this final shape: a persistent left cluster-selection sidebar always visible, with the right pane showing either the remembered cluster's node list or an empty-state hint. The landing card, the "前往集群列表" button, and the URL auto-redirect are all retired. State-render order in `Nodes.razor`: right pane renders the remembered-cluster list / empty state when `ClusterId` is null, else toolbar → not-found → unreachable → filter bar + table.

### Decision 8: Cluster-selection memory — scoped service, in-place restore (no URL redirect).

The user's core requirement ("切到其他页再切回来,应该还是之前的选择") is satisfied by URL state when the URL carries the cluster id (`/nodes/{ClusterId}`). But the Drawer's "节点管理" link is statically `/nodes` — landing there must not force the user to re-pick from the sidebar every time. Resolution: keep the shared scoped `ClusterSelectionState` service (`Components/Common/ClusterSelectionState.cs`, `AddScoped` in `Program.cs`, `int? SelectedClusterId` + `Set(int)` / `Clear()`, in-memory only — the user chose "会话内 scoped 服务" over `localStorage`). Write sites: `Nodes.razor.OnParametersSet` (when `ClusterId.HasValue`) and `NodeDetail.razor.OnParametersSet` (always). Read site: `Nodes.razor` on the parameterless route — if `SelectedClusterId` has a value, load and render that cluster's node list in the right pane WITHOUT navigating (URL stays `/nodes`), and highlight the row in the sidebar; if null, render the empty-state hint.

**Why not auto-redirect (`NavigateTo`)?** The earlier implementation redirected `/nodes` → `/nodes/{id}`; the user found the URL change plus the intermediate landing card jarring. In-place rendering keeps the Drawer nav stable (`/nodes` stays `/nodes`) while the content restores — the same behavior the cluster page gives for its group selection.

**Implementation trap — `int?` diff-vs-first-frame collision.** Naïvely writing `if (ClusterId != previousClusterId) { ... }` fails on the very first visit to `/nodes` because both sides are `null` (`int?` default), so `(null != null) == false` skips the entire branch. This bug made the earlier persistence feature appear to "have no effect". Resolution: gate the early-return with a `hasInitialized` boolean flag (field default `false`, set `true` at the bottom of the first `OnParametersSetAsync` run). The correct shape:

```csharp
if (ClusterId == previousClusterId && hasInitialized) return;
hasInitialized = true;
previousClusterId = ClusterId;
// ... then the has-value / null branches (LoadAsync / in-place memory restore) as before
```

Any follow-on change to `Nodes.razor` MUST preserve this first-frame guarantee; the spec requirement "Cluster selection is expressed in the URL and remembered in session" pins a scenario to it (`Scenario: Return nav restores the last cluster in place`).

**Alternatives considered:**
- (B) `localStorage` via `IJSInterop` (mirroring `ThemeManager` exactly). Rejected by the user as over-persistent for this UX choice; adds JS interop try/catch boilerplate.
- (C) Dynamic Drawer `Href` reading the scoped service. Rejected — needs a custom Drawer with conditional `Href`; in-place restore from `/nodes` achieves the same effect with zero Drawer edits.

### Decision 5 (context adjust): filter state still lives in the page; selected-cluster memory is separate.

`NodeListFilterBar.razor` binds to a `NodeListFilter` record (`Name` string, `Role` string?, `Status` string?, `Schedulable` bool?) declared in the page `@code` (or in a small `Components/Nodes/Requests/NodeListFilter.cs` if the page grows). The `NodeListTable` receives a `Func<ClusterNodeViewModel, bool>` predicate from the page's `FilteredNodes` computed property — exactly the existing pattern in the old `Nodes.razor` (`filteredNodes` getter, lines 299-308). No new service, no repository change, no DB impact. The Status filter values map one-to-one to `ClusterNodeViewModel.Status` ("Ready" / "NotReady" / "Unknown"); the Schedulability filter requires the list VM to also surface `Unschedulable`.

**Decision (resolves R3 below): option (a).** `ClusterNodeViewModel` gains one read-only `bool Unschedulable` field, populated from `node.Spec?.Unschedulable ?? false` in the existing `MapNode` mapper. This is a purely additive field (no existing consumer reads `Unschedulable` on the list VM today, so none break), and it does not change any service method signature. The "no service contract change" wording in the proposal is honored in spirit: nothing adds a new method, removes a method, or changes a parameter — a single setter line is added inside an existing private mapper. The detail-page VM (`ClusterNodeDetailViewModel`) already has `Unschedulable` and is untouched.

Note: the filter state itself is intentionally NOT persisted (the `NodeListFilter` instance resets when the list component unmounts). The persisted bit is only the selected cluster id, matching the user's request. Filter state restoration across `/nodes` ↔ other-page visits is a future change if it becomes a complaint.

### Decision 6: Toolbar not gated by `AuthorizeView`.

`ClusterDetailToolbar` wraps its 刷新状态/编辑/删除 buttons in `<AuthorizeView Roles="Admin">` because those are mutating cluster actions. Node toolbars' only action is Refresh, which is a read. Refresh is therefore unconditionally rendered (mirroring the old `Nodes.razor:152-159` 刷新 button which had no `AuthorizeView`), preserving the read-only public surface.

### Decision 7: Status color helpers remain per-component.

The `GetNodeStatusColor(string)` and `GetConditionColor(string, string)` helpers are small `private static` functions on each component that needs them, copied verbatim from the old pages. Promoting them to a shared `NodeStatusColors` static utility is tempting but out of scope (no other consumer exists). Reminder to self: the helpers must match the old semantics exactly — `GetConditionColor` returns `Color.Default` for "Unknown", `Color.Success`/`Color.Error` for healthy/unhealthy with the type-aware inversion (`Ready ? "True" : "False"`).

## Risks / Trade-offs

- **[R1] Dead-code risk during transition** → Mitigation: implementation task overwrites the two `.razor` files atomically (one task), so the route is never half-broken. There is no intermediate state where users see a mix of old UI and new cards.

- **[R7] Drawer nav targets a route removed by the rewrite — RESOLVED (final shape)** → `Href="/nodes"` points at the parameterless route that the legacy TreeView-style `Nodes.razor` used to register via `@page "/nodes"`. The rewrite initially dropped it (only `@page "/nodes/{ClusterId:int}"` survived), giving a 404 on the "节点管理" sidebar entry. Iteration history: (a) re-added `@page "/nodes"` with a landing card + auto-redirect; (b) the user rejected that UX as "怪怪的" and requested the cluster-management-style two-column layout, so the final shape is a persistent `NodeClusterSidebar` + right pane, with the landing card and auto-redirect retired. → Verified: `/nodes` renders sidebar + (empty or remembered) right pane, `/nodes/{ClusterId}` renders sidebar + node list, `/nodes/{ClusterId}/{NodeName}` renders detail.

- **[R8] Loss of cluster-selection context on intra-session navigation — RESOLVED (in-place restore)** → Without memory, the user's selected cluster id is lost when they side-track to `/clusters` / `/configmaps` / `/accounts` and return via the Drawer "节点管理" link (lands on `/nodes` empty pane again). Resolution (Decision 8): the shared scoped `ClusterSelectionState` writes from `Nodes.razor` and `NodeDetail.razor` whenever they render with a non-null `ClusterId`, and the parameterless `/nodes` route reads it back to render the remembered cluster's node list IN PLACE (URL unchanged, no redirect). **Trade-off**: state lives only in the scoped instance — a full browser refresh clears the choice (acceptable per user's explicit "会话内" option choice). The in-place restore path is tested during smoke.

- **[R2] Namespace drift** → The `Components/Nodes/**` namespace rule (`Components.Nodes.*`, NOT `Features.Nodes.*`) and the `Components/Clusters/ViewModels/`-hosts-`ClusterNodeViewModel` quirk both invite copy-paste errors. Mitigation: each new file's `@using` block is copied from the immediately-sibling file in the same folder (`ClusterNodesCard.razor` for the card pattern, `ClusterDetail.razor` for the page pattern) and the task list explicitly calls out re-reading a sibling before writing any new `@using`.

- **[R3] Schedulability filter needs unschedulable on the list VM — RESOLVED** → `ClusterNodeViewModel` today has 6 fields and does NOT expose `Unschedulable`. Resolution (Decision 5): add one `bool Unschedulable` to `ClusterNodeViewModel` and one setter line in `ClusterNodeService.MapNode` (`Unschedulable = node.Spec?.Unschedulable ?? false`). Purely additive; no consumer breaks; no method-signature change. The filter bar's Schedulability `MudSelect<bool?>` binds against this new field.

- **[R4] Detail card inflation** → 14 new `.razor` files is the mostFine-grained decomposition in the repo (`Components/Clusters/Shared/` has 9). Trade-off accepted because the alternative — one giant `NodeDetail.razor` with `@code` length 500+ — is the exact shape we're retiring. Each card stays small (under ~70 lines) and stays symmetric with `ClusterOverviewCard` / `ClusterEndpointsCard`.

- **[R5] Client-side paging over server paging** → `MudTable`'s `Pager` with `Items=...` paginates in memory. For clusters up to ~200 nodes this is fine; a large fleet (>1000 nodes) would benefit from server paging, but that requires a service-layer change that is out of scope per the user's explicit "只重做" constraint. Recorded here so a future `nodes-server-paging` change can pick it up.

- **[R6] No admin guard on Refresh** → Refresh is a pure read. This is consistent with the rest of the app's read paths (cluster list and detail are reachable to all authenticated users per `ClusterDetail.razor`'s non-`AuthorizeView` toolbar header showing for everyone — only edit/delete is admin-gated). No change required.

## Migration Plan

1. Implementation is local-only; no production data, no schema, no config — drop bins, run, smoke-test the two routes.
2. Apply order is fixed (tasks.md enforces): create `Shared/` files bottom-up (cards → detail toolbar → list helpers) → write `NodeDetail.razor` → write `Nodes.razor` → `dotnet build` → manual smoke on both routes against a real K8s cluster.
3. Rollback = `git revert` the change commit (the dead-code pages remain recoverable from git history if needed).

## Open Questions

- (none remaining — R3 / OQ1 resolved in Decision 5.)