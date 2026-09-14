## MODIFIED Requirements

### Requirement: Node IP remarks merge into node read paths

`ClusterNodeService` SHALL merge stored remarks into node data returned to the UI: each `ClusterNodeViewModel.IpAddresses` entry (IP-class addresses only) SHALL carry its stored `Note`; each `ClusterNodeDetailViewModel.Addresses` row of IP-class type SHALL carry its stored `Note`. Nodes, addresses, and IPs that have no stored remark SHALL render without note text. A remark whose `(NodeName, Address)` key no longer matches any live node address SHALL NOT render anywhere.

#### Scenario: Detail page shows stored remarks
- **WHEN** `GetNodeDetailAsync` returns addresses `10.0.0.5` (remark "管理口") and `172.16.8.2` (no remark)
- **THEN** the 地址 card shows "管理口" beside `10.0.0.5`
- **AND** `172.16.8.2` renders without note text

#### Scenario: List surfaces carry merged notes
- **WHEN** `GetClusterNodesAsync` returns a node whose stored remarks include `(node-1, 10.0.0.5) → "管理口"`
- **THEN** the node's `IpAddresses` entry for `10.0.0.5` has `Note == "管理口"`
- **AND** the node's `IpAddresses` list contains one entry per IP-class address, in the node's address order

#### Scenario: Hostname and DNS rows are not remark-eligible
- **WHEN** a node's `Status.Addresses` contains a `Hostname` row
- **THEN** it renders in the detail page's 地址 card without a note
- **AND** it never appears in the remark edit surface

#### Scenario: Stale remarks never render
- **WHEN** a stored remark's `(NodeName, Address)` key does not match any live address of the node
- **THEN** no UI surface displays that remark

### Requirement: Admin manages node IP remarks via dialog

The 地址 card (`NodeAddressesCard`) SHALL expose an Admin-only "管理" button (gated by `<AuthorizeView Roles="Admin">`). Clicking it SHALL open a `NodeIpNotesDialog` listing every live IP-class address of the current node (IP in monospace + type + note text field). The dialog SHALL have "保存" and "取消"; saving SHALL upsert remarks by `(ClusterId, NodeName, Address)` — inserting new keys, updating changed notes, and deleting keys whose note was cleared — via `ClusterNodeService.UpdateNodeIpNotesAsync(clusterId, nodeName, items)`, after which the detail page reloads. Members (non-admin) SHALL see remarks as plain text without the manage affordance.

#### Scenario: Save upserts remarks for the current node
- **WHEN** the admin opens the dialog for node `worker-1`, sets note "数据口" on `172.16.8.2`, clears a previously-set note on `10.0.0.5`, and saves
- **THEN** `UpdateNodeIpNotesAsync` persists a remark `(clusterId, "worker-1", "172.16.8.2") → "数据口"`
- **AND** the remark row for `(clusterId, "worker-1", "10.0.0.5")` no longer exists
- **AND** the detail page re-renders showing "数据口" beside `172.16.8.2`

#### Scenario: Dialog is admin-only
- **WHEN** a Member views the node detail page
- **THEN** no "管理" button is rendered and no edit affordance is available
- **WHEN** an Admin views the same page
- **THEN** the "管理" button is rendered

#### Scenario: Cancel discards edits
- **WHEN** the admin edits notes and clicks "取消"
- **THEN** no remark rows are written and the displayed remarks are unchanged
