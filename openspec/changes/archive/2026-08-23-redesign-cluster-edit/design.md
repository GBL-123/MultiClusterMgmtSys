## Context

`EditClusterDialog.razor` is fully commented out (`@* ... *@`) — a deliberate deferral: `refactor-clusters-group-sidebar-layout/proposal.md:15` reserved cluster editing for a separate change, and `redesign-cluster-detail-with-endpoints/design.md` ("EditClusterDialog dead link persists") kept the detail page's 编辑 button `Disabled` with tooltip 「编辑集群功能暂未实现」. The dead code has two user-visible symptoms: the list page's row 编辑 action opens an empty dialog shell, and the detail toolbar offers no edit at all.

Meanwhile the form design moved on: `AddClusterDialog.razor` gained a kubeconfig paste/upload toggle, a Token password field with visibility toggle, and an embedded `ClusterEndpointEditor` (endpoints captured at creation). The commented-out edit dialog is a stale copy of the old form — it would prefill the stored KubeConfig/Token into the textarea, has no upload toggle, and no endpoints section. The repo's edit-dialog convention (`AccountEditDialog`, `EditGroupDialog`) is a single component handling both create and edit via an `isEdit`-style flag.

The service layer is complete and untouched: `GetClusterForEditAsync` loads the full entity (incl. secrets) into `ClusterEditViewModel`; `UpdateClusterAsync` compares connection fields, re-probes via `ProbeAsync` when the config changed, persists, and writes an `AuditAction.Update` audit entry. `UpdateClusterEndpointsAsync` manages endpoints separately and is only invoked from the detail page's `ClusterEndpointsDialog`.

## Goals / Non-Goals

**Goals:**
- One shared create/edit dialog component that matches the current form design (paste/upload toggle, Token visibility toggle, connection-type toggle) — no more stale copies.
- Edit mode never exposes stored secrets by default: "留空保持不变" (blank = keep existing), with mode-switch forcing re-entry of the new secret.
- Both Admin entry points work and open the same dialog: list row 编辑 action, detail toolbar 编辑 button (currently Disabled).
- After a connection-config change, the existing re-probe runs and the caller reloads so status reflects the new probe.
- Zero service-layer changes: keep-if-blank substitution happens in the dialog; `UpdateClusterAsync` semantics stay "write what you get".

**Non-Goals:**
- Editing endpoints from the cluster edit dialog — post-creation endpoint maintenance stays exclusively on the detail page's `ClusterEndpointsDialog`.
- Live connection probing inside the dialog (e.g., showing new status before closing).
- Secret storage/encryption changes (plaintext SQLite columns are pre-existing; out of scope).
- Batch editing (existing batch mode only moves clusters to groups).
- Schema changes — none needed; no `.db` reset required.

## Decisions

### Decision: One shared dialog component — `EditClusterDialog.razor` resurrected as the single form, `AddClusterDialog.razor` deleted

`EditClusterDialog.razor` becomes the shared create/edit form with `[Parameter] public int? ClusterId` (null = create). `AddClusterDialog.razor` is deleted; its `KubeConfigInputMode` enum and file-upload logic move into the shared dialog. Callers: `Clusters.razor` (add → no `ClusterId` param; row edit → `{ "ClusterId", id }`) and `ClusterDetail.razor` (toolbar edit → `{ "ClusterId", Id }`).

**Why:** The root cause of the current breakage is form drift between two copies. The repo already settled on the single-dialog pattern twice (`AccountEditDialog` create+edit, `EditGroupDialog` add+rename). Naming it "Edit" while also serving create matches `AccountEditDialog` exactly.

**Alternatives considered:**
1. Keep two separate dialogs, modernize only `EditClusterDialog` — rejected: this is exactly the drift that produced the stale commented copy.
2. `EditClusterDialog` wraps `AddClusterDialog` as a child — rejected: adds component indirection and awkward parameter plumbing for no benefit.
3. Rename to a neutral `ClusterFormDialog` — acceptable, but renaming call sites adds churn without matching the repo's existing edit-dialog naming.

### Decision: Secrets are never prefilled; "留空保持不变" with mode-switch forcing re-entry

Edit mode leaves both secret fields empty with `Placeholder="留空保持不变"` + helper text. On submit the dialog maps blanks: `secret = string.IsNullOrWhiteSpace(secret) ? editVm.Secret : secret`. The substitution applies **only when the connection mode is unchanged** — the `Required` flag on the secret field becomes `@(!isEdit || connectionType != originalConnectionType)`, so switching KubeConfig ↔ Token demands a real value for the new mode. `UpdateClusterAsync` already nulls the now-unused other-mode secret (`entity.KubeConfig = vm.ConnectionType == KubeConfig ? vm.KubeConfig : null`), so no service change is needed there.

**Why dialog-side substitution, not service-side:** `UpdateClusterViewModel` keeps "write what you get" semantics; the existing `configChanged` comparison and re-probe logic in `UpdateClusterAsync` work unchanged. A service-side `null = keep` contract would break create/update symmetry and force rethinking what "clear a secret" means (there is no empty-secret concept — a cluster must always have credentials).

**Alternative considered:** Service-side `null` = keep existing. Rejected as above. The `ClusterOverviewCard` 显示密文 toggle keeps using `GetClusterForEditAsync` read-only — unchanged by this design.

