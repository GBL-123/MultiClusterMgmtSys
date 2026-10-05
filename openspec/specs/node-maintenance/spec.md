# node-maintenance

## Purpose
提供节点的三个标准维护操作:封锁(cordon)、解封(uncordon)与排空(drain)。管理员在系统内即可完成「摘出维修→放回」的完整生命周期,替代离线 kubectl 操作;排空以尽力迁移语义逐 Pod 走 Eviction API,并对 DaemonSet Pod、无控制器裸 Pod 与 PDB 拦截明确定义边界。

## Requirements

### Requirement: 节点维护操作入口与权限

系统 SHALL 在节点列表行操作列与节点详情工具栏提供「封锁」「解封」「排空」三个操作入口,按节点当前 `Unschedulable` 状态条件渲染(封锁与排空仅在未封锁节点可用,解封仅在已封锁节点可用;已封锁节点仍可再次排空)。三个操作 SHALL 仅对 Admin 角色渲染,SHALL NOT 提供给 Member;服务层 SHALL 独立强制 Admin 角色,非 Admin 调用 SHALL 抛出中文 `PermissionException` 且不调用 K8s API。集群不可达时页面 SHALL 禁用全部维护操作入口。

#### Scenario: 行内入口按状态渲染

- **WHEN** Admin 查看未封锁节点的列表行
- **THEN** 操作列渲染「封锁」「排空」入口,不渲染「解封」

#### Scenario: 已封锁节点可解封与排空

- **WHEN** 节点 `Unschedulable` 为 true
- **THEN** 操作列渲染「解封」与「排空」,不渲染「封锁」

#### Scenario: Member 不可见且服务端拒绝

- **WHEN** Member 用户打开节点列表并对服务层发起封锁调用
- **THEN** 页面不渲染任何维护操作入口,服务层抛出中文权限异常且无 K8s 写请求发生

#### Scenario: 集群不可达禁用入口

- **WHEN** 所选集群 `IsReachable` 为 false
- **THEN** 维护操作入口渲染为禁用态

### Requirement: 封锁与解封

系统 SHALL 允许 Admin 对节点执行封锁:将节点 `spec.unschedulable` 置为 `true`,不修改节点其它字段;解封将其置回 `false`。封锁 SHALL 经轻量确认,解封 SHALL 直接执行(解封是恢复性低风险操作)。操作成功 SHALL 刷新节点数据使「已封锁」表达即时生效,并写审计日志(类别「节点」、操作分别为「封锁」「解封」、目标含节点名与集群)。K8s 调用失败 SHALL 经既有异常翻译链路提示中文错误。

#### Scenario: 封锁成功

- **WHEN** Admin 确认封锁节点 node-a
- **THEN** 节点 `spec.unschedulable` 更新为 true,操作后节点视图显示「已封锁」徽标,审计记录一条「节点 封锁 node-a」

#### Scenario: 解封成功

- **WHEN** Admin 对已封锁节点 node-a 执行解封
- **THEN** `spec.unschedulable` 恢复 false,「已封锁」徽标消失,审计记录一条「节点 解封 node-a」

#### Scenario: 调用失败提示

- **WHEN** K8s API 返回错误(如 403)
- **THEN** 页面经异常翻译提示中文错误,不更新本地节点状态

### Requirement: 排空预检与强确认

系统 SHALL 在 Admin 触发排空时先执行预检:读取该节点上的全部 Pod,按迁移性质分类(控制器 Pod / DaemonSet Pod / 无控制器裸 Pod),在强确认对话框中明示将迁移的 Pod 清单与已知风险。确认对话框 SHALL 说明:排空会先封锁该节点;DaemonSet Pod 将被跳过;存在无控制器裸 Pod 时 SHALL 在确认文案中列名并说明这些 Pod 驱逐后不会重建。用户确认后方可开始驱逐。

#### Scenario: 预检弹确认

- **WHEN** Admin 点击排空,预检发现 3 个控制器 Pod、1 个 DaemonSet Pod、无裸 Pod
- **THEN** 确认对话框显示节点名、Pod 总数,列出 3 个将迁移的 Pod,DaemonSet Pod 标注跳过

#### Scenario: 裸 Pod 风险明示

- **WHEN** 预检发现节点上存在无控制器的裸 Pod
- **THEN** 确认对话框列出这些 Pod 名称,文案说明驱逐后不会自动重建,等待用户确认或放弃

