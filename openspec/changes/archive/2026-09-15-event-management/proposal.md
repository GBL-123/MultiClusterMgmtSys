## Why

系统已覆盖集群、节点、命名空间、工作负载、服务与 ConfigMap 的查看,但缺少 Kubernetes 事件视图。排查「Pod 为什么起不来」「节点为什么 NotReady」这类问题时,用户必须离开系统、改用 kubectl;事件(默认约 1 小时 TTL)恰是这类问题的第一手线索,也是集群内最需要即时可读的瞬态数据。

## What Changes

- 新增「事件管理」页(`/events`、`/events/{ClusterId:int}`):沿用集群侧栏选集群,新增命名空间/级别/关键词筛选与对象类型分类条(全部前端即时生效,分类条带计数、点击即筛)、刷新按钮与「数据截至」时间戳。
- 对象类型分类条:按关联对象 kind(`Pod`/`Node`/`Deployment`…)单选分类、点击即筛,计数反映其余筛选结果;原「类型」筛选与列头更名「级别」以消除两类「类型」并列的歧义。
- 新增 `EventService`:按所选集群凭据单次列举全部命名空间的 core/v1 事件,映射为展示用 ViewModel(类型/原因/消息/关联对象/次数/首见末见时间/来源),沿用统一 10s 超时与 K8s 异常翻译链路。
- 事件类型(Normal/Warning)以中英双语徽章展示;新增 `.status-badge.warning` / `.status-badge.normal` 视觉变体。
- 消息单元格单行截断,点击行打开详情对话框(完整消息、来源组件、字段路径、首见/末见时间、次数)。
- 关联对象在有详情页的 kind 上可点击跳转(Node/Namespace/ConfigMap/Service/Deployment/StatefulSet/DaemonSet/ReplicaSet);无页面的 kind(如 Pod,事件占比最高)保持纯文本。
- Drawer 导航新增「事件管理」入口。
- 事件为只读瞬态数据:不写审计、不做增删改、不做持久化。

## Capabilities

### New Capabilities
- `event-management`: 所选集群的 Kubernetes 事件查看能力——页面入口与布局、服务读取与结构映射、前端筛选与排序、类型徽章与时间展示口径、关联对象跳转规则、空态/离线/错误处理。

### Modified Capabilities
- `ui-theme`: 「状态徽章」要求新增事件类型 `warning`(警告,沿用未知徽章的琥珀配色公式)与 `normal`(正常,中性色)两个变体,淡彩底 + 深字公式不变。
- `display-conventions`: 枚举双语「SHALL 至少覆盖」清单新增事件类型:`Normal` → 正常、`Warning` → 警告。

## Impact

- 新增代码:`Services/EventService.cs`、`Requests/EventQueryRequest.cs`、`ViewModels/EventListViewModel.cs`、`ViewModels/Mappings/EventMappingExtensions.cs`、`Models/EventListFilter.cs`、`Components/Events/`(页面、筛选条、表格、详情对话框)。
- 修改代码:`Components/Layout/Drawer.razor`(导航)、`wwwroot/css/app.css`(徽章变体)、`Program.cs`(服务注册)。
- 测试:新增 `EventServiceTests`、事件映射与筛选测试、`EventsPageTests`(bUnit),`TestInfrastructure/K8sMocks` 补充事件列举 mock 扩展。
- 无数据库 schema 变更、无新依赖、无 EF 迁移、无破坏性变更。
- 非目标:跨集群事件汇总、watch/自动刷新、事件持久化或归档、删除事件、审计写入、详情页事件 tab(留作后续变更)。
