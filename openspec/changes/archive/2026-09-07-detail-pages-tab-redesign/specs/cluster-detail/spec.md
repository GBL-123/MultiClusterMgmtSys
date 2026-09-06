# cluster-detail delta

## REMOVED Requirements

### Requirement: Detail page composes three discrete cards in archive layout

**Reason**: 详情页布局统一改为「工具栏 + MudTabs 分页」,纵向三卡片(archive layout)契约被 tab 组成契约取代;原 requirement 中 "MUST NOT split into multiple pages or tabs" 的限制正式解除。

**Migration**: 由下方 ADDED 的 "Detail page composes toolbar plus three tabs" requirement 取代;概览/端点/节点预览的内容契约原样迁入对应 tab。

## ADDED Requirements

### Requirement: Detail page composes toolbar plus three tabs

The detail page SHALL render a top toolbar followed by a `MudTabs` area with exactly three tabs in this order: 概览 → 集群端点 → 节点. The 概览 tab contains the overview card (identity + connection metadata + Admin secret reveal); the 集群端点 tab contains the endpoints table card; the 节点 tab contains the nodes-preview card. Each tab's content is sourced from the single `ClusterDetailViewModel` eager-loaded by the page's `OnInitializedAsync`. The 集群端点 and 节点 tab labels SHALL carry their item counts in monospace per the `detail-page-tabs` capability. Tab selection is page-local state and MUST NOT be routed. The Overview tab merges identity metadata and connection metadata (连接方式, API Server) into one surface; no separate "Connection Info" tab exists.

#### Scenario: Tab composition order

- **WHEN** the detail page renders with a loaded cluster
- **THEN** three tabs appear in this order: 概览, 集群端点 (with endpoint count), 节点 (with node count), and 概览 is selected by default

#### Scenario: Overview tab surfaces identity and connection fields

- **WHEN** the 概览 tab renders
- **THEN** it shows: 集群名称, 版本 (k8s `Version`, `—` if null), 节点数 (`NodeCount`), 所属分组 (`GroupName` or "未分组"), 连接方式 (Kubeconfig / Token), API Server (`ApiServer` or `—`), 创建时间 (`CreatedAt` formatted `yyyy-MM-dd HH:mm`), 最后检测时间 (`LastCheckedAt` or `—`)

#### Scenario: Secret reveal is Admin-only and lives in the 概览 tab

- **WHEN** a user in role `Member` views the detail page
- **THEN** the 显示密文 toggle, the KubeConfig/Token value fields, and any fetched `ClusterEditViewModel` are absent from the rendered DOM — while the non-secret fields (连接方式, API Server) remain visible to all roles

#### Scenario: Nodes preview links to dedicated nodes page

- **WHEN** the 节点 tab renders and the cluster is reachable with at least one node
- **THEN** the tab shows a compact table of the first few nodes with a "查看全部" link navigating to `/nodes/{Id}` (the existing `Nodes.razor` route)

#### Scenario: Endpoints table contract unchanged inside its tab

- **WHEN** the 集群端点 tab renders with at least one `Vip` and one `Domain` row
- **THEN** a single `MudTable` renders both rows (no group headers), VIP rows above Domain rows, within each kind sorted by `SortOrder` ascending
