## MODIFIED Requirements

### Requirement: Node list page toolbar mirrors the cluster management page

The node list page's right pane SHALL follow the visual structure of the other cluster-scoped list pages (`Namespaces.razor`, `Svcs.razor`, `ConfigMaps.razor`, `WorkloadListView.razor`): a single `MudPaper pa-4` containing a title row and — only when the cluster is reachable — the filter bar in one card, followed by the table (reachable) or the "集群不可达，无法获取节点列表" card (unreachable). The title row (`NodeListToolbar`) SHALL render: the page title "节点管理" as `Typo.h5`, a single status-colored `MudChip` (`Size.Small`, `Variant.Filled`) showing `{cluster.Name} · {cluster.StatusText}`, a spacer, and a "刷新" `MudButton` (`Variant.Text`, `Color.Inherit`, disabled while `Processing`). The title row MUST NOT render a separate cluster-name chip, an inline progress spinner inside the refresh button, or a back-to-cluster-detail button (navigation back to the cluster detail is via the Drawer/sidebar, not this toolbar). The title row SHALL be rendered whenever the cluster context has loaded, including when `IsReachable == false`; the filter bar SHALL render inside the same `MudPaper` below the title row only when the cluster is reachable; the unreachable message card SHALL render below the `MudPaper`.

#### Scenario: Title row and filter share one paper
- **WHEN** the node list renders for a reachable cluster
- **THEN** `NodeListToolbar` and `NodeListFilterBar` appear inside the same `MudPaper pa-4`
- **AND** the page title reads "节点管理" at `Typo.h5` with the cluster context shown as a single `{Name} · {StatusText}` filled chip
- **AND** the title row contains no back-to-cluster-detail button

#### Scenario: Refresh keeps the toolbar visible
- **WHEN** the user clicks "刷新" while the node list is loaded
- **THEN** the toolbar and filter bar remain rendered and the table shows its loading state ("正在加载...") until the reload completes

#### Scenario: Unreachable cluster keeps the toolbar visible
- **WHEN** the selected cluster's `IsReachable` is `false`
- **THEN** `NodeListToolbar` still renders the title, the cluster context chip, and the refresh button inside the `MudPaper`
- **AND** `NodeListFilterBar` is not rendered
- **AND** the "集群不可达，无法获取节点列表" card renders below the `MudPaper` in place of the table
