## Context

The cluster feature established two visual templates during the in-flight `redesign-cluster-detail-with-endpoints`, `refactor-clusters-group-sidebar-layout`, and `redesign-nodes-page` changes:

**List page template** (see `Components/Nodes/Pages/Nodes.razor` for the canonical reference):
```
<MudStack Class="d-flex flex-auto">
  @if (!ClusterId.HasValue) {  <fallback: 请先选择一个集群 + 前往集群列表 button → /clusters> }
  else if (loading) { <MudProgressLinear/> }
  else if (cluster is null) { <未找到该集群 + 返回集群列表 button> }
  else if (!cluster.IsReachable) { <集群不可达 message> }
  else {
    <NodeListToolbar .../>        ← MudPaper pa-4 mb-4
    <NodeListFilterBar .../>      ← MudPaper pa-4 mb-4 (or merged into toolbar paper in compact variants)
    <NodeListTable .../>          ← MudTable Items + client Pager + NoRecords
  }
</MudStack>
```

**Detail page template** (`Components/Clusters/Pages/ClusterDetail.razor`):
```
<MudStack Class="flex-auto">
  <XxxDetailToolbar .../>   ← MudPaper pa-4 mb-4: 返回 | 标题 h4 | chip | (Edit/Delete gated)
  <XxxOverviewCard .../>    ← MudCard Elevation=1 Class="mb-4"
  <XxxMoreCard .../>
  ...
</MudStack>
```

The ConfigMap pages were written in the older style — left TreeView cluster-picker + right self-contained content column — and **all five `.razor` files in `Components/Configmaps/Pages/` and `Shared/` are currently wrapped in `@* ... *@`** (537 + 118 + 258 + 152 + 168 dead lines). `Components/Layout/Drawer.razor:12` still links to `/configmaps`, so the dead pages are a live 404 path.

The old design additionally carried a **semantic inconsistency** that this redesign must resolve, not just preserve:

| Path | Handled `Data` | Handled `BinaryData` | Handled `Labels`/`Annotations` |
|---|---|---|---|
| `CreateConfigMapAsync(ConfigMapCreateViewModel)` | ✅ | ❌ never read | ❌ |
| `UpdateConfigMapAsync(ConfigMapUpdateViewModel)` | ✅ | ❌ left server-side stale value intact | ❌ |
| `GetConfigMapAsync` → `ConfigMapDetailViewModel` | ✅ | ❌ not surfaced | ❌ |
| `UpdateConfigMapFromYamlAsync` | ✅ | ✅ | ❌ (preserves existing metadata intentionally) |

Result: a ConfigMap containing `binaryData` rendered as if it had no `binaryData`, and the form-editor save path silently preserved whatever `binaryData` the server had — a real-but-invisible state. The redesign collapses the feature to a single **YAML-first mental model**: read = only-read YAML view (which contains `binaryData` and `labels`/`annotations` for free), edit = YAML submit, create = YAML submit. The form-editor route is deleted; the form-based VMs are deleted; the form-based create/update service methods become dead code (signatures kept during this change — see Decision 7 for the "why not also delete them" reasoning).

Service/VM layer already partially supports the new shape:
- `ConfigMapService.GetConfigMapAsync` populates `ConfigMapDetailViewModel.Yaml` (`ConfigMapMappingExtensions.ToConfigMapDetailViewModel` already serializes the full `V1ConfigMap`).
- `ConfigMapService.UpdateConfigMapFromYamlAsync` already does the "keep existing metadata, overwrite Data + BinaryData" dance — exactly the contract the design wants to preserve.
- `ConfigMapService.ListConfigMapsAsync` → `List<ConfigMapListViewModel>` reused as-is.

Namespace rule (from AGENTS.md): `Components/Configmaps/**` uses `MultiClusterMgmtSys.Features.Configmaps.*` (NOT `Components.*`). This is the **opposite** of the Nodes rule and is one of two known namespace quirks in the repo. `Components/_Imports.razor` does not include `Features.Configmaps.*`, so each page carries its own `@using` lines (mirroring the commented `ConfigMaps.razor` and the live `Nodes.razor` precedent).

## Goals / Non-Goals

