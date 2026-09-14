# event-management

## Purpose

提供所选集群的 Kubernetes 事件(Kubernetes core/v1 Event)查看能力:从集群实时读取全部命名空间的事件并以最近发生时间倒序呈现,支持按命名空间/级别/关键词即时筛选、按关联对象类型单选分类(带计数、点击即筛)与按列排序,以中英双语徽章和人类可读的相对时间展示,并允许从事件关联对象跳转到系统中已有的资源详情页。事件是只读瞬态数据,本能力不提供增删改、不写审计、不做持久化。

## Requirements

### Requirement: 事件管理页入口与集群上下文
系统 SHALL 提供事件管理页,路由为 `/events` 与 `/events/{ClusterId:int}`。页面 SHALL 复用集群侧栏选择器与全局选中集群状态;访问带 `ClusterId` 的路由 SHALL 将该集群写入全局选中状态。未选中任何集群时,页面 SHALL 提示从左侧选择集群且不发起 K8s 调用;所选集群不存在时 SHALL 展示未找到空态;集群离线时 SHALL 展示不可达卡片且不发起事件读取。加载期间 SHALL 有可见的加载反馈。查看与筛选 SHALL NOT 受角色限制。

#### Scenario: 未选择集群
- **WHEN** 用户打开 `/events` 且全局未选中集群
- **THEN** 页面显示「请从左侧选择一个集群」提示,且不调用集群 K8s API

#### Scenario: 路由指定集群
- **WHEN** 用户打开 `/events/3`
- **THEN** 全局选中集群被设置为 3,页面按集群 3 加载事件

#### Scenario: 集群离线
- **WHEN** 所选集群状态为离线
- **THEN** 页面显示「集群不可达,无法获取事件」空态,且不调用事件 API

#### Scenario: 成员可查看
- **WHEN** 角色为 Member 的用户打开事件管理页
- **THEN** 事件列表、筛选与刷新能力完整可用,页面不出现任何写操作入口

### Requirement: 事件读取与只读契约
事件读取 SHALL 通过事件服务完成:按所选集群的数据库凭据构建客户端,单次列举该集群全部命名空间的 core/v1 事件,并映射为展示数据。调用 SHALL 复用统一 10s 超时与 K8s 异常翻译链路:集群记录不存在时抛出未找到类业务异常;K8s 侧失败(如 403、超时)经翻译后抛出对应业务异常,由页面统一提示。事件读取 SHALL NOT 写审计,SHALL NOT 提供创建、修改或删除事件的接口或界面入口。

#### Scenario: 全命名空间列举
- **WHEN** 事件服务为在线集群列举事件
- **THEN** 返回该集群全部命名空间的事件,并保留 K8s 的聚合语义(同一对象与原因的重复发生已由 API 聚合为带次数的单条)

#### Scenario: 集群不存在
- **WHEN** 以不存在的集群 Id 读取事件
- **THEN** 抛出未找到类业务异常(中文 UserMessage),且不发起 K8s 调用

#### Scenario: K8s 失败翻译
- **WHEN** 事件列举因权限不足被 K8s 拒绝(403)
- **THEN** 异常经统一翻译为权限类业务异常并由页面以统一提示呈现

#### Scenario: 只读无审计
- **WHEN** 用户查看、筛选或刷新事件
- **THEN** 不产生任何审计记录,也不存在增删改事件的入口

### Requirement: 事件列表字段与排序
事件表 SHALL 每行展示:级别、原因、关联对象、消息、次数、最近发生。关联对象 SHALL 展示其 kind、命名空间与名称;次数大于 1 时 SHALL 以 `×N` 形式展示。表格 SHALL 默认按最近发生时间倒序;级别、原因、关联对象、次数与最近发生列 SHALL 支持排序;分页控件 SHALL 展示事件总数。无事件时 SHALL 显示等宽虚线框空态(如 `[ 暂无事件 ]`)。

#### Scenario: 默认倒序
- **WHEN** 事件列表加载完成
- **THEN** 行按最近发生时间从新到旧排列

#### Scenario: 重复事件聚合展示
- **WHEN** 某事件的次数为 12
- **THEN** 次数列显示 `×12`,列表不额外拆分重复行

#### Scenario: 排序与总数
- **WHEN** 用户点击「次数」列排序标签
- **THEN** 行按次数排序,分页区显示事件总数

#### Scenario: 空列表
- **WHEN** 所选集群返回零条事件
- **THEN** 表格区域显示 `[ 暂无事件 ]` 等宽虚线框空态

### Requirement: 最近发生时间回退与展示
事件的最近发生时间 SHALL 按固定回退链取值:`LastTimestamp` → 序列最后观测时间(`Series.LastObservedTime`) → `EventTime` → 元数据创建时间。列表 SHALL 以相对时间(如「3 分钟前」)作为主展示,悬停时 SHALL 通过统一 tooltip 展示对应的绝对时间;详情 SHALL 展示首次发生与最近发生的绝对时间。回退链全部为空时 SHALL 展示占位符,不得显示空白。

#### Scenario: 新老集群字段兼容
- **WHEN** 事件仅提供 `EventTime` 或序列最后观测时间而 `LastTimestamp` 为空
- **THEN** 列表仍显示正确的最近发生相对时间

#### Scenario: 相对时间带绝对 tooltip
- **WHEN** 用户悬停某事件的相对时间
- **THEN** 显示统一 tooltip,内容为绝对时间(如 `2026-09-14 14:31:02`)

#### Scenario: 时间字段全空
- **WHEN** 事件所有候选时间字段均为空
- **THEN** 最近发生列显示占位符 `—`,列表其余字段正常展示

