## Why

Cluster editing is a half-built dead feature: `EditClusterDialog.razor` is entirely wrapped in `@* ... *@` (the list page's row "编辑" action therefore opens an empty dialog shell), the detail page's toolbar renders "编辑" permanently `Disabled` with tooltip 「编辑集群功能暂未实现」, and the commented-out form is a stale copy of `AddClusterDialog` that predates the current form design (no kubeconfig paste/upload toggle, no endpoints section, and it would prefill the real KubeConfig/Token into the textarea on open). Both `refactor-clusters-group-sidebar-layout` and `redesign-cluster-detail-with-endpoints` explicitly reserved resurrecting cluster editing for a separate change — this is that change.

## What Changes

- **Resurrect `EditClusterDialog.razor` as the single shared create/edit form** (`[Parameter] int? ClusterId`, null = create), replacing `AddClusterDialog.razor` (deleted). This matches the repo's established pattern (`AccountEditDialog`, `EditGroupDialog` use one component for create + edit). The form body follows the current `AddClusterDialog` design: connection-type `MudToggleGroup`, kubeconfig paste/upload toggle (`KubeConfigInputMode`), Token password field with visibility toggle, `SkipTlsVerify` checkbox.
- **Create mode**: identical behavior to today's `AddClusterDialog`, including the embedded `ClusterEndpointEditor` section (endpoints captured at creation).
- **Edit mode**: the KubeConfig/Token fields are **never prefilled** with stored secrets; empty secret field means "留空保持不变" (keep existing). This replaces the old prefill behavior. The endpoints section is **hidden in edit mode** — after creation, endpoints are maintained exclusively through the detail page's `ClusterEndpointsDialog` (per `redesign-cluster-detail-with-endpoints` design decision; that surface stays untouched).
- **Enable the detail page toolbar "编辑" button**: remove `Disabled="true"` and the 「编辑集群功能暂未实现」 tooltip; wire a new `OnEdit` callback from `ClusterDetailToolbar.razor` → `ClusterDetail.razor` opens the shared dialog and reloads the detail view on success.
- **List page `Clusters.razor`**: `OpenAddClusterDialog` and `OpenEditClusterDialog` both target the shared dialog (create vs edit via `ClusterId`); after edit-save the table, versions and sidebar reload as today.
- **Connection-type switch semantics in edit mode**: switching KubeConfig ↔ Token makes the new mode's secret field required (a "keep existing" blank is only valid when the mode is unchanged); the old mode's secret is nulled by the existing `UpdateClusterAsync` logic.
- **Connection-config change feedback**: `UpdateClusterAsync` already re-probes the cluster when connection fields change; the edit dialog shows a hint (「连接配置变更后保存将重新检测集群状态」) and, after save, the caller reloads so the status chip reflects the new probe result.
- **No database changes**: schema untouched, no `.db` deletion required.
- **Replace every `DialogService.ShowMessageBoxAsync` delete confirmation with a shared `ConfirmDialog` component** matching the project's dialog button language (取消 = `Variant.Text`, 确认 = `Variant.Filled` + `Color.Error` + delete icon). The built-in MudBlazor message box renders its confirm button in theme Primary blue, contradicting the red destructive-action convention used by every 删除 button in the app. Affected call sites: cluster delete (list + detail), group delete, account delete, batch account delete, ConfigMap delete.

## Capabilities

### New Capabilities
- `cluster-edit`: The shared cluster create/edit dialog (identity fields + connection configuration with "留空保持不变" secret semantics), its two Admin entry points (list row action, detail toolbar button), and the resulting reload/probe behavior after save.

### Modified Capabilities
<!-- None. `cluster-query-layering` is untouched. The in-flight `cluster-detail` delta spec (from `redesign-cluster-detail-with-endpoints`) described the toolbar's edit button as Disabled+tooltip; that delta's wording is superseded by the `cluster-edit` spec in this change, but `cluster-detail` is not yet a main spec and is not re-deltated here. -->

## Impact

- **Code:**
  - `Components/Clusters/Shared/EditClusterDialog.razor` — rewrite from commented-out stub to shared create/edit form (reuses the `KubeConfigInputMode` enum and file-upload logic currently inside `AddClusterDialog`).
  - `Components/Clusters/Shared/AddClusterDialog.razor` — **deleted** (superseded by the shared dialog).
  - `Components/Clusters/Pages/Clusters.razor` — `OpenAddClusterDialog`/`OpenEditClusterDialog` retarget to the shared dialog; refresh logic unchanged.
  - `Components/Clusters/Shared/ClusterDetailToolbar.razor` — add `OnEdit` `EventCallback`, remove the Disabled + tooltip on the 编辑 button.
  - `Components/Clusters/Pages/ClusterDetail.razor` — handle `OnEdit`, open dialog, reload `LoadAsync()` on success.
  - `Components/Clusters/Services/ClusterService.cs` — `GetClusterForEditAsync` / `UpdateClusterAsync` already exist and need **no** functional change; keep-if-blank substitution happens in the dialog before building the update VM. Audit logging unchanged (the service already logs `AuditAction.Update`).
  - `Components/Clusters/ViewModels/ClusterEditViewModel.cs`, `ClusterUpdateViewModel.cs`, `ClusterCreateViewModel.cs` — unchanged shapes (create keeps `Endpoints`).
  - `Components/Common/ConfirmDialog.razor` (new) — shared confirm dialog; replaces `ShowMessageBoxAsync` call sites in `Clusters.razor`, `ClusterDetail.razor`, `Accounts.razor`, `ConfigMaps.razor`.
- **Database:** None — no schema change, no migration, no `.db` reset.
- **URL contract:** unchanged.
- **Permissions:** editing remains Admin-only via existing `<AuthorizeView Roles="Admin">` gates on both entry points; the dialog itself requires no additional authorization logic.
