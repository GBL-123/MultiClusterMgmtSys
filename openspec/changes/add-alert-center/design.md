# Design

## Context

数据管道已存在、缺的只是评估器(动机见 proposal.md):

- `ClusterSyncBackgroundService`(Infrastructure/Sync)按 `ClusterSyncSettingService` 三层回退设置(DB AppSetting → appsettings.json → 代码默认)轮询 `ClusterService.RefreshAllClustersStatusAsync`;启动即跑首轮(先执行后休眠)。
- `ProbeAsync` **每次探测无论成败都刷新** `ClusterInfo.LastCheckedAt`(失败分支另置 Offline、NodeCount=0);**仅成功**追加 `ClusterHealthSnapshot`(只追加,含 CapturedAt 与就绪统计)。
- `IClusterHealthRepository.GetLatestPerClusterAsync()` 已提供「每集群最近一条快照」;`IClusterRepository.GetAllForDashboardAsync()` 提供无跟踪集群全量读。
- 看板新鲜度判定 = `LastCheckedAt` 超生效同步间隔 × 2(停用/从未同步各自表达)。

## Goals / Non-Goals

**Goals:**

- 把三条规则落到**可实现且不失效的字段语义**(锚点、阈值、边界)。
- open→resolved 状态机由评估器单写者驱动,历史持久保留。
- Admin-only 呈现:路由服务端强制 + 铃铛/导航/设置卡接线。
- 测试用种子数据(陈旧的 CapturedAt/LastCheckedAt)驱动全部开/解析边界,不引入时钟注入。

**Non-Goals:**

- 不做未读态、通知推送(SignalR/邮件)、人工解析入口、告警保留策略。
- 不改同步链路(`RefreshAllClustersStatusAsync` / `ClusterSyncBackgroundService` / `SnapshotRetentionService`)与看板页面。
- 评估间隔不暴露配置(硬编码 1 分钟)。
- 不做资源级规则(CrashLoop 等,v2 进化口)。

## Decisions

### D1 规则一锚点 = 最近快照 CapturedAt(最近成功探测时间)

`LastCheckedAt` 的语义是「最近一次连通性探测时间」——**失败探测也刷新**(ProbeAsync 失败分支同样置 `LastCheckedAt = UtcNow`)。若以它作「离线持续」的锚点,「同步正常运行 + 集群离线」的主流场景(探测每 5 分钟失败一次、LastCheckedAt 恒新鲜)永远触发不了规则一。快照只在探测成功时追加,故**最近快照 CapturedAt = 最近一次成功探测时间**,即 spec「自其最近一次在线检查时间起」的落地锚点。

判定:`Status == Offline && 最近快照存在 && (UtcNow - CapturedAt) > OfflineThresholdMinutes`。

- 备选:锚 LastCheckedAt(否决,如上失效);新增 `OfflineSince` 字段(否决——违背「同步链路一行不改」)。
- 边界:从未成功探测(无快照)且 Offline 的集群无锚点 → 不开立(无判定基线,与规则三对空 LastCheckedAt 同口径;此类集群由看板「需要关注·从未探测」呈现)。

### D2 规则三锚点 = ClusterInfo.LastCheckedAt(最近探测尝试,成败皆计)

与看板新鲜度**同一谓词**:同步启用 && LastCheckedAt 非空 && (UtcNow - LastCheckedAt) > 生效间隔 × 2。由于失败探测也刷新 LastCheckedAt,该规则实际检测「**探测尝试本身停止**」(同步服务停摆/长卡),与规则一互补:探测在跑而集群挂 → 规则一;探测本身停 → 规则三。同一故障不会双报。

- spec 中「最近成功探测时间(LastCheckedAt)」的措辞按字段实际语义(最近探测尝试,成败皆计)理解;操作口径(阈值 = 生效间隔 × 2、停用/从未探测不触发、随间隔设置自动跟随)不变,与看板零漂移。
- 启动即跑首轮同步 → 应用重启不产生人为断流窗口。
- 备选:锚最近快照 CapturedAt(否决——「探测在跑但持续失败」会被误判成断流,与规则一重叠双报)。

