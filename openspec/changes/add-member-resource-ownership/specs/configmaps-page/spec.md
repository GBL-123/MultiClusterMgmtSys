# Spec Delta

## MODIFIED Requirements
### Requirement: Admin-only "新建 ConfigMap" button on the list page

The 新建 ConfigMap button in the list page's title row SHALL be rendered for all authenticated users (no `AuthorizeView Roles="Admin"` wrapper) and SHALL stay Disabled when `cluster is null` or `!cluster.IsReachable`. Clicking the button SHALL open `CreateConfigMapDialog` via `DialogService.ShowAsync<CreateConfigMapDialog>` with `ClusterId` as a dialog parameter. Whether the create request ultimately succeeds is governed by the `k8s-resource-ownership` contract: server-side ownership stamping and the protected-namespace blacklist apply to everyone, so a silently misdirected member create (`kube-*`) fails with a Chinese validation error inside the dialog.

#### Scenario: Admin user on a reachable cluster

- **WHEN** an Admin user opens `/configmaps/{ClusterId}` for a reachable cluster
- **THEN** the 新建 ConfigMap button is visible and enabled
- **AND** clicking it opens the create dialog scoped to `ClusterId`

#### Scenario: Member user (non-Admin) on any cluster

- **WHEN** a Member user opens the list page
- **THEN** the 新建 ConfigMap button is rendered and enabled on a reachable cluster
- **AND** the created ConfigMap is stamped with the Member's ownership metadata per the `k8s-resource-ownership` contract

#### Scenario: Admin user on an unreachable cluster

- **WHEN** an Admin user opens the list page for a cluster whose `IsReachable` is false
- **THEN** the 新建 ConfigMap button is visible but Disabled

#### Scenario: Member create into a protected namespace fails at submit

- **WHEN** a Member submits the create dialog with `metadata.namespace: kube-system`
- **THEN** the dialog shows a Chinese validation error and no K8s API call is made

### Requirement: Row actions on the list table

Each table row SHALL expose three action icon buttons: 详情 (always visible to all authenticated users, navigates to `/configmaps/{ClusterId}/{Namespace}/{Name}`), 编辑 YAML and 删除 (both rendered only when the service-computed `CanOperate` projection for the row is true, per the `k8s-resource-ownership` contract; no `AuthorizeView Roles="Admin"` wrappers). 删除 opens a confirm dialog and then calls `ConfigMapService.DeleteConfigMapAsync`.

#### Scenario: Non-Admin user sees row actions

- **WHEN** a Member user views the table
- **THEN** the 详情 icon button is rendered on every row
- **AND** the 编辑 YAML and 删除 icon buttons are rendered only on rows where `CanOperate` is true (rows the Member created, including Helm-interlocked releases they installed)

#### Scenario: Admin user deletes a ConfigMap

- **WHEN** an Admin user clicks the 删除 icon button on a row
- **THEN** the system opens a confirmation dialog asking 确认删除 ConfigMap「{name}」？此操作不可撤销
- **AND** if the user confirms, the system calls `ConfigMapService.DeleteConfigMapAsync(ClusterId, name, ns)`
- **AND** on success the system shows a 删除成功 snackbar and refreshes the table
- **AND** no undo affordance is provided

#### Scenario: Delete fails due to 404

- **WHEN** the delete API returns a 404 / Not Found error
- **THEN** the system shows a ConfigMap 不存在或已被删除 warning snackbar (not an error)

#### Scenario: Owner sees mutating row actions

- **WHEN** a Member views a ConfigMap row they created (`CanOperate` is true)
- **THEN** the 编辑 YAML and 删除 icon buttons are rendered on that row

### Requirement: Admin-only edit-YAML navigation from the detail page

The 编辑 YAML button in `ConfigMapDetailToolbar` SHALL be rendered only when the service-computed `CanOperate` projection for the loaded detail is true (per the `k8s-resource-ownership` contract) and SHALL navigate to `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml` on click. Users without permissions see no edit affordance.

#### Scenario: Admin user opens edit from detail

- **WHEN** an Admin user on the detail page clicks 编辑 YAML
- **THEN** the system navigates to `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml`

#### Scenario: Non-Admin user on the detail page

- **WHEN** a user without ownership views the detail page
- **THEN** no 编辑 YAML button is rendered in the toolbar

#### Scenario: Non-Admin owner opens edit from detail

- **WHEN** a Member who owns the ConfigMap clicks 编辑 YAML
- **THEN** the system navigates to `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml`

### Requirement: YAML editor page for an existing ConfigMap

The system SHALL render a YAML editor page at `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml` as a toolbar (`EditConfigMapYamlToolbar`: 返回列表 + 编辑 YAML: `{Name}` h4 + 保存 `MudButton`) followed by a single `ConfigMapYamlEditCard` containing an editable `MudTextField` Lines=30 monospace `@bind-Value=yamlContent`. The page SHALL load `ConfigMapService.GetConfigMapAsync` to seed `yamlContent` from `ConfigMapDetailViewModel.Yaml`. The page route SHALL be accessible to any authenticated user (`@attribute [Authorize]` only); the actual save is only executable for Admins or owners because `ConfigMapService.UpdateConfigMapFromYamlAsync` enforces the `k8s-resource-ownership` contract server-side, and a user who cannot operate the resource SHALL see a Chinese permission error instead of the editor's save result.

#### Scenario: Successful editor load

- **WHEN** an Admin or owner navigates to the YAML editor for an existing ConfigMap
- **THEN** the page renders the toolbar with the resource name and an editable monospace YAML field
- **AND** the YAML field is pre-filled with the resource's current full YAML

#### Scenario: Resource disappears during edit

- **WHEN** `GetConfigMapAsync` returns null on editor load
- **THEN** the page renders a ConfigMap 不存在或已被删除 empty state with a 返回列表 button
- **AND** no editable YAML field is rendered

#### Scenario: Non-owner saving is rejected

- **WHEN** a Member submits the editor for a ConfigMap they do not own
- **THEN** the service throws a Chinese permission error, no K8s API call is made, and the page keeps the editor state
