## MODIFIED Requirements

### Requirement: 工作负载列表页
系统 SHALL 为四种 apps/v1 工作负载各提供独立列表页,页面结构一致:集群侧栏、集群状态徽章、刷新按钮、命名空间下拉过滤(选项来自集群命名空间列表)、名称搜索、统一列表表格。列表行 SHALL 展示:名称(`.link-primary` 链接,进入详情)、命名空间、就绪度(`n/m 个` 等宽字体,遵循 `display-conventions` 的计数单位)、滚动状态徽章、创建时间、操作列。集群不可达时 SHALL 显示不可达提示并禁用写操作入口;未选择集群时 SHALL 显示空态引导。行内操作 SHALL 为「扩缩容」「重启」「⋯菜单(删除)」,按操作可用性矩阵条件渲染。

#### Scenario: 列表加载与过滤
- **WHEN** 用户选择集群并按命名空间过滤、输入名称搜索
- **THEN** 表格只显示匹配命名空间且名称包含搜索词的工作负载,行内展示就绪度与滚动状态

#### Scenario: 就绪度带计数单位
- **WHEN** 某 Deployment 期望 3 副本且当前 2 个就绪
- **THEN** 就绪度列显示 `2/3 个`

#### Scenario: 名称链接进入详情
- **WHEN** 用户点击列表行中的名称链接
- **THEN** 页面导航到该工作负载的详情页

#### Scenario: 集群不可达
- **WHEN** 所选集群状态为不可达
- **THEN** 页面显示"集群不可达"提示,不提供列表数据与写操作入口

### Requirement: 工作负载详情页
系统 SHALL 为每种类型提供详情页(路由 `/workloads/{kind}/{ClusterId:int}/{Namespace}/{Name}`),页面布局为工具栏(`WorkloadDetailToolbar`)加上 `MudTabs` 双 tab:运行状态(副本数/就绪/已更新/标签选择器/UID/创建时间与条件表)与 YAML(只读 `yaml-textarea` 卡片),tab 选择为页面局部状态(见 detail-page-tabs),默认选中「运行状态」。条件表的类型与状态 SHALL 遵循 `display-conventions` 以中文主行 + 英文原值次行展示(`Available` → 可用、`Progressing` → 进行中、`ReplicaFailure` → 副本失败;`True` → 成立、`False` → 不成立、`Unknown` → 未知);条件的 Reason/Message 保持原文;`UID` 字段标签保持原文。工具栏操作保持:刷新、编辑 YAML、扩缩容(按矩阵)、重启(按矩阵)、删除。资源不存在时 SHALL 显示"不存在或已被删除"空态并可返回列表。

#### Scenario: 详情页加载
- **WHEN** 用户从列表进入工作负载详情页
- **THEN** 页面默认显示「运行状态」tab,可切换到「YAML」tab;工具栏动作按可用性矩阵渲染且保持在 tab 区之外

#### Scenario: 条件表双语展示
- **WHEN** 运行状态 tab 的条件表渲染 `Type = Available`、`Status = True` 的一行
- **THEN** 类型单元格主行显示「可用」、次行显示等宽字体的 `Available`
- **AND** 状态单元格主行显示「成立」、次行显示 `True`

#### Scenario: 未知条件类型回退
- **WHEN** 条件表渲染未登记的类型(如 `CustomCondition`)
- **THEN** 类型单元格以原始文本展示,不显示臆造中文

#### Scenario: 对象已被删除
- **WHEN** 详情页请求的工作负载在集群中不存在
- **THEN** 页面显示不存在提示与"返回列表"入口,不渲染 tab 栏
