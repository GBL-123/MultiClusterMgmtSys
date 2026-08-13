## Context

`/clusters/{id}` is currently a dead route — `Components/Clusters/Pages/ClusterDetail.razor` (412 lines) is wrapped in `@* ... *@`, so Blazor never registers `@page "/clusters/{Id:int}"`. The cluster list's `NavigateToDetail` at `Clusters.razor:372` (`RedirectManager.RedirectTo($"/clusters/{id}")`) sends users into that dead route. Operators meanwhile need to record cloud-vendor-supplied virtual IPs and multiple domains per cluster — metadata the k8s API does not expose. The `ClusterInfo` entity has a single `ApiServer` column and no place for arbitrary endpoints.

The repo uses `db.Database.EnsureCreated()` at startup with no EF migrations (see `AGENTS.md` "Database quirks"), so schema evolution = delete `MultiClusterMgmtSys.db` + restart. This relaxes the migration burden but resets local data (the `admin` user is reseeded automatically by `AccountService.CreateAdminAsync()` in `Program.cs:97`; operator-entered clusters are not).

The in-flight change `refactor-clusters-group-sidebar-layout` explicitly at `proposal.md:15` reserves `EditClusterDialog.razor` for *"a separate change";* this change is not that one. The endpoints management dialog introduced here is a distinct, scoped surface; the cluster non-endpoint edit dead link stays untouched.

## Goals / Non-Goals

**Goals:**
- Bring `/clusters/{id}` back to life as a usable "archive page."
- Introduce `ClusterEndpoint` as a first-class per-cluster metadata child entity persisted in SQLite, readable even when the cluster is offline.
- Ship endpoints management UI reusable from `AddClusterDialog` (capture at creation) and from the new dedicated `ClusterEndpointsDialog` (manage existing), both embedding a single `ClusterEndpointEditor` row component.
- Lock the "at most one primary per kind" invariant and value-length invariants in `ClusterService` so they survive regardless of UI path.
- Stay consistent with repo conventions: repository exposes data, service composes logic, pages bind view models via `Mappings/*` extension methods.

**Non-Goals:**
- Resurrecting `EditClusterDialog.razor` — out of scope, see `refactor-clusters-group-sidebar-layout/proposal.md:15`. The rebuilt detail page's 编辑 button stays rendered but `Disabled` with explanatory tooltip.
- Reading endpoints from the k8s API or auto-syncing them — the user's VIPs/domains are provisioned out-of-band by a cloud vendor; this is operator-managed metadata only.
- Extending `ClusterEndpointKind` beyond `Vip`/`Domain` in this change. The enum is designed for extensibility but ships with two values.
- Building per-endpoint health probes or latency testing.
- Splitting the nodes concept out of `/nodes/{id}` — the detail page's Nodes Preview card is a compact entrypoint that still links to the dedicated nodes page.
- Perf-instrumenting the Endpoints card or virtualizing it — expected endpoint counts per cluster are single digits.

## Decisions

### Decision: One child table, not two JSON columns

Considered:
1. Add `VirtualIPsJson` + `DomainsJson` JSON-serialized `string?` columns on `ClusterInfo`.
2. Add a `ClusterEndpoint` child table (`Id`, `ClusterId`, `Kind`, `Value`, `Note`, `IsPrimary`, `SortOrder`).

**Picked 2** — the child table.

**Why:** The user said endpoints carry contextual notes ("主控", "API 入口"), need primary/secondary marking, and the data model is likely to extend (a third Kind like `Bastion` is plausible). A normalized child table makes `Note`, `IsPrimary`, `Kind`, and `SortOrder` first-class columns. Option 1 would either drop `Note`+`IsPrimary` or stuff them into JSON, removing service-layer validation surface and forcing clients to deserialize the whole blob back to mutate any row. With EF Core 10 + SQLite and `EnsureCreated()`, the child table costs nothing in added migration complexity over Option 1.

**Alternative considered and rejected:** Migrate the existing `ClusterInfo.ApiServer` into a row of `Kind = ApiServer`. Would inflate blast radius to "schema refactor of `ClusterInfo`" — saved for whatever change ends up resurrecting `EditClusterDialog.razor` or revisiting cluster identity. `ApiServer` stays its own column in this change; `ClusterEndpoint` is purely additive.

### Decision: Full-replace mutation strategy, not row diffing

