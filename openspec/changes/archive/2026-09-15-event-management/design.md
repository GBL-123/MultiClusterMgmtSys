## Context

见 `proposal.md - Why`。与本设计相关的既有约束:

- 列表页范式固定:`Components/<Feature>/Pages` + `Shared/`,集群侧栏(`ClusterSelectSidebar` + `ClusterSelectionState`)、`SvcService`/`SvcQueryRequest`/`SvcListViewModel`/`SvcListFilter` 一条链可整体参照。
- K8s 客户端 19.0.2 同时暴露 `CoreV1.ListEventForAllNamespacesAsync` / `ListNamespacedEventAsync` 与 `EventsV1.*`(均含 watch 变体);`Corev1Event` 字段为 `Type / Reason / Message / Count / FirstTimestamp / LastTimestamp / EventTime / Series / InvolvedObject / Source`,已用 XML 文档核实。
- 所有 K8s 服务复用 `KubernetesClientConfig.Build`(统一 10s 超时)与 `K8sExceptionMapper.Translate`;只读读取不写审计(`AuditService` 不注入)。
- 详情页/表格展示受 `display-conventions` 与 `ui-theme` 约束;`.status-badge` 现有 online/offline/unknown 三变体,`unknown` 恰为琥珀色板。

## Goals / Non-Goals

**Goals:**

- 一个独立事件管理页完成「读、筛、看」:全命名空间事件读取、前端即时筛选与排序、双语类型徽章、相对时间、消息详情对话框、关联对象跳转。
- 服务、映射、筛选、页面四层与既有范式同构,后续加详情页事件 tab 时可直接复用服务与映射。
- 时间字段按回退链接住 K8s 版本差异。

**Non-Goals:**

- 不引入 watch/推送/自动定时刷新(页面只有手动刷新 + 「数据截至」时间戳)。
- 不做事件持久化、跨集群汇总、事件增删改、审计写入。
- 不新增 Pod 详情页,也不实现「工作负载 → Pod → 事件」的所有权链聚合。
- 不做服务端分页/排序(`Corev1Event` 列表无服务端排序能力,量级在 1h TTL 内可控)。

## Decisions

### D1. 只读 core/v1 Event,不做 events.k8s.io/v1 双 API 回退

选 `CoreV1.ListEventForAllNamespacesAsync` 单次列举。core/v1 兼容层字段最全(`Count / FirstTimestamp / LastTimestamp / Source.Component`),与 `kubectl get events` 口径一致,映射只需一套。备选 `EventsV1.*`(`regarding/note/series`,无 `message`/`source`,且 1.19 之前不可用)需要两套映射与回退分支,收益不足;若未来确有需要,可仿 `SvcService.GetSvcEndpointsAsync` 的 EndpointSlice→Endpoints 回退模式增量引入。

### D2. 一次全命名空间拉取,筛选全部在前端

默认视图本就是「全部命名空间」(与 `kubectl get events -A` 一致),因此全量数据必须在手;命名空间、级别、关键词、对象类型一律走前端即时过滤(不设「查询」按钮),对齐 `SvcListFilter.Apply` 的做法,新增 `Models/EventListFilter.cs` 承载 namespace/level/keyword/kind 四个条件(关键词匹配关联对象名 + reason + message,忽略大小写)。MudTable 客户端分页与排序。

备选:命名空间选择走服务端 `ListNamespacedEventAsync` 重取(与 `Svcs` 页一致)——放弃,因为默认全量视图已经拉过全命名空间数据,再重取只增加往返;类型筛选服务端不被 K8s 支持(需 fieldSelector `type=Warning`,但关键词仍要前端,混用两套过滤语义反而难解释)。命名空间下拉仍调 `GetNamespacesAsync`(集群命名空间列表)以对齐其他页面,并把「空事件命名空间」也呈现给用户。

### D3. 最近发生时间采用四级回退链,展示用相对时间

