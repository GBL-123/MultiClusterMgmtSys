# Spec Delta

## Purpose

为多集群管理系统提供告警中心:独立计时的后台评估器基于集群主档与节点健康快照(纯 SQLite,零 K8s 调用)持续评估三条 v1 规则(集群离线持续、节点未就绪、快照断流),把异常固化为可追溯的告警记录(open→resolved,恢复也留痕),并以 Admin-only 的告警页面与 AppBar 铃铛(角标 = 当前 open 数)呈现,让系统从被动查看的仪表盘升级为主动值守。

## ADDED Requirements

### Requirement: 评估器独立计时且零 K8s 调用

告警评估 SHALL 由独立计时的后台服务周期性自动执行(分钟量级),SHALL NOT 挂在定时同步的轮末、SHALL NOT 依赖任何页面访问触发。评估 SHALL 纯读 SQLite(集群主档 + 节点健康快照),SHALL NOT 发起任何 K8s 调用。

#### Scenario: 同步停用后断流仍可触发

- **WHEN** 定时同步已停用或同步服务自身故障,某集群的最近成功探测时间持续超过断流阈值
- **THEN** 断流告警照常开立(评估器独立计时,不受同步轮次是否运行影响)

#### Scenario: 评估不触发 K8s 调用

- **WHEN** 评估周期运行且存在离线集群
- **THEN** 评估不发起任何可达性探测或 K8s 客户端调用,仅基于本地数据判定

### Requirement: 规则一集群离线持续告警

集群状态为离线、且自其最近一次在线检查时间起持续超过可配置的离线持续时间阈值时,系统 SHALL 为该集群开立「集群离线」告警;持续期间 SHALL NOT 重复开立(同一集群同一规则至多一条 open 记录)。离线持续时间阈值 SHALL 可配置并有分钟级默认值。

#### Scenario: 离线超过阈值开立告警

- **WHEN** 某集群状态为离线且持续时长超过阈值
- **THEN** 开立该集群的「集群离线」open 告警,记录开立时间

#### Scenario: 短暂离线不开立

- **WHEN** 某集群状态为离线但持续时长未超过阈值
- **THEN** 不开立「集群离线」告警

#### Scenario: 同键不重复开立

- **WHEN** 某集群的「集群离线」告警已为 open,且下一轮评估条件仍满足
- **THEN** 不再新开记录,既有 open 记录保持

### Requirement: 规则二节点未就绪告警

某集群最近一份节点健康快照的未就绪节点数大于 0 时,系统 SHALL 为该集群开立「节点未就绪」告警,告警信息 SHALL 包含未就绪节点数;同一集群同一规则至多一条 open 记录。

#### Scenario: 快照出现未就绪节点

- **WHEN** 某集群最近快照的未就绪节点数由 0 变为 2
- **THEN** 开立该集群的「节点未就绪」open 告警,信息含未就绪数 2

#### Scenario: 无快照不开立

- **WHEN** 某集群从未产生过健康快照(从未成功探测)
- **THEN** 不对该集群开立「节点未就绪」告警

### Requirement: 规则三快照断流告警

某集群的最近成功探测时间(LastCheckedAt)超过过期阈值时,系统 SHALL 为该集群开立「快照断流」告警。过期阈值 SHALL 复用全局集群看板的新鲜度判定口径(生效同步间隔 × 2),并随同步间隔设置变化自动跟随。同步已停用的集群 SHALL NOT 触发断流告警(管理员知情的主动停用,不误报);从未成功探测过(LastCheckedAt 为空)的集群 SHALL NOT 触发断流告警(无判定基线,归属「从未同步」表达)。

#### Scenario: 启用同步但探测停滞触发

- **WHEN** 某集群启用定时同步、曾有成功探测,但最近成功探测时间已超过生效间隔 × 2
- **THEN** 开立该集群的「快照断流」open 告警

#### Scenario: 同步停用不触发