### Decision: ApiServer stays "form-accurate" — blank = clear

Only the two secrets get keep-if-blank. `ApiServer` in edit mode: blank submits `null`. Rationale: in Token mode it is required (cannot be blank); in KubeConfig mode it is optional and re-parsed from the config at probe time, so clearing it is harmless and predictable. Documented in the dialog helper text (「留空则从 kubeconfig 中解析」) so the semantics are visible.

### Decision: Endpoints section renders only in create mode

`@if (!isEdit) { <MudDivider/> ... <ClusterEndpointEditor Items="@endpoints" /> }`. Create mode is byte-identical to today's `AddClusterDialog`. Post-creation, `ClusterEndpointsDialog` on the detail page is the single maintenance surface.

**Why:** `redesign-cluster-detail-with-endpoints` deliberately built the dedicated endpoints management dialog and asked (open question in its design.md) whether the edit form should also carry endpoints. Including the editor in the edit dialog would create two full-replace editors for the same data with last-writer-wins races, and would make the detail-page dialog redundant. Keeping one surface per concern matches the current design's intent.

### Decision: Detail toolbar 编辑 enabled and wired

`ClusterDetailToolbar.razor` gains `[Parameter] public EventCallback OnEdit { get; set; }`; the button loses `Disabled="true"` and the tooltip. `ClusterDetail.razor` implements `OpenEditDialog()` → `DialogService.ShowAsync<EditClusterDialog>("编辑集群", { "ClusterId", Id }, ...)`; on OK → `LoadAsync()` (reloads detail VM incl. status chip) — snackbar 「集群已更新」 comes from the dialog, matching the list flow.

**Why:** The `cluster-detail` delta spec (in-flight change) worded the toolbar button as Disabled-with-tooltip; that wording is superseded by this change. No layout change — the button simply becomes functional.

### Decision: Static hint for re-probe, no in-dialog probe

Edit mode shows a `MudText Typo="Typo.caption"` hint: 「连接配置变更后保存将重新检测集群状态」. The service already probes inside `UpdateClusterAsync` when `configChanged`; both callers reload afterwards, so the status chip reflects the result. No probe UI inside the dialog.

**Why:** Probing inside the dialog would delay the save by a network round-trip with a spinner and add failure-mode UI for marginal information gain. The snackbar + reload pattern is the established UX across the app.

### Decision: Dialog interaction details carry over unchanged

- `MudForm` + `ValidateAsync` gating, `saving` spinner state, Cancel disabled while saving, `Dialog.Close(DialogResult.Ok(true))` on success.
- Edit mode preloads `GetClusterForEditAsync(ClusterId)` + `GetGroupsAsync()` on init (as the commented code did), but copies only non-secret fields into the form; the loaded `ClusterEditViewModel` is kept in a field for keep-if-blank substitution at submit.
- Connection-type `MudToggleGroup` and the mode-specific sections mirror `AddClusterDialog` exactly (including `SkipTlsVerify` checkbox in Token mode, and the upload file-size cap of 256KB).

## Risks / Trade-offs

- **Blind saves on existing clusters**: an operator who edits Name only and hits save keeps the old connection config — safe by design (blank = keep), but the 「留空保持不变」 placeholder + helper text are the only guard against confusion. **Mitigation:** both placeholder and caption on every secret field in edit mode; spec scenarios pin the behavior.
- **Accidental secret overwrite when switching modes**: switching KubeConfig → Token and saving nulls the stored KubeConfig. **Mitigation:** the new mode's field is `Required` (validation blocks the save), so the old secret can only be dropped intentionally — and the re-probe afterwards will surface a broken connection immediately.
- **`AddClusterDialog` deletion breaks references if any exist outside `Clusters.razor`.** **Mitigation:** grep before deletion (implementation task); known reference is only `Clusters.razor:266`.
- **`cluster-detail` delta wording drift**: the in-flight `redesign-cluster-detail-with-endpoints` delta says the toolbar edit button is Disabled. **Mitigation:** that change is not archived yet; when both land, the `cluster-edit` spec supersedes the Disabled-button wording (noted in proposal's Modified Capabilities comment).
- **Two callers, one dialog — parameter contract**: create must NOT pass `ClusterId`; edit must. Passing `0`/wrong type would create a phantom cluster or fail load. **Mitigation:** dialog guards `ClusterId` (edit path shows 「未找到该集群」 + `Dialog.Cancel()` when load fails — same as the commented original).

## Migration Plan

1. Implement per tasks; `dotnet build MultiClusterMgmtSys.slnx` must stay clean.
2. No DB change — existing `MultiClusterMgmtSys.db` stays.
3. Manual verification: create a cluster (form identical to before, incl. endpoints section); edit it (name only — secrets kept, status chip preserved); switch connection mode in edit (new secret required); open edit from detail toolbar; verify list table + versions reload after edit; verify Member role sees no edit affordances.
4. Rollback: revert the change (pure code change, no data impact).

## Open Questions

- **Open:** Should the edit dialog eventually also manage endpoints (folding `ClusterEndpointsDialog` in)? Deferred — v1 keeps the detail page as the sole post-creation endpoint surface, per this design.
- **Open:** Should ApiServer also get keep-if-blank semantics? Deferred — current pick is form-accurate (blank = clear), because blank is only reachable in KubeConfig mode where the probe re-parses the value anyway.
