# workload-management delta

## MODIFIED Requirements

### Requirement: 工作负载详情页

系统 SHALL 为每种类型提供详情页(路由 `/workloads/{kind}/{ClusterId:int}/{Namespace}/{Name}`),页面布局为工具栏(`WorkloadDetailToolbar`)加上 `MudTabs` 双 tab:运行状态(副本数/就绪/已更新/标签选择器/UID/创建时间与条件表)与 YAML(只读 `yaml-textarea` 卡片),tab 选择为页面局部状态(见 detail-page-tabs),默认选中「运行状态」。工具栏操作保持:刷新、编辑 YAML、扩缩容(按矩阵)、重启(按矩阵)、删除。资源不存在时 SHALL 显示"不存在或已被删除"空态并可返回列表。

#### Scenario: 详情页加载

- **WHEN** 用户从列表进入工作负载详情页
- **THEN** 页面默认显示「运行状态」tab,可切换到「YAML」tab;工具栏动作按可用性矩阵渲染且保持在 tab 区之外

#### Scenario: 对象已被删除

- **WHEN** 详情页请求的工作负载在集群中不存在
- **THEN** 页面显示不存在提示与"返回列表"入口,不渲染 tab 栏