### Requirement: 筛选与刷新
页面 SHALL 提供命名空间、级别、关键词与对象类型筛选,且 SHALL 全部在前端即时生效(不设「查询」按钮):命名空间下拉 SHALL 包含「全部命名空间」与集群实际命名空间;级别下拉 SHALL 包含全部、正常、警告,提交值保持 K8s 原始值;关键词 SHALL 对关联对象名称、原因与消息做包含匹配(忽略大小写);对象类型分类 SHALL 以带计数的单选分类条呈现,点击即筛。页面 SHALL 提供重置与刷新;刷新 SHALL 重新读取事件并更新「数据截至」时间戳,重置 SHALL 清空全部筛选并恢复全量。筛选与刷新 SHALL NOT 修改集群数据或事件数据。

对象类型分类条 SHALL 按关联对象 kind 分组并满足:分类与计数 SHALL 全部由已加载事件在前端计算,SHALL NOT 发起新的 K8s 调用;每类计数 SHALL 反映命名空间/级别/关键词筛选后的集合且不含对象类型维度自身;分类条 SHALL 仅显示计数大于 0 的类,SHALL 按计数降序排列(同数按 kind 名升序);分类条 SHALL 含「全部」选项,选中即清空对象类型筛选;`InvolvedKind` 为空的事件 SHALL 仅在「全部」下出现,SHALL NOT 臆造分类。

#### Scenario: 即时筛选
- **WHEN** 用户在关键词输入框输入 `BackOff`
- **THEN** 列表立即只保留原因或消息命中该关键词的事件,无需额外点击查询按钮

#### Scenario: 级别筛选保持原始值
- **WHEN** 用户在级别下拉选择「警告」
- **THEN** 过滤按原始级别值 `Warning` 执行

#### Scenario: 对象类型分类点击即筛
- **WHEN** 用户点击分类条中的 `Pod 51`
- **THEN** 列表立即只保留关联对象 kind 为 `Pod` 的事件,其余筛选维度保持不变

#### Scenario: 分类计数反映其余筛选
- **WHEN** 用户已选择级别「警告」
- **THEN** 分类条计数按警告事件统计(如 `Pod 8`、`Node 2`),且点击某一分类后计数不因该分类被选中而塌缩

#### Scenario: 空关联对象类型不臆造分类
- **WHEN** 某事件的 `InvolvedKind` 为空
- **THEN** 分类条不为其生成分类项,该事件仅在「全部」下可见

#### Scenario: 刷新更新时间戳
- **WHEN** 用户点击刷新
- **THEN** 事件重新读取,「数据截至」更新为最近一次成功读取的时间

#### Scenario: 重置
- **WHEN** 用户点击重置
- **THEN** 命名空间回到全部、级别回到全部、关键词清空、对象类型回到「全部」,列表恢复全量

### Requirement: 事件类型双语徽章
事件类型 SHALL 以淡彩底 + 深字的徽章展示,主行为中文、次行为等宽英文原值:`Normal` → 正常、`Warning` → 警告;未识别类型 SHALL 回退为原始文本单行展示。徽章 SHALL 使用既有状态徽章视觉公式与圆角,SHALL NOT 引入新的强调色或实心色块。

#### Scenario: 警告事件
- **WHEN** 事件类型为 `Warning`
- **THEN** 徽章主行显示「警告」,次行以等宽字体显示 `Warning`,配色与 ui-theme 状态徽章契约的警告变体一致

#### Scenario: 未知类型回退
- **WHEN** 事件类型为未识别的值
- **THEN** 该值以原始文本展示,不显示空白或臆造中文

### Requirement: 消息截断与事件详情
消息列 SHALL 单行渲染,超出列宽时截断为省略号且不得撑开行高。页面 SHALL 提供事件详情查看入口:对话框 SHALL 以等宽字体展示完整消息,并展示来源组件(`Source.Component`)、关联对象字段路径(`InvolvedObject.FieldPath`)、首次发生时间、最近发生时间与次数;对话框 SHALL 支持关闭并回到列表原状态。

#### Scenario: 长消息截断
- **WHEN** 某事件消息超出消息列宽
- **THEN** 单元格以省略号截断且行高不增加

#### Scenario: 查看完整消息
- **WHEN** 用户点击某事件行打开详情对话框
- **THEN** 完整消息以等宽字体展示,同时可见来源组件、字段路径、首见/末见绝对时间与次数

#### Scenario: 关闭对话框
- **WHEN** 用户关闭详情对话框
- **THEN** 回到列表且筛选、排序与分页状态不变

### Requirement: 关联对象跳转
当事件关联对象的 kind 在系统中存在详情页时,关联对象 SHALL 可点击并跳转到对应详情页:Node → `/nodes/{ClusterId}/{Name}`、Namespace → `/namespaces/{ClusterId}/{Name}`、ConfigMap → `/configmaps/{ClusterId}/{Namespace}/{Name}`、Service → `/services/{ClusterId}/{Namespace}/{Name}`、Deployment/StatefulSet/DaemonSet/ReplicaSet → 各自的 `/workloads/.../{ClusterId}/{Namespace}/{Name}`。没有详情页的 kind(如 `Pod`)SHALL 以纯文本展示且不可点击;目标资源不存在时 SHALL 由目标页面按既有空态处理。

#### Scenario: 跳转工作负载
- **WHEN** 用户点击 kind 为 Deployment 的关联对象
- **THEN** 浏览器导航到该 Deployment 的详情路由并携带当前集群、命名空间与名称

#### Scenario: Pod 不可跳转
- **WHEN** 事件关联对象 kind 为 Pod
- **THEN** 对象名称以纯文本展示,不呈现可点击样式