**Goals:**
- Bring `/configmaps`, `/configmaps/{ClusterId}`, `/configmaps/{ClusterId}/{Namespace}/{Name}`, and `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml` back online with the project's current visual vocabulary (toolbar + filter bar + paged table; toolbar + stacked `MudCard`).
- Decompose each page into small shared components under `Components/Configmaps/Shared/`, mirroring `Components/Nodes/Shared/`, so individual cards can be reasoned about in isolation.
- Make `binaryData` parityinvisible-by-construction: read = full YAML (contains `binaryData`), edit = full YAML submit (overwrites `binaryData`), create = full YAML submit.
- Restore `Drawer.razor`'s `/configmaps` link to a working destination with zero edits to that file.
- Clean up the now-dead form-based VMs in `Components/Configmaps/ViewModels/` as part of the same change (Decision 10).
- Preserve the existing service contract for `UpdateConfigMapFromYamlAsync` exactly — its "only overwrites `Data` + `BinaryData`, preserves `existing` metadata" semantics is the explicit design contract for the YAML edit path.

**Non-Goals:**
- No Secret support — ConfigMap is the only resource type scoped in. A future `add-secret-page` change can reuse the visual scaffolding.
- No cross-cluster capability — list/detail/edit/create are all single-cluster contexts scoped by `ClusterId`. A future `configmaps-cross-cluster-diff` change is on the table.
- No server-side paging of ConfigMaps — K8s `ListNamespacedConfigMap` returns the whole namespace in one call (--tens to low hundreds of ConfigMaps); client-side paging inside `MudTable` is sufficient (matches the Nodes page decision).
- No sidebar on the list page — ConfigMaps belong to exactly one cluster (already chosen via the URL), so there is no equivalent of `GroupSidebar`.
- No introduction of monaco / codemirror / syntax-highlight editor — the YAML editor is a plain `MudTextField` (Lines=30, monospace, RO/RW). A future `configmaps-yaml-editor-upgrade` change can pick that up.
- No undo on delete — a 2-step confirm dialog followed by immediate K8s delete (matches the user's explicit "先不做 undo" decision; mirrors current behavior).
- No edits to `_Imports.razor`. No edits to `ClusterService`, `ClusterDetailViewModel`, or any non-Configmaps file. No DB schema change (ConfigMaps remain a live K8s read/write; `ClusterInfo` is the only persisted entity, unchanged). No new NuGet packages (uses `KubernetesYaml` already pulled in by `KubernetesClient` 19).
- No edits to the existing method **signatures** of `ConfigMapService` (Decision 7 explains why dead-code methods are kept in the file rather than removed during this change).

## Decisions

### Decision 1: Direct overwrite of the four commented Pages + rewrite of the commented Shared dialog.

`ConfigMaps.razor` (537), `ConfigMapDetail.razor` (118), `EditConfigMapYaml.razor` (152), `EditConfigMap.razor` (258), and `CreateConfigMapDialog.razor` (168) are overwritten entirely. `EditConfigMap.razor` is **deleted**, not overwritten. The commented-out bodies reference non-existent namespaces in `Nodes.razor` (e.g. `MultiClusterMgmtSys.Models`) but `ConfigMaps.razor` actually uses the current `Features.Configmaps.*` namespaces correctly — there is salvageable binding code worth copy-adapting (the `MapNode`-equivalent) `GetClusterStatusColor` / `GetClusterStatusIcon` helpers, the `filteredConfigMaps` predicate, the namespace-select `MudSelect` wiring). The rewrite copies those fragments where useful but composes them into the new MudStack/Toolbar/Card skeleton.

**Alternative considered:** keep the commented files as `.razor.bak`. Rejected — git history already preserves them; leaving `.bak` files violates the "no loose files" norm established by the Nodes change.

### Decision 2: New `Components/Configmaps/Shared/` components, mirroring `Components/Nodes/Shared/`.

Concrete file list:

