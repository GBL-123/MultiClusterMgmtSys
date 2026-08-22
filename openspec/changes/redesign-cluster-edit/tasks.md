## 1. Shared create/edit dialog

- [x] 1.1 Rewrite `EditClusterDialog.razor`: remove the `@* ... *@` wrapper, add `[Parameter] public int? ClusterId` (null = create), and implement `isEdit => ClusterId.HasValue`
- [x] 1.2 Port the create-mode form from `AddClusterDialog.razor` verbatim: connection-type `MudToggleGroup`, 集群名称, 所属分组, API Server field, kubeconfig paste/upload toggle (`KubeConfigInputMode` enum + `OnKubeConfigFileSelected` with 256KB cap), Token password field with visibility toggle, `SkipTlsVerify` checkbox
- [x] 1.3 Port the 集群端点 section (`ClusterEndpointEditor` + `endpoints` list) and render it ONLY in create mode (`@if (!isEdit)`)
- [x] 1.4 Implement edit-mode init: load `GetClusterForEditAsync(ClusterId)` + `GetGroupsAsync()`; populate 名称/分组/连接方式/API Server/`SkipTlsVerify` only — never prefill KubeConfig/Token; keep the loaded `ClusterEditViewModel` in a field for keep-if-blank substitution; cancel with 「未找到该集群」 snackbar if the cluster is missing
- [x] 1.5 Edit mode secret fields: empty with `Placeholder="留空保持不变"`, helper caption, and `Required="@(!isEdit || connectionType != originalConnectionType)"` so only a mode switch forces re-entry
- [x] 1.6 Edit mode hint `MudText`「连接配置变更后保存将重新检测集群状态」below the connection section
- [x] 1.7 `Submit()`: edit mode maps blank secret → existing value from the loaded edit VM (only when mode unchanged); build `ClusterCreateViewModel` (create) or `ClusterUpdateViewModel` (edit) and call `AddClusterAsync` / `UpdateClusterAsync`; keep saving spinner, Cancel disabled, success snackbar 「集群添加成功」/「集群已更新」, `Dialog.Close(DialogResult.Ok(true))`

## 2. Replace AddClusterDialog with the shared dialog

- [x] 2.1 Grep for all references to `AddClusterDialog` and confirm `Clusters.razor` is the only caller
- [x] 2.2 `Clusters.razor`: `OpenAddClusterDialog` opens `EditClusterDialog` with no `ClusterId` parameter; `OpenEditClusterDialog` passes `{ "ClusterId", id }`; dialog titles 添加集群 / 编辑集群
- [x] 2.3 Delete `Components/Clusters/Shared/AddClusterDialog.razor`

## 3. Detail page edit entry

- [x] 3.1 `ClusterDetailToolbar.razor`: add `[Parameter] public EventCallback OnEdit`; enable the 编辑 button (remove `Disabled="true"` and the 「编辑集群功能暂未实现」 `MudTooltip`); keep 刷新/删除 unchanged
- [x] 3.2 `ClusterDetail.razor`: wire `OnEdit` to a handler opening `EditClusterDialog` with `{ "ClusterId", Id }` (MaxWidth.Medium, FullWidth); on success call `LoadAsync()` (status chip and cards refresh)

## 4. Verification

- [x] 4.1 `dotnet build MultiClusterMgmtSys.slnx` compiles clean
- [ ] 4.2 Manual check (admin): create a cluster — form identical to pre-change behavior, endpoints section present
- [ ] 4.3 Manual check (admin): edit name only from the list row — dialog opens empty secrets, 「留空保持不变」 shown, save keeps connection config and status
- [ ] 4.4 Manual check (admin): edit from detail toolbar — dialog opens, save reloads detail incl. status chip
- [ ] 4.5 Manual check (admin): switch connection mode in edit — new secret required by validation; with valid input the old secret is cleared and the cluster is re-probed
- [ ] 4.6 Manual check (member): no 编辑 affordances on list or detail pages

## 5. Confirm-delete dialog style alignment

- [x] 5.1 Create shared `ConfirmDialog.razor` in `Components/Common/` (namespace already global via `_Imports.razor`): `Message`, `ConfirmText` (default 删除), optional `ConfirmIcon` parameters; 取消 = `Variant.Text`, 确认 = `Variant.Filled` + `Color.Error` + icon; closes `DialogResult.Ok(true)` on confirm
- [x] 5.2 Replace `ShowMessageBoxAsync` in `Clusters.razor` `DeleteGroup` + `DeleteCluster` with `ShowAsync<ConfirmDialog>` (title 确认删除, `MaxWidth.Small`)
- [x] 5.3 Replace `ShowMessageBoxAsync` in `ClusterDetail.razor` `DeleteCluster`
- [x] 5.4 Replace `ShowMessageBoxAsync` in `Accounts.razor` `DeleteAccount` + `BatchDelete`
- [x] 5.5 Replace `ShowMessageBoxAsync` in `ConfigMaps.razor` `DeleteConfigMap`
- [x] 5.6 Rebuild `dotnet build MultiClusterMgmtSys.slnx` compiles clean; no `ShowMessageBoxAsync` usages remain in `Components/`