### D3 规则二口径与跨规则独立性

判定:最近快照存在 && `NotReadyNodes > 0`;开立记录 `Detail = "未就绪节点 {n} 个"`(spec 要求告警信息含未就绪数)。无快照不开。**三条规则独立评估、不做跨规则抑制**:离线中最近快照陈旧且含未就绪时,规则一、二可能并行 open——如实反映本地最后已知状态,与看板「需要关注」多项并列口径一致。

### D4 AlertRecord 表与状态机

字段:`Id` / `ClusterId`(FK → ClusterInfo,**级联删除**,随 ClusterHealthSnapshot 先例)/ `RuleKind`(`AlertRuleKind`:ClusterOffline / NodeNotReady / SnapshotStalled)/ `Detail`(string?,仅规则二填)/ `OpenedAt`(UTC)/ `ResolvedAt`(UTC,可空)。**open ≡ ResolvedAt IS NULL**,不设独立 status 列(派生态,避免双写漂移)。

- `Domain/Enums/AlertRuleKind.cs` 同文件附 `AlertRuleText.ToChineseText`(集群离线/节点未就绪/快照断流),镜像 `ClusterStatus + ClusterStatusText` 同文件模式。
- 部分唯一索引 `(ClusterId, RuleKind) WHERE ResolvedAt IS NULL`(EF `HasFilter`)兜底「同键至多一条 open」——评估器是单写者(串行循环)本已保证,DB 约束为纵深防御。
- 备选:存 status 列(否决——冗余);保留策略(否决——spec 要求历史持久保留)。

### D5 评估器:独立计时、恒运行、1 分钟硬编码

`AlertEvaluationBackgroundService`(Infrastructure/Sync,镜像 `ClusterSyncBackgroundService` 结构:scopeFactory + logger + BackgroundService):每轮新作用域解析 `AlertService.EvaluateAsync()` → 整轮失败 LogError 不中断 → `Task.Delay(1 分钟)` 循环。

- **无启用开关**(区别于同步服务):停用同步 ≠ 停用值守——停用前已离线的集群,规则一/二仍须照常评估;规则三自身按「停用不触发」排除。
- 1 分钟硬编码不暴露配置:纯 SQLite 读极便宜,少一个配置面(探索阶段用户定案)。
- `EvaluateAsync` 逻辑:读(设置/集群/最近快照/当前 open 记录)→ 三规则求「应开集合」→ 与当前 open diff:缺则 `AddAsync(OpenedAt = UtcNow)`、余则 `ResolveAsync(ResolvedAt = UtcNow)`;返回 (opened, resolved) 供日志与断言。逐记录保存(量级 = 集群数 × 3 规则,毫秒级)。
- 评估零 K8s 调用、零审计写入(spec);配置变更走设置服务(有审计)。

### D6 配置照抄 ClusterSyncSettingService 模式

`AlertSettingService`(Application/Services):键 `Alert:OfflineThresholdMinutes`,三层回退 DB AppSetting → appsettings.json(`"Alert": { "OfflineThresholdMinutes": 10 }`)→ 代码默认 10 分钟;范围校验 1~1440;`UpdateAlertSettingsAsync` 服务端 Admin 强制(`PermissionException`)+ `ValidationException` 中文提示 + 审计(`AuditCategory.Cluster` / `AuditAction.Update`,「告警设置: 离线持续阈值 N 分钟」)。UI = Profile 页 `_isAdmin` 块内、同步设置卡之后并列「告警设置」卡(MudNumericField + 保存 + 说明),镜像既有卡结构与角色口径;告警页保持只读纯列表。

### D7 页面、铃铛与导航接线