| File | Mirrors | Purpose |
|---|---|---|
| `ConfigMapListToolbar.razor` | `NodeListToolbar.razor` | `MudPaper pa-4 mb-4`: 返回集群详情 button (→ `/clusters/{ClusterId}`) + cluster name h4 + status `MudChip` + 刷新 `MudButton`. Params: `Cluster` (`ClusterDetailViewModel`), `Processing` (bool), `OnBack`, `OnRefresh` `EventCallback`. |
| `ConfigMapListFilterBar.razor` | `NodeListFilterBar.razor` | `MudPaper pa-4 mb-4`: 命名空间 `MudSelect<string?>` (options from `ConfigMapService.GetNamespacesAsync`, with empty-selection = 全部命名空间) + 名称 `MudTextField<string>` (客户端过滤) + 新建 ConfigMap `MudButton` (inside `<AuthorizeView Roles="Admin">`, Disabled when `cluster is null \|\| !cluster.IsReachable`). Params: `Cluster`, `Namespaces` (`List<string>`), `Filter` (record bound to page state), `OnFilterChanged`, `OnCreate` `EventCallback`. |
| `ConfigMapListTable.razor` | `NodeListTable.razor` | `MudTable<ConfigMapListViewModel>` Items=`@FilteredConfigMaps` + Dense + Hover + client `Pager` + `NoRecordsContent` mirroring the empty-state copy from old `ConfigMaps.razor:212-228`. Six columns: 名称 (clickable → detail route), 命名空间 (`MudChip Small`), Data 键数, 键名预览 (ellipsis cell), 创建时间 (`yyyy-MM-dd HH:mm`), 操作 (icon buttons: 详情跳列表内导航 / 编辑 YAML Admin-gated / 删除 Admin-gated). Params: `Items`, `OnNavigateDetail(ns,name)`, `OnNavigateEditYaml(ns,name)`, `OnDelete(ns,name)` callbacks. |
| `ConfigMapDetailToolbar.razor` | `NodeDetailToolbar.razor` | `MudPaper pa-4 mb-4`: 返回列表 button (→ `/configmaps/{ClusterId}`) + `{cm.Name}` h4 + `MudChip` showing "Data 键数: {n}" (computed from `detail.Data.Count`) + 编辑 YAML `MudButton` (inside `<AuthorizeView Roles="Admin">`) + 刷新 `MudButton`. Params: `ClusterId` (int), `Name` (string), `Detail` (`ConfigMapDetailViewModel`), `Processing`, `OnBack`, `OnRefresh`, `OnEditYaml` `EventCallback`. |
| `ConfigMapYamlViewCard.razor` | (no Nodes analog — Nodes detail has no large read-only text block; closest is `ClusterOverviewCard` grid) | `MudCard Elevation=1 Class="mb-4"` containing `<MudCardContent>` with a read-only `MudTextField<string>` Lines=30 monospace, `Value="@Yaml"`, `ReadOnly="true"`. Renders nothing if `Yaml` is null/empty (a "暂无 YAML" empty-state). Params: `Yaml` (string). |
| `ConfigMapYamlEditCard.razor` | (par. of `ConfigMapYamlViewCard`) | `MudCard Elevation=1 Class="mb-4"` containing `<MudCardContent>` with an editable `MudTextField<string>` Lines=30 monospace, `@bind-Value="Yaml"`. Save handler lives in the parent page (toolbar), not in the card. Params: `Yaml` (string, bindable two-way via `YamlChanged` `EventCallback<string>`). |
| `CreateConfigMapDialog.razor` (rewritten in place) | (rewrite of the existing dialog file, not a new component) | `MudDialog` body containing a single `MudTextField<string>` Lines=25 monospace `@bind-Value="yamlContent"` pre-filled on init with the minimal `V1ConfigMap` template (see Decision 6). `DialogActions`: 取消 + 创建. Submit handler pre-parses with `KubernetesYaml.Deserialize<V1ConfigMap>`; on success calls `ConfigMapService.CreateConfigMapFromYamlAsync(ClusterId, yamlContent)` and closes the dialog; on parse failure shows the exception text via `Snackbar` and does not close. Params: `ClusterId` (int), `Dialog` (`IMudDialogInstance` via `[CascadingParameter]`). |

All shared components follow the same parameter convention as `ClusterNodesCard.razor` / `NodeListToolbar.razor`: a `[Parameter]` per needed datum, and `EventCallback` per action the parent owns. The parent page owns data loading, snackbar, navigation, and dialog coordination — components are stateless renderers.

### Decision 3: List page layout, sans sidebar.

```
<MudStack Class="d-flex flex-auto">
  @if (!ClusterId.HasValue) {
    <请先选择一个集群 fallback: icon + h6 + body2 + 前往集群列表 button → /clusters>
  }
  else if (loading) { <MudProgressLinear Indeterminate="true" Class="my-4"/> }
  else if (cluster is null) { <未找到该集群 + 返回集群列表 button → /clusters> }
  else if (!cluster.IsReachable) { <MudCard>集群不可达，无法获取 ConfigMap</MudCard> }
  else {
    <ConfigMapListToolbar Cluster="@cluster" Processing="@loading" OnBack=... OnRefresh=.../>
    <ConfigMapListFilterBar Cluster="@cluster" Namespaces="@namespaces" Filter="@filter"
                            OnFilterChanged=... OnCreate=.../>
    <ConfigMapListTable Items="@filteredConfigMaps" ...callbacks.../>
  }
</MudStack>
```

