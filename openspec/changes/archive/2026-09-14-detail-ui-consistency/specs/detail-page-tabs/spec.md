## REMOVED Requirements

### Requirement: Tab 标签携带计数
**Reason**: 用户反馈 tab 标签计数是视觉噪音(如「键值 1」),计数信息可从 tab 内容本身获得;该行为被新增的「Tab 标签不带计数」需求取代。
**Migration**: 移除各详情页 `MudTabPanel` 的 `TabContent` 计数后缀(集群端点/节点/端口/后端端点/键值),标签仅保留标题文字;命名空间标签/注解卡标题的计数同步去除(见 `namespace-management` 变更)。

## ADDED Requirements

### Requirement: Tab 标签不带计数
详情页 tab 标签 SHALL 仅显示标题文字,SHALL NOT 在标签内展示集合项计数(如「集群端点 3」「节点 12」「键值 1」);集合条目数量 SHALL NOT 成为 tab 标签的一部分。

#### Scenario: 集合类 tab 无计数
- **WHEN** 集群详情页渲染「集群端点」tab 标签
- **THEN** 标签仅显示「集群端点」标题文字,不显示端点数量

#### Scenario: 键值 tab 无计数
- **WHEN** ConfigMap 详情页渲染「键值」tab 标签
- **THEN** 标签仅显示「键值」标题文字,不显示键值对数量
