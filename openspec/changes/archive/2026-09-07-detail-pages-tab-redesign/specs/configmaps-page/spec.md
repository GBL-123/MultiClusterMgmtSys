# configmaps-page delta

## REMOVED Requirements

### Requirement: ConfigMap detail page as read-only YAML

**Reason**: 详情页布局统一改为「工具栏 + MudTabs 分页」,并补上 Data 键值只读视图;「YAML 是唯一视图」与 "SHALL NOT render MudTabs" 的契约被「YAML | 键值双 tab」取代。

**Migration**: 由下方 ADDED 的 "ConfigMap detail page as read-only tabs" requirement 取代;YAML 视图卡片的实现形态(原生 `<textarea class="yaml-textarea">`,非 MudTextField)与只读性质保持不变。

## ADDED Requirements

### Requirement: ConfigMap detail page as read-only tabs

The system SHALL render a ConfigMap detail page at `/configmaps/{ClusterId}/{Namespace}/{Name}` as a toolbar (`ConfigMapDetailToolbar`: 返回列表 + `{Name}` h4 + "Data 键数: {n}" `MudChip` + 编辑 YAML button admin-gated + 刷新 button) followed by a `MudTabs` area with exactly two tabs in this order: YAML → 键值. The YAML tab contains the existing read-only `ConfigMapYamlViewCard` (plain `<textarea class="yaml-textarea">` in a `MudCard Class="pa-4 yaml-card"`, unchanged). The 键值 tab contains a read-only table listing every entry of `ConfigMapDetailViewModel.Data` with 键 and 值 columns in monospace; long values follow the `detail-page-tabs` truncation + click-to-expand contract. An empty `Data` dictionary SHALL show the `.empty-state` placeholder in the 键值 tab. The page SHALL NOT render any per-data-key editing surface — both tabs are read-only; editing remains on the separate Admin-only YAML editor route.

#### Scenario: Successful detail load

- **WHEN** an authenticated user navigates to `/configmaps/{ClusterId}/{Namespace}/{Name}` for a real ConfigMap
- **THEN** the page renders the toolbar and two tabs (YAML, 键值) with the YAML tab selected by default
- **AND** the YAML tab renders the full `V1ConfigMap` YAML read-only (including `data`, `binaryData`, `labels`, `annotations`, `metadata`)

#### Scenario: 键值 tab lists every key

- **WHEN** the ConfigMap's `Data` contains three keys
- **THEN** the 键值 tab's table lists three rows with monospace 键 and truncated 值 cells

#### Scenario: 键值 tab empty state

- **WHEN** the ConfigMap's `Data` dictionary is empty
- **THEN** the 键值 tab shows the `.empty-state` placeholder(如 `[ 暂无键值 ]`),不渲染表格

#### Scenario: 键值 tab is read-only

- **WHEN** an Admin views the 键值 tab
- **THEN** no editing affordance exists; the only edit path remains the toolbar's 编辑 YAML navigation to the YAML editor route

#### Scenario: ConfigMap not found

- **WHEN** the detail page loads a ConfigMap whose `GetConfigMapAsync` returns null
- **THEN** the page shows the "ConfigMap 不存在或已被删除" empty-state card with a back affordance and no tab bar