映射层计算 `OccurredAt = LastTimestamp ?? Series?.LastObservedTime ?? EventTime ?? Metadata.CreationTimestamp`。K8s 1.25+ 的 core/v1 事件由 events.k8s.io 生成,兼容层字段填充情况需要在真实集群验证(任务清单含 spike),回退链保证任一版本都取得到值;全空显示 `—`。列表主展示相对时间(如「3 分钟前」),tooltip 显示绝对 `yyyy-MM-dd HH:mm:ss`;排序按 `OccurredAt` 而非展示字符串。相对时间偏离审计日志的绝对时间先例,理由:事件 1 小时 TTL,「多久之前」是核心信息;实现为一个静态格式化方法,不引入依赖。

### D4. 类型徽章扩展两个 `.status-badge` 变体

`.status-badge.warning`(警告,复用 `unknown` 的琥珀色板 `#FBF3DB/#956400`)与 `.status-badge.normal`(正常,中性 `#EDEAE3/#57534E`,与 `.role-badge.member` 同调)。仍然使用既有 `StatusBadge` 组件(`CssClass` + `Text` 中文 + `Raw` 英文次行),组件本身不改。备选:复用现有 `unknown`/`online` 类(零 CSS 改动但类名语义说谎,且 Normal 用绿色过度表达健康)——放弃;新造独立徽章类(重复设计词汇)——放弃。

### D5. 事件详情用对话框而非路由

事件是瞬态数据、无深链价值,不值得独占路由;消息列单行截断(不撑行高),点击行开 `EventDetailDialog`,展示完整消息(等宽)、`Source.Component`、`InvolvedObject.FieldPath`、首见/末见绝对时间与次数。对齐既有「查看完整值」对话框交互。

### D6. kind→路由映射为静态白名单,Pod 等纯文本

在映射层提供静态路由映射:`Node / Namespace / ConfigMap / Service / Deployment / StatefulSet / DaemonSet / ReplicaSet` 八种有详情页的 kind 生成链接(当前集群 Id + 对象命名空间/名称),其余(Pod/Job/Ingress 等)纯文本。Node 事件落在 `default` 命名空间,但节点路由不含命名空间,天然成立。不做所有权链(Pod 是最大缺口,记录为已知限制而非本变更范围)。

### D7. 文件与注册落位

```
MultiClusterMgmtSys/
  Services/EventService.cs                    ListEventsAsync / GetNamespacesAsync
  Requests/EventQueryRequest.cs               record (int ClusterId)
  ViewModels/EventListViewModel.cs            类型/原因/对象/消息/次数/时间/来源
  ViewModels/Mappings/EventMappingExtensions.cs  Corev1Event -> VM + 路由映射 + 时间回退
  Models/EventListFilter.cs                   前端过滤 Apply(静态,对齐 SvcListFilter)
  Components/Events/Pages/Events.razor        /events、/events/{ClusterId:int}
  Components/Events/Shared/EventListFilterBar.razor
  Components/Events/Shared/EventListTable.razor
  Components/Events/Shared/EventDetailDialog.razor
```

- `Program.cs` 注册 scoped `EventService`;`Drawer.razor` 在「网络管理」组之后、「账号管理」之前加「事件管理」导航(无 Admin 门控)。
- 服务只注入 `ClusterRepository / ILogger / Func<KubernetesClientConfiguration, IKubernetes>`,不注入审计;`EventQueryRequest` 仅含 `ClusterId`,保持单原语语义但符合 `service-contracts` 的 Request 对象要求。
- 表格 `Class="events-table"` 复用全高页面滚动模式(同 `.clusters-table` / `.mud-table-container` 链路),空态 `[ 暂无事件 ]`、加载文案 `// 正在加载...` 均按既有词汇。

### D8. 测试落位

