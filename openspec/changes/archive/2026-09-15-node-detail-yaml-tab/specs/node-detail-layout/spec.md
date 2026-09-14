## RENAMED Requirements

- FROM: `### Requirement: Node detail page five-tab composition`
- TO: `### Requirement: Node detail page tab composition`

## MODIFIED Requirements

### Requirement: Node detail page tab composition

The system SHALL render the node detail page (`/nodes/{ClusterId}/{NodeName}`) as a `NodeDetailToolbar` `MudPaper` followed by a `MudTabs` area with exactly six tabs in this order: YAML → 基本信息 → 资源容量 → 条件 → 标签与注解 → 系统信息. The YAML tab SHALL be selected by default and SHALL contain the read-only `NodeYamlViewCard` (`yaml-card` with a readonly `yaml-textarea`) rendering the node's serialized YAML, with no edit affordance; when the YAML text is empty the card SHALL show the `.empty-state` placeholder `[ 暂无 YAML ]`. The 基本信息 tab contains `NodeOverviewCard`, `NodeAddressesCard` and `NodeTaintsCard` stacked vertically, each full width within the tab panel; the 资源容量 tab contains `NodeResourcesCard`; the 条件 tab contains `NodeConditionsCard`; the 标签与注解 tab contains `NodeLabelsCard` and `NodeAnnotationsCard` stacked vertically, each full width within the tab panel; the 系统信息 tab contains `NodeSystemInfoCard`. The former standalone `NodeSchedulingCard` and `NodeMetadataCard` components SHALL remain deleted. Tab selection is page-local state per the `detail-page-tabs` capability.

#### Scenario: Tab composition order
- **WHEN** a reachable node is rendered
- **THEN** the tabs appear in this order: YAML, 基本信息, 资源容量, 条件, 标签与注解, 系统信息, with YAML selected by default

#### Scenario: YAML tab renders the serialized node read-only
- **WHEN** the YAML tab renders for a loaded node
- **THEN** it shows the node's serialized YAML in a readonly `yaml-textarea` inside the `yaml-card`
- **AND** no edit or save affordance is rendered

#### Scenario: Cards that lost their standalone component
- **WHEN** the detail page renders
- **THEN** no `NodeSchedulingCard` or `NodeMetadataCard` component is instantiated anywhere

#### Scenario: 基本信息 tab card composition
- **WHEN** the 基本信息 tab renders
- **THEN** `NodeOverviewCard`, `NodeAddressesCard` and `NodeTaintsCard` appear stacked vertically in that order
- **AND** each card occupies the full width of the tab panel

#### Scenario: Labels and annotations share one tab
- **WHEN** the 标签与注解 tab renders
- **THEN** `NodeLabelsCard` and `NodeAnnotationsCard` appear stacked vertically
- **AND** each card occupies the full width of the tab panel (no `md=6` side-by-side pairing)

#### Scenario: Overview card content unchanged inside its tab
- **WHEN** the 基本信息 tab renders
- **THEN** `NodeOverviewCard` keeps its identity/scheduling/metadata field contract (no `Uid`, `—` for empty fields)
- **AND** the 地址 and 污点 sections move to `NodeAddressesCard` and `NodeTaintsCard`, rendered below it in the same tab
