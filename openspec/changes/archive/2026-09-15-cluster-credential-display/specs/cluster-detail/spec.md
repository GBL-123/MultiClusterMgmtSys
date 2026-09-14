## MODIFIED Requirements

### Requirement: Detail page composes toolbar plus three tabs

The detail page SHALL render a top toolbar followed by a `MudTabs` area with exactly three tabs in this order: 概览 → 集群端点 → 节点. The 概览 tab contains the overview card (identity + connection metadata + Admin credential annex); the 集群端点 tab contains the endpoints table card; the 节点 tab contains the nodes-preview card. Each tab's content is sourced from the single `ClusterDetailViewModel` eager-loaded by the page's `OnInitializedAsync`. The 集群端点 and 节点 tab labels SHALL carry their item counts in monospace per the `detail-page-tabs` capability. Tab selection is page-local state and MUST NOT be routed. The Overview tab merges identity metadata and connection metadata (连接方式, API Server) into one surface; no separate "Connection Info" tab exists.

#### Scenario: Tab composition order

- **WHEN** the detail page renders with a loaded cluster
- **THEN** three tabs appear in this order: 概览, 集群端点 (with endpoint count), 节点 (with node count), and 概览 is selected by default

#### Scenario: Overview tab surfaces identity and connection fields

- **WHEN** the 概览 tab renders
- **THEN** it shows: 集群名称, 版本 (k8s `Version`, `—` if null), 节点数 (`NodeCount` displayed with the 台 unit, e.g. `3 台`), 所属分组 (`GroupName` or "未分组"), 连接方式 (配置文件 (Kubeconfig) / 访问令牌 (Token), per `display-conventions`), API Server (`ApiServer` or `—`), 创建时间 (`CreatedAt` formatted `yyyy-MM-dd HH:mm`), 最后检测时间 (`LastCheckedAt` or `—`)

#### Scenario: Secret reveal is Admin-only and lives in the 概览 tab

- **WHEN** a user in role `Member` views the detail page
- **THEN** the credential annex, its 查看凭据 control, the raw KubeConfig/Token value block, and any fetched `ClusterEditViewModel` are absent from the rendered DOM — while the non-secret fields (连接方式, API Server) remain visible to all roles

#### Scenario: Nodes preview links to dedicated nodes page

- **WHEN** the 节点 tab renders and the cluster is reachable with at least one node
- **THEN** the tab shows a compact table of the first few nodes with a "查看全部" link navigating to `/nodes/{Id}` (the existing `Nodes.razor` route)

#### Scenario: Endpoints table contract unchanged inside its tab

- **WHEN** the 集群端点 tab renders with at least one `Vip` and one `Domain` row
- **THEN** a single `MudTable` renders both rows (no group headers), VIP rows above Domain rows, within each kind sorted by `SortOrder` ascending

### Requirement: Show-secret toggle reuses `GetClusterForEditAsync` as-is

The Admin-only credential reveal lives in the 概览 tab's overview card as a "连接凭据" annex and SHALL reuse the existing `ClusterService.GetClusterForEditAsync(id)` call as its only data path; this change MUST NOT introduce a new service method for secret reading. The annex SHALL be a single-control disclosure: a redacted placeholder state that shows the credential type (配置文件 (Kubeconfig) / 访问令牌 (Token), per `display-conventions`) without rendering any part of the raw value, and a revealed state that renders `KubeConfig` or `Token` read-only in a monospace block. Visibility SHALL be driven by exactly one toggle control (查看凭据 ↔ 隐藏); a second content-masking control (visibility adornment) MUST NOT exist. The revealed state SHALL offer copy-to-clipboard of the raw credential value. The overview card header MUST NOT carry a show-secret action button.

#### Scenario: Redacted state reveals nothing

- **WHEN** an Admin views the 概览 tab before revealing
- **THEN** the annex shows the credential type and a redacted placeholder, the raw `KubeConfig`/`Token` value is absent from the rendered DOM, and 查看凭据 is the only actionable control

#### Scenario: Secret toggle is lazy

- **WHEN** the Admin clicks 查看凭据
- **THEN** `GetClusterForEditAsync(Id)` is called only the first time the annex is expanded per page session, the raw value renders in a read-only monospace block, and subsequent 隐藏 / 查看凭据 toggles reuse the cached `ClusterEditViewModel` without another service call

#### Scenario: Copy copies the raw credential

- **WHEN** the Admin clicks 复制 in the revealed annex
- **THEN** the raw `KubeConfig`/`Token` value is written to the system clipboard and a snackbar reports success ("已复制到剪贴板") or failure

#### Scenario: Loading and failure states

- **WHEN** the Admin expands the annex and `GetClusterForEditAsync` is in flight or fails
- **THEN** the annex shows a loading indicator while in flight, and on failure reports through the standard exception presenter and returns to the redacted state without rendering a raw value

#### Scenario: Secret never exposed to Members

- **WHEN** a user in role `Member` views the detail page
- **THEN** the credential annex, its 查看凭据 control, the fetched `ClusterEditViewModel`, and the raw value block do not exist on the rendered DOM