#### Scenario: 预检失败

- **WHEN** 预检阶段 K8s 调用失败
- **THEN** 页面提示中文错误且不进入驱逐,不写审计

### Requirement: 排空执行语义

系统 SHALL 按以下顺序执行排空:①先对节点自身执行封锁(语义 = drain 含 cordon);②对确认清单中的每个控制器 Pod 发 policy/v1 Eviction 请求,逐个执行。执行 SHALL 遵循尽力迁移语义:单个 Pod 驱逐被 PDB 拦截(K8s 429)SHALL 记为「被策略阻塞」,SHALL NOT 中断其余 Pod 的驱逐;DaemonSet Pod SHALL 跳过不调 Eviction;无控制器裸 Pod SHALL 默认不驱逐。系统 SHALL NOT 等待节点 Pod 清零:全部驱逐请求返回结果后即完成并汇报。排空完成 SHALL 向用户汇总「成功 n / 跳过 m / 阻塞 k」并列出阻塞 Pod 名称,写审计日志(类别「节点」、操作「排空」、描述含三个计数);排空过程不用整体取消回滚。已封锁的节点执行排空 SHALL NOT 重复写封锁审计。

#### Scenario: 尽力迁移

- **WHEN** 排空节点上的 5 个控制器 Pod,其中 2 个被 PDB 429 拦截
- **THEN** 其余 3 个照常驱逐,完成后汇报「成功 3 / 跳过 m / 阻塞 2」并列出被阻塞的 2 个 Pod 名

#### Scenario: DaemonSet Pod 不驱逐

- **WHEN** 节点上存在 DaemonSet 所属 Pod
- **THEN** 系统不对其调用 Eviction,计入「跳过」计数

#### Scenario: 裸 Pod 默认不驱逐

- **WHEN** 节点上存在无控制器裸 Pod
- **THEN** 第一版不驱逐该 Pod(不提供强制驱逐选项),计入汇总说明

#### Scenario: 不重复封锁审计

- **WHEN** 对已封锁节点执行排空
- **THEN** 仅写一条「排空」审计,不额外写封锁审计

### Requirement: 排空进度展示

排空执行期间页面 SHALL 展示进度(已完成 Pod 数 / 总数),复用「刷新全部」的进度反馈模式;执行中 SHALL 禁止再次触发同一节点的排空及其它维护操作。执行结束(无论汇总结果)SHALL 恢复操作可用并刷新节点与(如可见的)Pod 数据。

#### Scenario: 执行中进度

- **WHEN** 排空 3 个 Pod 已完成 1 个
- **THEN** 进度展示「正在迁移 1/3」形态,维护操作按钮禁用

#### Scenario: 完成恢复

- **WHEN** 全部驱逐请求返回(含被阻塞项)
- **THEN** 进度结束,按钮恢复可用,节点与相关数据刷新为最新状态

### Requirement: 已封锁状态可见化

系统 SHALL 在节点列表与节点详情对 `Unschedulable=true` 的节点展示「已封锁」小徽标(中文主行,遵循 display-conventions 词汇;原值 Unschedulable 经统一 tooltip 或等宽次行可查)。该徽标 SHALL NOT 改变既有 Ready 三态状态徽章的语义与配色;节点未封锁时 SHALL NOT 渲染该徽标。

#### Scenario: 列表徽标

- **WHEN** 节点列表加载,其中 node-a `Unschedulable=true` 且 Ready
- **THEN** node-a 行同时渲染「就绪」状态徽章与「已封锁」小徽标

#### Scenario: 封锁立即生效

- **WHEN** Admin 完成封锁且页面刷新完成
- **THEN** 该节点行立即呈现「已封锁」徽标

### Requirement: 维护操作审计

三个维护操作成功后 SHALL 各写一条审计日志:类别「节点」,操作分别为「封锁」「解封」「排空」,目标描述含集群与节点名;排空描述 SHALL 含「成功 n / 跳过 m / 阻塞 k」。预检失败与 Member 被拒的调用 SHALL NOT 写审计。

#### Scenario: 排空审计计数

- **WHEN** 排空完成(成功 2 / 跳过 1 / 阻塞 1)
- **THEN** 审计描述含节点名与该三组计数

