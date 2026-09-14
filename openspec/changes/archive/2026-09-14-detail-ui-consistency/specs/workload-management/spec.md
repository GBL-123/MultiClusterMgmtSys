## MODIFIED Requirements

### Requirement: 工作负载导航入口
系统 SHALL 在侧边导航(Drawer)提供「工作负载管理」导航组(`MudNavGroup`,图标区分于既有项),按 `/workloads` 前缀匹配保持展开,内含四个子入口:部署管理(`/workloads/deployments`)、有状态应用(`/workloads/statefulsets`)、守护进程(`/workloads/daemonsets`)、副本集(`/workloads/replicasets`),对所有登录用户可见。列表页 SHALL 同时支持不带集群与带集群参数两种路由(`/workloads/{kind}` 与 `/workloads/{kind}/{ClusterId:int}`),沿用集群选择状态(`ClusterSelectionState`)在无参路由下恢复上次选择。

#### Scenario: 导航组展开与子入口
- **WHEN** 任意登录用户打开侧边导航
- **THEN** 可以看到「工作负载管理」导航组,展开后含部署管理、有状态应用、守护进程、副本集四个子入口

#### Scenario: 会话内集群记忆
- **WHEN** 用户在 `/workloads/deployments` 直接访问(不带集群参数)
- **THEN** 页面恢复会话中上次选择的集群并加载其工作负载列表

### Requirement: 工作负载详情页
系统 SHALL 为每种类型提供详情页(路由 `/workloads/{kind}/{ClusterId:int}/{Namespace}/{Name}`),页面布局为工具栏(`WorkloadDetailToolbar`)加上 `MudTabs` 双 tab:YAML(只读 `yaml-textarea` 卡片)与运行状态(状态字段卡 + 条件卡两张卡片),tab 选择为页面局部状态(见 detail-page-tabs),默认选中「YAML」(与 ConfigMap、服务、命名空间详情页的 tab 顺序一致)。运行状态 tab SHALL 由 `WorkloadStatusCard`(副本数/就绪/已更新/标签选择器/UID/创建时间,使用与节点概览卡一致的标签/值展示词汇,不使用 `MudSimpleTable`)与 `WorkloadConditionsCard`(条件表)纵向堆叠组成,两卡均全宽;条件卡在无条件下 SHALL 仍渲染卡片并显示 `.empty-state` 空态占位「[ 暂无条件 ]」。条件表的类型与状态 SHALL 遵循 `display-conventions` 以中文主行 + 英文原值次行展示(`Available` → 可用、`Progressing` → 进行中、`ReplicaFailure` → 副本失败;`True` → 成立、`False` → 不成立、`Unknown` → 未知);条件的 Reason/Message 保持原文;`UID` 字段标签保持原文。工具栏操作保持:刷新、编辑 YAML、扩缩容(按矩阵)、重启(按矩阵)、删除。资源不存在时 SHALL 显示"不存在或已被删除"空态并可返回列表。

#### Scenario: 详情页加载
- **WHEN** 用户从列表进入工作负载详情页
- **THEN** 页面默认显示「YAML」tab,可切换到「运行状态」tab;工具栏动作按可用性矩阵渲染且保持在 tab 区之外

#### Scenario: 运行状态 tab 双卡组成
- **WHEN** 运行状态 tab 渲染
- **THEN** `WorkloadStatusCard` 在上、`WorkloadConditionsCard` 在下纵向堆叠,两卡均为全宽
- **AND** 状态字段以标签/值布局展示,不渲染 `MudSimpleTable`

#### Scenario: 条件为空仍显示卡片
- **WHEN** 工作负载没有任何条件
- **THEN** 条件卡仍渲染,内容显示「[ 暂无条件 ]」空态占位

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