- `Web/Components/Alerts/Pages/Alerts.razor`:`@page "/alerts"` + `@attribute [Authorize(Roles = "Admin")]`(路由级服务端强制;Blazor 电路无对外告警 API 端点,路由授权即服务端强制,Member → 全局 access-denied);MudTable ServerData 分页(`TableState` → `AlertListRequest` 组件边界翻译);列 = 状态(StatusBadge:告警中/offline、已解析/online)/ 规则中文 / 集群(`.link-primary` 链集群详情)/ 信息(Detail,空显 —)/ 开立时间 / 解析时间(mono);状态过滤默认 open;空态 `.empty-state`;`.alerts-table` 登记 app.css flex-fill 三处。
- `Web/Components/Alerts/Shared/AlertBell.razor`:MudIconButton + MudBadge,组件内 1 分钟轮询 `GetOpenCountAsync()`(无 SignalR 推送基建,最坏 评估 1′ + 轮询 1′ ≈ 2 分钟上角标,探索阶段用户定案);open 数 0 不显角标;点击 NavigateTo `/alerts`;`IAsyncDisposable` 释放计时。接入 `AppBar.razor` 既有 `<AuthorizeView>` 的 Authorized 块内再嵌 `Roles="Admin"`(置于用户名左侧)。
- `Drawer.razor`:`AuthorizeView Admin` 段、「账号管理」之前新增「告警中心」NavLink(Notifications 图标)。
- 查询读复用:`GetAlertsAsync` 集群名经 `GetAllForDashboardAsync` join(不存集群名快照,改名不漂移);返回 `PagedResult<AlertListItemViewModel>`,默认 OpenedAt 倒序。

### D8 端口与 DI 归位

`IAlertRepository`(GetOpenAsync / CountOpenAsync / GetPagedAsync / AddAsync / ResolveAsync)在 Application/Abstractions,实现在 Infrastructure/Persistence;`AlertService` / `AlertSettingService` 经 `AddApplicationServices()` 注册;`AlertEvaluationBackgroundService` 经 `AddInfrastructure()` `AddHostedService` 注册(与 ClusterSyncBackgroundService 并列)。集群读复用 `GetAllForDashboardAsync`(无跟踪;量级极小,Include(Group) 开销可忽略,零新端口方法);同步设置读复用 `ClusterSyncSettingService.GetClusterSyncSettingsAsync()`。

## Risks / Trade-offs

- [规则一锚依赖快照留存] 快照保留策略把某集群快照全清(同步停摆超过保留窗)且集群 Offline → 规则一失去锚点而解析,规则三仍开 → 单边解析。→ 接受:极角落(离线 + 全量快照被清),规则三兜底,记录在案。
- [角标延迟] 无推送基建,最坏约 2 分钟才上角标 → v1 接受(分钟级值守定位);v2 可加 SignalR 推送。
- [告警量] 集群数 × 3 规则量级极小;告警历史永久累积(年千行级)→ SQLite 无感;保留策略 v2 再议。
- [每 Admin 电路每分钟一次 CountOpen] → 纯部分索引读,开销可忽略。
- [HasFilter 方言] SQLite 原生支持部分索引,EF Core SQLite `HasFilter` 直透;万一遇方言问题,评估器单写者已保证唯一,DB 约束可降级为普通复合索引,不阻塞。
- [可测性无时钟注入] 边界断言依赖种子时间差(如 CapturedAt = UtcNow − 11 分钟)→ 分钟粒度、开区间严格大于,确定性足够。

## Migration Plan

- `AlertRecord` 为新表:`EnsureCreated` 不为既有库补建 → 升级必须删除 `MultiClusterMgmtSys.Web/db/` 下库文件后重启重建(已登记集群与凭据需重新录入;随 `add-cluster-dashboard` / `add-helm-management` 先例)。
- 回滚:还原旧版本 + 删库重建即可(告警历史无迁移价值,随库丢弃)。