- **WHEN** 定时同步设置已停用,各集群最近成功探测时间均已陈旧
- **THEN** 不开立「快照断流」告警

#### Scenario: 阈值随间隔设置跟随

- **WHEN** 生效同步间隔由 5 分钟调整为 30 分钟
- **THEN** 断流判定阈值随之调整为 60 分钟口径,不再按 10 分钟误报

### Requirement: 告警状态机与恢复解析

告警记录 SHALL 为 open→resolved 状态机:规则条件不再满足(如集群回到在线、未就绪数回到 0、断流恢复新鲜快照)时,系统 SHALL 将对应 open 记录解析为 resolved 并记录解析时间;解析 SHALL 仅由评估器自动完成,系统 SHALL NOT 提供人工关闭告警的操作。告警历史 SHALL 持久保留(含解析时间),SHALL NOT 物理删除。

#### Scenario: 离线恢复解析留痕

- **WHEN** 某集群「集群离线」告警 open 中,集群恢复在线
- **THEN** 该告警被解析为 resolved 并带解析时间,记录保留可查

#### Scenario: 未就绪恢复解析

- **WHEN** 某集群「节点未就绪」告警 open 中,最近快照未就绪节点数回到 0
- **THEN** 该告警被解析为 resolved

#### Scenario: 无人工解析入口

- **WHEN** 用户在告警页面查看 open 告警
- **THEN** 不存在「关闭/确认告警」类人工操作,解析仅由评估器自动完成

### Requirement: Admin-only 可见性

告警页面路由 SHALL 服务端强制 Admin 角色,非 Admin 访问 SHALL 被拒绝(重定向访问受限页);AppBar 铃铛入口 SHALL 仅对 Admin 渲染。权限强制 SHALL 落在服务端,SHALL NOT 仅依赖 UI 遮挡。

#### Scenario: Member 直接访问告警页被拒

- **WHEN** Member 角色用户直接访问告警页面路由
- **THEN** 被拒绝并导向访问受限页,不返回告警数据

#### Scenario: Member 不见铃铛

- **WHEN** Member 角色用户浏览任意页面
- **THEN** AppBar 不渲染告警铃铛入口

### Requirement: 铃铛角标为当前 open 告警数

Admin 的 AppBar 铃铛 SHALL 显示角标数字,数值 = 当前 open 告警总数;open 数为 0 时 SHALL NOT 显示角标数字。告警体系 SHALL 无未读/已读概念(不以用户为维度的阅读状态)。

#### Scenario: 角标随 open 数变化

- **WHEN** open 告警由 0 条变为 3 条
- **THEN** Admin 的铃铛角标显示 3;全部解析后角标消失

#### Scenario: 点击铃铛进入告警页

- **WHEN** Admin 点击 AppBar 铃铛
- **THEN** 导航到告警页面

### Requirement: 告警页面呈现告警记录

告警页面 SHALL 展示告警记录列表,每条 SHALL 含中文规则名(集群离线/节点未就绪/快照断流)、目标集群、开立时间、状态(open/resolved,中文展示)与解析时间(resolved 时);SHALL 支持按状态过滤(默认 open);无告警时 SHALL 展示空态。页面 SHALL 为只读(无人工解析、无删除)。

#### Scenario: 查看历史告警

- **WHEN** Admin 打开告警页面并切换到已解析过滤
- **THEN** 列表展示 resolved 记录,含开立时间与解析时间

#### Scenario: 无告警空态

- **WHEN** 无任何 open 告警且过滤为 open
- **THEN** 页面展示空态提示

### Requirement: 告警评估不写审计日志

告警的开立与解析为系统自动行为,SHALL NOT 写入审计日志;告警追溯以告警记录自身(含状态与时间戳)为准。

#### Scenario: 评估开立告警不产生审计

- **WHEN** 评估器开立或解析一条告警
- **THEN** 审计日志无新增记录,告警页面可查该记录
