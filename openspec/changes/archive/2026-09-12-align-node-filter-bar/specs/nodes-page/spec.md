# nodes-page delta (align-node-filter-bar)

## MODIFIED Requirements

### Requirement: Node list filter bar with four filters
The list page SHALL render `NodeListFilterBar` containing exactly four filter controls bound to a single `NodeListFilter` draft held by the page: a free-text Name field (with a search adornment), a Role drop-down, a Status drop-down, and a Schedulability drop-down, followed by a "查询" filled primary button that invokes `OnQuery` and a "重置" outlined button that clears the filter state and invokes `OnReset`. The bar SHALL match the other cluster-scoped list pages' filter bars in structure (spacer pushed query/reset buttons) and SHALL NOT add extra top margin. Editing a control alone SHALL NOT change the table; filtering SHALL be applied only when the user clicks "查询". Filtering MUST be performed client-side against the loaded `List<ClusterNodeViewModel>` — no new server round-trip is introduced when a filter is applied.

#### Scenario: Query applies the drafted filters
- **WHEN** the user edits the Name/Role/Status/Schedulability controls and clicks "查询"
- **THEN** the table shows only nodes matching the drafted conditions

#### Scenario: Editing without query leaves the table unchanged
- **WHEN** the user edits one or more filter controls but does not click "查询"
- **THEN** the table continues to show the previously applied result set

#### Scenario: Reset clears the filter and restores the full list
- **WHEN** the user clicks "重置"
- **THEN** all four controls return to their default values and the table shows all loaded nodes

#### Scenario: Name filter matches by substring (case-insensitive)
- **WHEN** the applied Name filter is a non-empty string
- **THEN** the table shows only nodes whose `Name` contains that value (ordinalIgnoreCase)

#### Scenario: Role filter narrows by label-derived role
- **WHEN** the applied Role filter is a non-null string (e.g. `"control-plane"`, `"worker"`)
- **THEN** the table shows only nodes whose `Roles` field (a comma-joined list of `node-role.kubernetes.io/<role>` labels) contains that value as one of its comma-separated segments

#### Scenario: Status filter narrows by Ready condition
- **WHEN** the applied Status filter is `"Ready"` / `"NotReady"` / `"Unknown"`
- **THEN** the table shows only nodes whose `ClusterNodeViewModel.Status` equals that exact string

#### Scenario: Schedulability filter narrows by Unschedulable flag
- **WHEN** the applied Schedulable filter is `true`
- **THEN** the table shows only nodes whose `Unschedulable` is `true` (i.e. cordoned / not schedulable)
- **WHEN** the applied Schedulable filter is `false`
- **THEN** the table shows only nodes whose `Unschedulable` is `false` (i.e. schedulable)
- **WHEN** the applied Schedulable filter is `null`
- **THEN** the table applies no schedulability filter

#### Scenario: All filters compose
- **WHEN** multiple filters are applied simultaneously
- **THEN** the table shows the intersection of all active filters
