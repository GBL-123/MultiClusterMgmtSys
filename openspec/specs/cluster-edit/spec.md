# cluster-edit

## Purpose

Define the contract for cluster create/edit in the `/clusters` UI: a single shared `EditClusterDialog` serves both create and edit modes with secret-safe editing semantics (stored secrets are never prefilled and blank means keep), Admin-only edit entry points on the list and detail pages, save feedback with connection re-probe on connection-config changes, and project-styled confirm-delete dialogs replacing MudBlazor message boxes.

## Requirements

### Requirement: Shared create/edit dialog component

The cluster form SHALL be a single dialog component (`EditClusterDialog`) that serves both create and edit modes via an optional `ClusterId` parameter: `null` opens create mode, a value opens edit mode for that cluster. The old `AddClusterDialog` SHALL be removed and all its functionality (connection-type toggle, kubeconfig paste/upload input modes, Token field with visibility toggle, `SkipTlsVerify` checkbox, embedded `ClusterEndpointEditor` section for endpoints capture at creation) SHALL be preserved in the shared dialog's create mode. The create mode form SHALL be functionally identical to the current `AddClusterDialog` so creation behavior does not regress.

#### Scenario: Dialog opens in create mode
- **WHEN** an Admin clicks 添加集群 on the cluster list page
- **THEN** the shared dialog opens in create mode with an empty form: connection-type toggle (Kubeconfig/Token), 集群名称 (required), 所属分组 select (default 无分组), API Server field, the mode-specific secret input, and the 集群端点 section with the `ClusterEndpointEditor` (initialized empty)

#### Scenario: Dialog opens in edit mode
- **WHEN** an Admin triggers edit for a cluster (list row 编辑 or detail toolbar 编辑)
- **THEN** the shared dialog opens in edit mode titled 编辑集群, preloading the cluster's 名称, 分组, 连接方式, API Server and SkipTlsVerify from `GetClusterForEditAsync`, and the 集群端点 section is NOT rendered (endpoints are managed on the detail page)

#### Scenario: Legacy add dialog removed
- **WHEN** the project is built
- **THEN** `AddClusterDialog.razor` no longer exists and no file references it

### Requirement: Secret fields are never prefilled in edit mode

In edit mode the KubeConfig/Token secret fields SHALL open empty (never showing the stored secret value) and SHALL carry placeholder 「留空保持不变」 plus helper text explaining the semantics. On save, a blank secret field with an unchanged connection type SHALL keep the existing stored secret; a non-blank value SHALL replace it. If the operator switches the connection type in edit mode, the new mode's secret field SHALL become required so a mode switch always supplies fresh credentials, and saving SHALL null the other mode's stored secret (existing `UpdateClusterAsync` behavior).

#### Scenario: Edit name only, secrets preserved
- **WHEN** an Admin opens edit for a Token-mode cluster and changes only 集群名称, leaving Token blank
- **THEN** the stored Token is unchanged after save and the cluster keeps its connection config

#### Scenario: Replace secret in same mode
- **WHEN** an Admin opens edit, keeps the same connection type, and enters a new Token/KubeConfig value
- **THEN** the new value replaces the stored secret and the cluster is re-probed because the connection config changed

#### Scenario: Switch connection type requires new credentials
- **WHEN** an Admin switches the connection type from KubeConfig to Token in edit mode and leaves Token blank
- **THEN** validation blocks the save (Token required in Token mode) and the stored KubeConfig remains untouched

#### Scenario: Mode switch with new credentials clears the old secret
- **WHEN** an Admin switches KubeConfig → Token and enters a valid Token + API Server
- **THEN** the cluster saves with the new Token and the stored KubeConfig is nulled, and the cluster is re-probed

### Requirement: Edit entry points on list and detail pages

Both Admin-only edit entry points SHALL open the same shared dialog and reload their data on success. The cluster list row 编辑 action SHALL open the dialog in edit mode for that cluster and, on save, refresh the table, the version dropdown and the group sidebar. The detail page toolbar SHALL render an enabled 编辑 button (no Disabled attribute, no 「编辑集群功能暂未实现」 tooltip) that opens the dialog in edit mode and, on save, reloads the detail view so the status chip and cards reflect the update.

#### Scenario: List page edit entry
- **WHEN** an Admin clicks 编辑 on a cluster row in `/clusters`
- **THEN** the shared dialog opens in edit mode for that cluster, and after a successful save the table rows, available versions and group sidebar are reloaded

#### Scenario: Detail page edit entry
- **WHEN** an Admin clicks 编辑 in the detail page toolbar at `/clusters/{id}`
- **THEN** the shared dialog opens in edit mode for that cluster, and after a successful save the detail view reloads

#### Scenario: Member has no edit affordances
- **WHEN** a Member views the cluster list or detail page
- **THEN** no 编辑 button is rendered anywhere and the dialog cannot be opened

### Requirement: Save feedback and connection re-probe

The dialog SHALL disable 取消 and show a saving spinner while submitting, and SHALL close with success after save. The edit form SHALL display a hint that saving after a connection-config change re-detects the cluster status. When connection fields change on save, `UpdateClusterAsync` SHALL re-probe the cluster; the caller's reload SHALL then surface the new status.

#### Scenario: Save shows progress and success
- **WHEN** an Admin clicks 保存 with valid input
- **THEN** the submit button shows a spinner and 取消 is disabled until the save completes, then the dialog closes with a 「集群已更新」 success snackbar

#### Scenario: Connection change triggers re-probe
- **WHEN** an Admin edits an offline cluster's Token (connection config changed) and saves
- **THEN** the cluster is re-probed during save and the reloaded list/detail shows the updated status

### Requirement: Confirm-delete dialogs match the project dialog style

Destructive-action confirmations (cluster/group/account/ConfigMap delete) SHALL use a shared `ConfirmDialog` component styled like the project's own dialogs instead of the built-in MudBlazor message box. The dialog SHALL show the title 确认删除 and a message naming the target, with 取消 as `Variant.Text` and the destructive button as `Variant.Filled` + `Color.Error` with a delete icon. No `DialogService.ShowMessageBoxAsync` call SHALL remain in `Components/`.

#### Scenario: Delete confirmation renders project-styled buttons
- **WHEN** an Admin clicks 删除 on a cluster row and the confirm dialog appears
- **THEN** the dialog shows 确认删除 with the message 「确认删除集群「x」？此操作不可撤销。」, a 取消 text button, and a red filled 删除 button with a delete icon

#### Scenario: Cancel and confirm close the dialog
- **WHEN** the Admin clicks 取消
- **THEN** the dialog closes and no deletion happens
- **WHEN** the Admin clicks the red 删除 button
- **THEN** the dialog closes successfully and the caller performs the deletion
