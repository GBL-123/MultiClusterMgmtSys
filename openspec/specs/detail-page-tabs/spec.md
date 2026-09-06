# detail-page-tabs

## Purpose

为集群、节点、工作负载与 ConfigMap 详情页提供统一的 tab 分页布局契约:工具栏、加载态与空态保持在 tab 区之外,tab 选择为页面局部状态(不进 URL),集合类 tab 标签携带等宽计数,以及键值表格长文本截断加点击展开查看的交互约定。

## Requirements

### Requirement: 详情页统一采用工具栏 + Tab 分页布局

系统 SHALL 将 ClusterDetail(`/clusters/{Id:int}`)、NodeDetail(`/nodes/{ClusterId}/{NodeName}`)、WorkloadDetailView(四类工作负载详情共用)与 ConfigMapDetail(`/configmaps/{ClusterId}/{Namespace}/{Name}`)的正文由纵向卡片堆改为「`*DetailToolbar` + `MudTabs`」结构。工具栏(返回、标题、状态展示、角色门控操作)SHALL 渲染在 tab 区之外;页级 loading(`MudProgressLinear`)分支、未找到/集群不可达空态分支 SHALL 保持在 tab 区之外;每个 tab 面板 SHALL 以既有卡片(`MudCard`)作为内容容器,卡片内的字段与表格契约由各页面 spec 保持不变。系统 SHALL NOT 引入新的服务方法或 ViewModel 字段来支撑 tab 化(数据仍在进入页面时一次性拉取)。

#### Scenario: 工具栏在 tab 区之外

- **WHEN** 任一详情页正常渲染
- **THEN** 返回按钮、标题、状态徽章与操作按钮渲染在 tab 栏上方,且不随 tab 切换消失

#### Scenario: 分支在 tab 之外

- **WHEN** 详情页处于加载中,或目标对象未找到,或集群不可达
- **THEN** 页面显示加载进度条或对应空态卡片,不渲染 tab 栏

#### Scenario: tab 面板内保留卡片容器

- **WHEN** 任一 tab 面板渲染
- **THEN** 面板内容沿用既有 `MudCard` 容器与卡片内空态样式(`.empty-state`、`yaml-card` 等),不因 tab 化丢失

#### Scenario: 数据加载不变

- **WHEN** 用户进入任一详情页
- **THEN** 页面在 `OnInitializedAsync` 阶段一次性拉取全部所需数据,tab 切换不触发新的数据请求

### Requirement: Tab 状态为页面局部状态

Tab 选择 SHALL 为页面内局部状态(绑定 MudBlazor 的 `ActivePanelIndex`,v9 参数名),SHALL NOT 写入路由、URL 查询串、localStorage 或任何持久化存储。页面加载完成后 SHALL 默认选中第一个 tab。

#### Scenario: 默认选中第一个 tab

- **WHEN** 用户进入任一详情页并完成加载
- **THEN** 第一个 tab 处于选中态

#### Scenario: 刷新后回到第一个 tab

- **WHEN** 用户选中第三个 tab 后刷新浏览器
- **THEN** 页面重新加载后选中的是第一个 tab

#### Scenario: 切换不触发导航

- **WHEN** 用户点击另一个 tab
- **THEN** 浏览器 URL 不变化,不产生导航记录,页面不重新发起数据加载

### Requirement: Tab 标签携带计数

当 tab 对应集合类数据时,tab 标签 SHALL 在标题旁以等宽字体(`.font-mono`)展示该项计数,如「集群端点 3」「节点 12」;计数为 0 时 SHALL 显示 `0` 而非隐藏。非集合类 tab(概览、条件、系统信息、YAML 等)SHALL NOT 带计数。

#### Scenario: 集合类 tab 计数

- **WHEN** 集群详情页渲染「集群端点」tab 标签
- **THEN** 标签显示「集群端点」标题与等宽字体的端点数量

#### Scenario: 计数为零仍显示

- **WHEN** 集群未登记任何端点
- **THEN** 「集群端点」tab 标签计数显示为 `0`

#### Scenario: 非集合类 tab 无计数

- **WHEN** 节点详情页渲染「条件」tab 标签
- **THEN** 标签仅显示标题文字,无计数后缀

### Requirement: 键值视图长文本截断与展开查看

当详情页以表格展示键值对(ConfigMap `Data`)时,表格 SHALL 单行渲染每个键值对;超过单元格可容纳宽度的 value SHALL 截断并以省略号结尾,且不得把行高撑开。系统 SHALL 提供点击查看入口,打开的查看对话框 SHALL 以等宽字体完整展示该键的 value 并支持复制到剪贴板;关闭对话框后 SHALL 回到原 tab 且原 tab 选中状态不变。

#### Scenario: 长 value 截断

- **WHEN** ConfigMap 某键的 value 超过表格单元格可容纳宽度
- **THEN** 该单元格单行截断显示并以省略结尾,行高保持密集

#### Scenario: 点击查看全文

- **WHEN** 用户点击某键值行的查看入口
- **THEN** 对话框以等宽字体完整展示该 value 并提供复制操作

#### Scenario: 关闭后状态保持

- **WHEN** 用户关闭查看对话框
- **THEN** 详情页停留在键值 tab,tab 选中状态与滚动位置不丢失