`ClusterService.UpdateClusterEndpointsAsync(clusterId, List<ClusterEndpointEditItem>)`:
- Validates the incoming list (kind/value/note constraints + at-most-one-primary-per-kind).
- Eager-loads the cluster including its current `Endpoints` collection.
- `entity.Endpoints.Clear()` → EF Core marks the old rows for deletion.
- Projects each surviving `ClusterEndpointEditItem` to a new `ClusterEndpoint` and `entity.Endpoints.Add(...)`.
- `repo.UpdateAsync(entity)` issues DELETEs + INSERTs in a single SaveChanges transaction.

**Why full-replace:** Endpoint counts per cluster are tiny (single digits). A diff protocol introduces a wealth of failure modes (race against concurrent edit, ambiguous "what happens to an undeclared row id?", orphaned rows) for essentially zero perf payoff. The editor submits the entire list every save; the service is the single authority for what survives. This also plays well with Blazor interactive server semantics — no per-row optimistic concurrency token to thread through.

**Trade-off:** large endpoint sets would produce a chatty SaveChanges. Not a concern at expected scale.

### Decision: The "primary per kind" invariant is a service rule, no DB unique index

SQLite supports partial indexes, but EF Core 10's mapping needs explicit config in `OnModelCreating` and the existing convention in `Data/ApplicationDbContext.cs:23-34` is minimal config. The invariant is small enough to enforce in `ClusterService` before SaveChanges; violations surface as `ArgumentException` with a clear message. UI also de-selects other primaries within a kind via an `EventCallback` when a user marks a row primary, catching the case before round-trip.

**Picked:** service-layer `ArgumentException` + UI radio-button grouping per `Kind`. No DB-level unique index.

### Decision: `ClusterEndpointEditor.razor` is a shared row editor reused by Add and Manage

Takes `[Parameter] List<ClusterEndpointEditItem> Items` and mutates that list in place via `@bind`. Each row has: `Kind` (`MudSelect`), `Value` (`MudTextField`, required), `Note` (`MudTextField`, optional), `IsPrimary` (`MudCheckBox` whose value-changed handler deselects other rows of the same `Kind`), and a row delete button that removes the row from `Items`. Plus a "[+ 添加端点]" button appending a new row with `Kind = Vip`, `IsPrimary = false`.

`AddClusterDialog.razor` embeds the editor directly with `Items = new()` initial state. `ClusterEndpointsDialog.razor` wraps the editor in a `MudDialog`, seeds `Items` on init from `ClusterService.GetClusterDetailAsync(ClusterId)` (no separate "list endpoints" service call needed), and on OK calls `UpdateClusterEndpointsAsync(ClusterId, Items)`.

**Why one component:** consistent UX between create and manage, single point of truth for primary-per-kind/length validation UI.

### Decision: Detail page layout uses `MudCard` cards, not the list page's workbench `MudPaper`

The rebuilt `/clusters` list page is `MudStack Row="true"` with a 240px sidebar and a right `MudStack` of `MudPaper pa-4 + ClusterFilterBar + ClusterTable`. That workbench feel is right for filtering + dense table interactions.

The detail page is one-object deep: a single cluster's fact sheets. We pick `MudCard Elevation="1"` cards stacked vertically below a `MudPaper pa-4` toolbar. Cards read as discrete records of a single resource; the list page aesthetic would suggest multiple independent work surfaces.

```
Toolbar (MudPaper pa-4):  ← back  |  name (h4)  status chip  |  Admin: [刷新][编辑 disabled+tooltip][删除]
Card 1 — 概览            (MudCard Elevation="1")   ← 合并了原"连接信息"：字段区全员可见，密文揭示区 Admin-only
Card 2 — 集群端点        (MudCard Elevation="1")   ← NEW
Card 3 — 节点列表预览    (MudCard Elevation="1")
```

### Decision: Overview and Connection Info merge into one card; only the secret reveal is Admin-gated

Originally the design kept four cards with the whole Connection Info card behind `<AuthorizeView Roles="Admin">`. In review the two cards proved redundant — both surfaced API Server, and 连接方式 (Kubeconfig/Token) is not itself sensitive. The merged Overview card shows all identity + connection metadata fields to every role; only the secret value reveal (显示密文 toggle + the masked KubeConfig/Token `MudTextField`) stays wrapped in `<AuthorizeView Roles="Admin">`, placed in the card's header actions and a section below the field grid.

### Decision: Clipboard via `IJSRuntime` + `navigator.clipboard.writeText`