- `EventServiceTests`:全命名空间映射、时间回退(`LastTimestamp` 为空走 `Series.LastObservedTime` / `EventTime`)、集群不存在、K8s 403 翻译、`GetNamespacesAsync` 排序与失败;mock 走 `K8sMocks` 新增的事件 setup(底层 `ListEventForAllNamespacesWithHttpMessagesAsync`)。
- 映射与筛选单测:`EventListFilter.Apply`(namespace/type/keyword 组合)与 `ToEventListViewModel` 边界(空字段、count=1)。
- bUnit `EventsPageTests`:无集群提示、离线空态、表格渲染、筛选交互、详情对话框打开/关闭(经 `InvokeAsync` 调度);`BunitServiceExtensions` 增补事件页服务栈。

### D9. 对象类型分类条:单选 chip facet + 计数,零新增 K8s 调用

用户反馈扁平列表「所有类型的 event 混在一起不直观」,选定按关联对象类型分类、形态为**分类条**(对比过表格分区与左侧 facet 栏:分区表受 `MudTable` 无 `GroupBy`、仓库不引入 `MudDataGrid` 的约束需自定义渲染且破坏排序/分页;侧栏改动布局结构;分类条与现有前端筛选同构、成本最低):

- **落点**:扩展现有 `EventListFilterBar` 增加第二行「对象类型: [全部 N] [Pod N] …」,控件用 `MudChipSet<string>`(`SelectionMode.SingleSelection`,`SelectedValue` 双向;`""` 哨兵表示全部)。仓库虽有 `MudToggleGroup` 先例(EditClusterDialog),但分类种类动态且需计数,chip set 更贴 facet 语义。
- **计数口径**(facet 标准做法):每类计数基于「命名空间/级别/关键词筛选后的集合」、**不含对象类型维度自身**——选中某分类后计数不塌缩,可连续横跳;「全部」计数 = 其余筛选下的总数,与分页 `共 N 条` 一致。
- **排序与裁剪**:仅显示 count > 0 的类,计数降序、同数按 kind 名升序;`InvolvedKind` 为空的事件仅在「全部」出现,不臆造「未知」类;kind 数极多时先换行铺开,不做 Top-N(实测超过约 12 类再议折叠)。
- **实现**:`Models/EventListFilter.Apply` 增加 `appliedKind` 参数(精确匹配 `InvolvedKind`);页面新增 `selectedKind` 状态并计算分类选项(如 `Models` 下小 record:`Kind` + `Count`);重置清空该维度;不新增服务层方法、不新增 K8s 调用、不写审计。
- **命名消歧**:原「类型」筛选与表格列头统一更名「**级别**」(下拉:全部级别/正常/警告),新增维度命名「对象类型」,避免两个「类型」并列歧义;`K8sDisplayText.EventTypeText/EventTypeCssClass` 等代码标识符不改名(内部命名,非用户可见)。

## Risks / Trade-offs

- [不同 K8s 版本事件时间字段填充不一致] → 四级回退链兜底,全空显示 `—`;实现期用真实集群 spike 验证一次(见 tasks)。
- [繁忙集群全命名空间事件量大,首屏 payload 偏大] → 1h TTL 内量级通常数百到数千条,可接受;不做服务端 limit(limit 会先截断再排序,破坏「最近优先」语义);若实测过大,后续可加「只看警告」快捷筛选,不改本设计。
- [Pod 事件占比高但无详情页,跳转覆盖不全] → 明确记录为已知限制;后续变更可加 Pod 详情页或 Pod YAML 查看,再扩展白名单。
- [凭据 RBAC 受限时全命名空间列举 403] → 不静默降级为部分命名空间;统一翻译为权限类业务异常并由页面提示,让用户明确感知。
- [相对时间静态渲染,长时间停留在页面上会变旧] → 「数据截至」时间戳 + 手动刷新;不做定时器(与系统无 watch 先例一致)。
- [事件瞬态,阅读期间可能过期] → 设计上不承诺数据持久性;刷新即最新,不提供归档。

## Migration Plan

纯新增能力,无数据库 schema 变更、无配置变更、无数据迁移:部署后立即可用;回滚只需回退应用版本,无残留状态需要清理。