No `GroupSidebar` analog — the cluster is fixed by URL, mirroring the Nodes decision. The `/configmaps` (no `ClusterId`) variant renders the fallback (mirrors `Nodes.razor:14-27`), satisfying the user's "和 /nodes 的逻辑一样" decision.

### Decision 4: Detail page = single read-only YAML view.

`ConfigMapDetail.razor` layout:
```
<MudStack Class="flex-auto">
  <ConfigMapDetailToolbar .../>
  <ConfigMapYamlViewCard Yaml="@detail?.Yaml"/>
</MudStack>
```

The old `MudTabs` per-key-TabPanel pattern is dropped. The `ConfigMapDetailViewModel.Yaml` field already contains the serialized full `V1ConfigMap` (via `ToConfigMapDetailViewModel` in `ConfigMapMappingExtensions`), so showing only the YAML gives the user `data` + `binaryData` + `labels` + `annotations` + `metadata` in one view, eliminating the binaryData-parity bug at the read side.

**Alternative considered:** keep a small "Data keys" overview card on top of the YAML, showing just key names + sizes (mirrors Nodes' "Overview card then detail cards" rhythm). Rejected — for a single-resource detail page this duplicates the YAML's header rows without adding clarity; the YAML itself is the overview. The "Data 键数: N" chip in the toolbar gives a one-glance summary.

### Decision 5: Edit path = YAML only; form-editor route deleted.

`/configmaps/{ClusterId}/{Namespace}/{Name}/edit` is removed entirely. `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml` is the only write path for an existing ConfigMap. The list-page row actions expose exactly three icons: 详情 (navigate), 编辑 YAML (admin-gated, navigates to `/yaml`), 删除 (admin-gated, confirms + deletes). The list page no longer surfaces a "form-edit" entry point.

`UpdateConfigMapFromYamlAsync` is reused unmodified; its current "read existing → overwrite `Data` + `BinaryData` → keep existing metadata → Replace" semantics is the explicit design contract, resolving the user's "保持现状" decision on whether YAML edit can change metadata (it cannot).

### Decision 6: Create dialog collapsed to YAML-only with pre-filled minimal template.

The dialog body is a single `MudTextField<string>` Lines=25 monospace, initialized on `OnInitializedAsync` to:
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: 
  namespace: 
data: {}
```

Submit pre-parses with `KubernetesYaml.Deserialize<V1ConfigMap>` (resolving the user's "预填最小模板" decision). On parse failure, the exception message is shown via `Snackbar` and the dialog stays open. On parse success, calls the new `ConfigMapService.CreateConfigMapFromYamlAsync(ClusterId, yamlContent)`; on K8s API conflict (`409` / `Already Exists`), shows "同名 ConfigMap 已存在"; on other errors shows the raw exception message — the error-mapping pattern from the existing `CreateConfigMapDialog.razor:157-162` (commented) is preserved verbatim.

The old form-based "add data entry" / "namespace `MudSelect` from `GetNamespacesAsync`" logic is dropped. Users type a YAML document; the K8s API server validates `metadata.name` shape, namespace existence, and data/binaryData key constraints on submit. The pre-parse step catches structural YAML errors before the network round-trip.

**Alternative considered:** keep the `MudSelect` for namespace + only let the user author `data`/`binaryData` YAML. Rejected — splits the mental model back into form/YAML hybrid territory, which is exactly the inconsistency this change removes.

### Decision 7: Service layer gets one new method; dead VM-create/update methods are deleted along with their VMs.

`ConfigMapService` gains one new method:
```csharp
public async Task CreateConfigMapFromYamlAsync(int clusterId, string yaml)
{
    var entity = await repo.GetByIdAsync(clusterId)
        ?? throw new InvalidOperationException($"Cluster {clusterId} not found");
    var config = BuildConfig(entity);
    using var client = new Kubernetes(config);
    var body = KubernetesYaml.Deserialize<V1ConfigMap>(yaml);
    var ns = body.Metadata?.NamespaceProperty;
    if (string.IsNullOrWhiteSpace(ns))
        throw new InvalidOperationException("YAML metadata.namespace 未指定");
    await client.CoreV1.CreateNamespacedConfigMapAsync(body, ns);
}
```

The existing `CreateConfigMapAsync(ConfigMapCreateViewModel)` and `UpdateConfigMapAsync(ConfigMapUpdateViewModel)` methods are **deleted in the same change** as their VM types (Decision 10). Initial exploration considered keeping the dead methods as a stability / scope-narrowing measure (the original Draft Decision 7 in proposal.md/pre-merge design.md), but during apply this turned out to be incoherent — the dead method signatures **reference the dead VM types**, so keeping the methods would force keeping the VMs. The "narrow scope for review" rationale collapses when one kept-dead-thing forces keeping another kept-dead-thing. The coherent move is to delete all four together (2 methods + 2 VMs + the per-entry VM), so reviewers see one clean cut: "the form-based write path is gone, both at the service boundary and at the VM boundary." A future `cleanup-configmap-dead-service-methods` change is therefore unnecessary and is removed from the risks list.

**Alternative considered (original Draft):** keep the dead methods in the file, leave the follow-up to delete them. Rejected because the methods' parameter types are the very types Decision 10 deletes. Originally drafted before the VM-cleanup was scoped in (Decision 10); the two are intertwined and must move together.

### Decision 8: Admin gating — write actions gated, read actions not gated.

Mirrors the Nodes-page decision exactly, applied to the write-capable ConfigMap pages:
- List page: 刷新 + 命名空间切换 + 名称搜索 + 详情跳转 → readable by all authenticated users (no `AuthorizeView`).
- List page "新建 ConfigMap" button → wrapped in `<AuthorizeView Roles="Admin">`.
- Detail page "编辑 YAML" button → wrapped in `<AuthorizeView Roles="Admin">`.
- List page row "编辑 YAML" icon button → wrapped in `<AuthorizeView Roles="Admin">`.
- List page row "删除" icon button → wrapped in `<AuthorizeView Roles="Admin">`.
- Detail toolbar 刷新 button → not gated (read, mirrors Nodes refresh decision).

`@attribute [Authorize]` (no roles) is kept at the top of every page (mirrors `Nodes.razor:1` and `NodeDetail.razor`), so anonymous users are redirected to /login; only write actions are admin-gated further.

### Decision 9: Back button semantics — edit page 退 to list, not detail.

The user's "返回列表返回到当前集群的 configmap 列表页" decision is implemented literally: every "返回" / "返回列表" button in every page of this feature navigates to `/configmaps/{ClusterId}`. This includes the YAML editor's back button (the current `EditConfigMapYaml.razor:11` already does this; preserved). The old "返回详情 vs 返回列表" ambiguity in `EditConfigMap.razor` (where the button text said "返回列表" but the user expectation as expressed in discovery was "should have been back to detail") is moot because the form-editor page is deleted.

**Alternative considered:** have the YAML editor return to detail, since after editing the user likely wants to see the new YAML. Rejected — the user's explicit decision was "回到当前集群的 configmap 列表页"; after save we already NavigateTo list (Decision 5 preserves this behavior), and a manual back press from edit should go to the same destination to avoid "where am I after edit" surprise. Users wanting to see the updated YAML on the detail page can click the row again from the list (same as the current Nodes back-to-list flow).

### Decision 10: VM cleanup — delete three form-based VMs, trim MappingExtensions, narrow ConfigMapDetailViewModel.Data.

As part of this change:
- Delete `Components/Configmaps/ViewModels/ConfigMapCreateViewModel.cs`.
- Delete `Components/Configmaps/ViewModels/ConfigMapUpdateViewModel.cs`.
- Delete `Components/Configmaps/ViewModels/ConfigMapDataEntryViewModel.cs`.
- Modify `Components/Configmaps/ViewModels/ConfigMapDetailViewModel.cs` to change `public List<ConfigMapDataEntryViewModel> Data { get; set; }` to `public Dictionary<string, string> Data { get; set; } = new();`. Rationale: the detail page only reads `Data.Count` for the toolbar chip and renders the YAML for the actual content; the per-entry structured VM is no longer needed. Keeping `Data` as a Dictionary preserves the `Data.Count` accessor used by the chip while removing the dependency on `ConfigMapDataEntryViewModel`.
- Trim `Components/Configmaps/ViewModels/Mappings/ConfigMapMappingExtensions.cs`:
  - `ToConfigMapListViewModel(this V1ConfigMap)` — kept verbatim (used by `ConfigMapService.ListConfigMapsAsync`).
  - `ToConfigMapDetailViewModel(this V1ConfigMap)` — kept but the `Data` assignment changes from `cm.Data?.Select(kvp => new ConfigMapDataEntryViewModel { Key = kvp.Key, Value = kvp.Value ?? "" }).ToList()` to `cm.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? "") ?? new()`. `Yaml = KubernetesYaml.Serialize(cm)` unchanged.
  - Any other mapper helpers (none currently exist beyond the two above) are deleted.
- `ConfigMapListViewModel.cs` is reused unmodified.

This is the visible "we cleaned the dead form code" story. It's paired with the service-layer decision to keep the dead methods (Decision 7): the VMs are deleted because they're literally value-object types with no behavior and zero remaining references after the Razor rewrite, but the service methods are kept because they're public methods on a DI service and removing them is a contract change scoped for a follow-up.

**Alternative considered:** keep `ConfigMapDataEntryViewModel` so `ConfigMapDetailViewModel` can stay unmodified. Rejected — the only remaining consumer of the detail VM in the new UI reads `Data.Count` (chip) and `Yaml` (card); a Dictionary preserves both with no semantic loss and removes one more orphaned type.

### Decision 11: Save handlers do client-side pre-parse before the K8s API call.

Both `EditConfigMapYaml.razor` Save and `CreateConfigMapDialog.razor` Submit wrap their service call in a two-step handler:
1. `try { KubernetesYaml.Deserialize<V1ConfigMap>(yamlContent); } catch (Exception ex) { Snackbar.Add($"YAML 格式错误: {ex.Message}", Severity.Error); return; }`
2. Only on pre-parse success: `await ConfigMapService.CreateConfigMapFromYamlAsync(...)` or `UpdateConfigMapFromYamlAsync(...)`.

This resolves the user's "(c) textarea + 保存前 KubernetesYaml.Deserialize 解析预检" decision. The pre-parse step catches structural YAML errors before the round-trip; K8s API validation (key conflicts, label value shape, etc.) still happens server-side and is mapped to the Snackbar message as in the existing code (`409`/`Conflict` → "资源已被他人修改" or "同名 ConfigMap 已存在"; other → raw `ex.Message`).

The pre-parse code lives in the page/dialog `@code` block, not in the service — keeping the service a thin wrapper over `KubernetesClient` calls. Failure of step 1 does not modify any state (`yamlContent` stays, dialog stays open, user can fix and resubmit).

### Decision 12: Cluster-configmaps entrypoint + ClusterSelectionState refactor (discovered during apply).

During apply, the user reported that after picking a cluster and navigating away via the Drawer, returning to `/configmaps` (or `/nodes`) re-shows the 请先选择一个集群 fallback — the cross-feature context was not persisted. Investigation surfaced that `Components/Common/NodeSelectionState.cs` already existed as a scoped service implementing this for Nodes (renamed here to reflect generalisation below), but the `ConfigMaps.razor` rewrite initially copied only Nodes' fallback UI without the corresponding `selected-cluster-id` recovery logic. The same gap is also closed on the Nodes side because we centralise on a single shared service.

**Three coupled changes:**

1. **Rename `NodeSelectionState` → `ClusterSelectionState`** (file `Components/Common/NodeSelectionState.cs` → `ClusterSelectionState.cs`, class name same). The original name implied a Nodes-only concern; the service is purely "most recent cluster id visited", a generic feature-page-context primitive. DI registration in `Program.cs` and all 4 existing call sites (`Nodes.razor`, `NodeDetail.razor`, plus now `ConfigMaps.razor`, `ConfigMapDetail.razor`, `EditConfigMapYaml.razor`, `ClusterDetail.razor`) update to the new name. No behaviour change for Nodes — the underlying service implementation is byte-for-byte identical.

2. **`ConfigMaps.razor` invokes `ClusterSelectionState.Set` and recovers** — `LoadAsync` calls `ClusterSelection.Set(ClusterId.Value)` after a successful `GetClusterDetailAsync`. The existing fallback branch (the `else` when `!ClusterId.HasValue`) now reads `ClusterSelection.SelectedClusterId` and `NavigateTo($"/configmaps/{id}")` instead of rendering the static fallback. Mirrors `Nodes.razor:124-148` exactly.

3. **`ClusterDetail.razor` records the cluster id too** — its `LoadAsync` now calls `ClusterSelection.Set(Id)` after a successful detail load. This means a user who lands on `/clusters/3` (e.g. via Search or a Clusters list selection) and then clicks the Drawer's `/nodes` or `/configmaps` link receives the auto-recovery for free — without it, the user would have to first deep-link into a Nodes/ConfigMaps page before the service could record the id. Belt-and-suspenders: `ConfigMapDetail.razor` and `EditConfigMapYaml.razor`'s `OnInitializedAsync` also call `ClusterSelection.Set(ClusterId)` at entry; matches `NodeDetail.razor:96-99`'s pattern.

**New `ClusterConfigMapsCard`** entry component under `Components/Clusters/Shared/`, mirrored from `ClusterNodesCard.razor` minus the K8s list preview (Decision: pure navigation affordance, no extra K8s calls). The card sits below `ClusterNodesCard` on the `ClusterDetail.razor` page and exposes a 查看全部 (→ `/configmaps/{Cluster.Id}`) button. The button is `Disabled="@(!Cluster.IsReachable)"`. This answers the user's discovery question "怎么跳过来呢" (how does one reach the feature after selecting a cluster) without forcing users to enter via the Drawer's feature-fallback → cluster-list → cluster-detail dance.

**Alternative considered for the entry:** add a 配置管理 button directly to `ClusterDetailToolbar` instead of a new card. Rejected — `ClusterNodesCard` is the established convention for "feature entry from cluster detail", and adding a toolbar button breaks the visual rhythm. The card pattern keeps the detail page's stacked-card shape intact and gives future feature pages (Secrets, workloads, etc.) a consistent extension pattern.

**Alternative considered for the state service:** introduce a new `ConfigMapSelectionState` scoped service (a fork of `NodeSelectionState`) to avoid renaming a service used by the Nodes feature. Rejected — it would defeat the point of the cross-feature UX consistency goal (the user's complaint was specifically "回退到 Nodes 后切到 ConfigMaps 也应该记得同一个集群"). One service per concept; renaming is small and matches the new conceptual boundary.

**Alternative considered for K8s preview on `ClusterConfigMapsCard`:** mirror `ClusterNodesCard`'s `Cluster.Nodes.Take(5).ToList()` preview. Rejected — `ClusterDetailViewModel.Nodes` is pre-populated but there is no equivalent `ClusterDetailViewModel.ConfigMaps` field; adding one would require `ClusterService.GetClusterDetailAsync` to fire an additional `ListConfigMapForAllNamespacesAsync` K8s call on every cluster detail load, even when the user never opens ConfigMaps. The card is a pure entry affordance; users click 查看全部 to incur the K8s round-trip. Recorded here so a future `cluster-detail-configmaps-preview` change can revive the question if preview UX becomes important.

## Risks / Trade-offs

- **[R1] Create dialog cold-start UX regression** — Users no longer get a form to guide namespace/name/key constraints; they type YAML and find out about validation errors only on submit. Mitigation: the pre-filled minimal template (Decision 6) shows the expected shape at first glance, the pre-parse catches syntax YAML errors before the round-trip, and the existing 409/Conflict error mapping surfaces K8s API validation errors with the existing Chinese mapping. The Window of high-friction is one cold-start attempt — acceptable trade-off for unifying the mental model.
- **[R2] Old form-type `EditConfigMap.razor` URL 404s** — Anyone with a bookmark to `/configmaps/{...}/edit` 404s after this change. Mitigation: there are no in-app links to that route after this change (the row actions pair is 详情 / 编辑 YAML / 删除, with 编辑 YAML routing to `/yaml`). The bookmark cohort is presumably zero (the route has never served real traffic because the page is wrapped in `@*...*@`). Recorded as a known flag, not a mitigation target.
- **[R3] Namespace drift between Configmaps and Nodes** — `Components/Configmaps/**` uses `MultiClusterMgmtSys.Features.Configmaps.*` (NOT `Components.*`), which is the **opposite** of the Nodes rule (`Components.Nodes.*`). Mitigation: each new file's `@using` block is copied from the immediately-sibling file in the same folder (the commented `ConfigMaps.razor` is a valid sibling reference for the namespace lines). The tasks.md explicitly calls out re-reading a sibling before writing any new `@using`.
- **[R4] Service contract slimming** — The two form-based methods `CreateConfigMapAsync(ConfigMapCreateViewModel)` / `UpdateConfigMapAsync(ConfigMapUpdateViewModel)` are removed in this change rather than left as dead code. Trade-off: slightly wider change surface for reviewers (service contract + Razor + VMs in one change); accepted because the dead methods reference the dead VM types, so keeping one would force keeping the other. Mitigation: no migration story needed (the methods had zero callers in the repo after the Razor rewrite; no external SDK consumer exists for this app's internal service).
- **[R5] `binaryData` round-trip is symmetric only by virtue of `KubernetesYaml` having a 1:1 (de)serialize mapping for the `V1ConfigMap` model** — If the user's YAML input drops `binaryData`, it gets overwritten with `null` on save, deleting the existing `binaryData` server-side. This is correct-by-design behavior (it's what "only overwrites Data + BinaryData" means). The target deployment scenario for this system does not involve `binaryData`-bearing ConfigMaps (per user confirmation during discovery), so this behavior is a theoretical sharp edge rather than a practical footgun. The detail page shows the full YAML (including any `binaryData`), so users can verify before editing what they're keeping. No confirmation dialog or undo is added; the behavior is documented as by-design.
- **[R6] `MudTextField` Lines=30 as YAML editor is a minimal editor** — No line numbers, no syntax highlight, no diff, no schema validation. Mirrors the existing `EditConfigMapYaml.razor:60-66` shape. Mitigation: pre-parse catches structural errors; the line is byte-monospace + Lines=30 gives a reasonable-height editing surface (delete/edit pages should still fit a single screen for most ConfigMaps). A future `configmaps-yaml-editor-upgrade` change can add codemirror/monaco if the team finds the textarea too limiting.
- **[R7] No server-side paging** — `ListNamespacedConfigMap` returns all ConfigMaps in a namespace in one call. For namespaces with >500 ConfigMaps this could feel slow. Mitigation: matches Nodes' decision; the "no server paging for now" trade-off is accepted for ConfigMaps because the typical namespace has tens of ConfigMaps, not hundreds. A future `configmaps-list-paging` change can pick this up.
- **[R8] Edit-page back button goes to list, not detail** — After save we NavigateTo list; manual back press from edit also goes to list (Decision 9). User could expect "back to detail to see my saved YAML". Mitigation: the user explicitly selected this in discovery; the list is one click back to the row → 详情 link, so the round-trip cost is one extra click and feels consistent with the Nodes flow.
- **[R9] `ClusterSelectionState` is circuit-scoped, not persisted** — The service is `AddScoped`, so selected-cluster memory does not survive a hard refresh or a new Blazor circuit (e.g. reopening the browser tab). Mitigation: matches the existing `NodeSelectionState` semantics; for the live user session (Drawer switches, navigations between feature pages within the same SPA load) the context is preserved, which covers the reported UX gap. Persisting across sessions (e.g. via `ProtectedSessionStorage`) would be a follow-up change.

## Migration Plan

1. Implementation is local-only; no production data, no schema, no config — drop bins, run, smoke-test the four routes (`/configmaps`, `/configmaps/{ClusterId}`, `/configmaps/{ClusterId}/{ns}/{name}`, `/configmaps/{ClusterId}/{ns}/{name}/yaml`) plus the create dialog and delete confirmation.
2. Apply order is fixed (tasks.md enforces): create `Shared/` files bottom-up (renderers first → filter bar → toolbars → dialog) → write `ConfigMapDetail.razor` → write `EditConfigMapYaml.razor` → write `ConfigMaps.razor` → delete `EditConfigMap.razor` → trim `ConfigMapMappingExtensions.cs` → delete the three form VMs → add `CreateConfigMapFromYamlAsync` to `ConfigMapService.cs` → `dotnet build` → manual smoke against a real K8s cluster (covering: a ConfigMap containing only `data`; a ConfigMap containing `binaryData` to verify round-trip symmetry; delete confirmation + delete; create-dialog pre-parse failure on malformed YAML; create-dialog success on valid YAML).
3. Rollback = `git revert` the change commit (the commented pages remain recoverable from git history if needed).

## Open Questions

- (none remaining — all discovery questions resolved in the explore-mode session that produced this proposal.)