MudBlazor 9 doesn't ship a first-class clipboard service in this stack. We use `IJSRuntime` to call `navigator.clipboard.writeText(value)` from a small JS interop. Failure path surfaces a snackbar error.

**Trade-off:** `navigator.clipboard` requires a secure context (HTTPS or localhost). Dev `http://localhost:5021` is a secure context by exception; production `https://localhost:7081` and Docker 8080/8081 are also fine when accessed over localhost. Non-localhost plain HTTP deployments would fail silently — the snackbar surfaces the error to make this obvious. A future change could fall back to `document.execCommand('copy')` against a hidden textarea; out of scope here.

### Decision: Editor exposes `SortOrder` as a manual numeric input, no drag-and-drop in v1

Drag-and-drop adds a significant JS layer for marginal value at single-digit endpoint counts. Each editor row carries a small numeric `SortOrder` input; the service sorts by `SortOrder` ascending with `IsPrimary == true` rows first as a tiebreaker within each `Kind` group.operators renumber manually.

## Risks / Trade-offs

- **Schema reset wipes local cluster data.** Operators with local cluster rows lose them when they delete `.db` to rebuild schema. **Mitigation:** documents in proposal + tasks; `admin / Changeme_123` reseeds automatically; operator clusters are input data and not auto-restored. Call this out in the eventual commit message.
- **Members can read VIPs/domains but cannot edit.** The spec also accepts Members reading. Operators in some environments may want endpoint reads gated Admin-only. **Mitigation:** easily changed later — wrap the Endpoints card in `<AuthorizeView Roles="Admin">` in the detail page. Not doing it preemptively.
- **`navigator.clipboard.writeText` requires secure context.** Plain HTTP non-localhost deployments would fail silently. **Mitigation:** the snackbar reports failure verbosely. Out of scope here.
- **No drag-and-drop reorder.** Operators wanting a specific order use the `SortOrder` numeric input. **Mitigation:** documented; enhancement is a plausible future change.
- **`EditClusterDialog` dead link persists.** An operator expecting "编辑" to work will be disappointed. **Mitigation:** tooltip "编辑集群功能暂未实现"; field editing explicitly reserved for a separate change.
- **`GetByIdAsync` adds `.Include(c => c.Endpoints)`.** Bloats each detail fetch by one round-trip. Lazy load would N+1; explicit Include is the convention across this repo. **Mitigation:** `GetPagedAsync` deliberately omits the Include (list view doesn't render endpoints).
- **Race condition on full-replace endpoints save.** Two admins editing the same cluster's endpoints simultaneously — last writer wins, no merge. **Mitigation:** acceptable at this scale; the spec calls this out by saying "the service is the single authority for what survives." Optimistic concurrency tokens can be added later if needed.

## Migration Plan

1. Stop the running app (Ctrl+C on the `dotnet run` process).
2. Delete `MultiClusterMgmtSys/MultiClusterMgmtSys.db` plus any `*-wal` / `*-shm` and the stray `clusters.db` if present per `AGENTS.md`.
3. `dotnet build MultiClusterMgmtSys.slnx` — expected clean.
4. `dotnet run --project MultiClusterMgmtSys` — first startup runs `EnsureCreated()` and rebuilds the schema including `ClusterEndpoints`; `AccountService.CreateAdminAsync()` reseeds `admin / Changeme_123`.
5. Log in as `admin / Changeme_123`, navigate to `/clusters`, click a cluster row — `/clusters/{id}` renders the rebuilt detail page.
6. Open the Endpoints card's "管理" dialog, add 2 VIPs and 2 domains, mark one of each as primary, save — rows appear on the detail page sorted per the spec.
7. No rollback path beyond restoring the deleted `.db` from a backup; this matches the repo-wide convention noted in `AGENTS.md`.

## Open Questions

- **Open:** Should a future change collapse the existing `ApiServer` column into the `ClusterEndpoint` table (as `Kind = ApiServer`)? Deferred — covered in the rejected alternative above.
- **Open:** Whether the "Nodes Preview" card shows top-5 or all-loaded rows is a UX nitpick. **Current pick:** top 5 sorted by name, with "查看全部 →" linking to `/nodes/{id}`; revisit if feedback requests differently.
- **Open:** Once `EditClusterDialog` is resurrected by a future change, should endpoints editing stay in its own dialog (as introduced here) or fold into the cluster edit form? **Deferred:** out of scope; revisit when the cluster-edit change lands.