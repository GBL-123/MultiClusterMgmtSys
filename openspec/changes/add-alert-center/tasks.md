# Tasks

## 1. Domain 与持久化(AlertRecord 表)

- [x] 1.1 新增 `Domain/Enums/AlertRuleKind.cs`(ClusterOffline/NodeNotReady/SnapshotStalled + `AlertRuleText.ToChineseText` 集群离线/节点未就绪/快照断流,镜像 ClusterStatus+ClusterStatusText 同文件模式)与 `Domain/Entities/AlertRecord.cs`(Id/ClusterId/RuleKind/Detail?/OpenedAt/ResolvedAt?,open ≡ ResolvedAt 为空,全量中文 XML 注释),验证 `dotnet build` 0 错误
- [x] 1.2 `Infrastructure/Persistence/ApplicationDbContext.cs` 挂 `DbSet<AlertRecord>` 并配置:ClusterId FK 级联删除(随 ClusterHealthSnapshot 先例)、部分唯一索引 (ClusterId, RuleKind) 过滤 `ResolvedAt IS NULL`(EF `HasFilter`),验证 EnsureCreated 建表成功(由 1.3 仓储测试的 SQLite 内存库覆盖)
- [x] 1.3 新增 `Application/Abstractions/IAlertRepository.cs`(GetOpenAsync/CountOpenAsync/GetPagedAsync/AddAsync/ResolveAsync,方法级中文契约注释)与 `Infrastructure/Persistence/AlertRepository.cs`(分页语义镜像既有 GetPagedAsync 元组口径:状态过滤 + OpenedAt 倒序 + 命中总数),新增 `AlertRepositoryTests`(分页过滤排序与总数、同键第二条 open 触发唯一索引冲突、ResolveAsync 回填 ResolvedAt、集群删除级联清空告警)验证全绿
- [x] 1.4 `MultiClusterMgmtSys.Web/appsettings.json` 增加 `"Alert": { "OfflineThresholdMinutes": 10 }`,验证 `dotnet build` 0 错误且 2.1 三层回退用例可读到该层

## 2. Application 层(设置服务、评估服务、后台评估器)

- [x] 2.1 新增 `AlertSettingService`(镜像 ClusterSyncSettingService:键 `Alert:OfflineThresholdMinutes`、默认 10、范围 1~1440、Get 三层回退 / Update 服务端 Admin 强制 + 中文 ValidationException + 审计 AuditCategory.Cluster/Update)与 `AlertSettingsViewModel` / `AlertSettingsUpdateRequest` / `AlertPageQuery`(Application/Models)/ `AlertListRequest` / `AlertListItemViewModel`,经 `AddApplicationServices()` 注册;新增 `AlertSettingServiceTests`(DB 值优先 / DB 非法回退 appsettings / 再回退默认 10;非 Admin 抛 PermissionException;越界抛 ValidationException 中文提示;成功落库 + 写审计)验证全绿
- [x] 2.2 新增 `AlertService.EvaluateAsync`(读 GetClusterSyncSettingsAsync / GetAlertSettingsAsync / GetAllForDashboardAsync / GetLatestPerClusterAsync / GetOpenAsync → 三规则判定:①锚最近快照 CapturedAt 超 OfflineThresholdMinutes、②NotReadyNodes>0 且 Detail=「未就绪节点 N 个」、③启用且 LastCheckedAt 超生效间隔×2 → 与 open diff 开立(OpenedAt=UtcNow)/解析(ResolvedAt=UtcNow),返回 (opened, resolved);零 K8s、零审计),新增 `AlertServiceTests` 判定矩阵(离线 11 分钟开 / 9 分钟不开 / 已开不重复开;NotReady>0 开含 Detail / 回到 0 解析 / 无快照不开;启用且探测停超 interval×2 开 / 停用不开 / LastCheckedAt 空·不开 / 间隔调 30 阈值随 60;恢复在线解析留痕;评估后审计表零新增)验证全绿
- [x] 2.3 `AlertService` 查询面:`GetOpenCountAsync()` 与 `GetAlertsAsync(AlertListRequest) → PagedResult<AlertListItemViewModel>`(集群名经 GetAllForDashboardAsync join、规则中文文本、状态派生、默认 OpenedAt 倒序),经 `AddApplicationServices()` 注册;`AlertServiceTests` 查询用例(状态过滤 / 分页与总数 / 集群名拼接 / 空表空页)验证全绿
- [x] 2.4 新增 `Infrastructure/Sync/AlertEvaluationBackgroundService.cs`(镜像 ClusterSyncBackgroundService 结构:每轮新作用域调 EvaluateAsync、整轮失败 LogError 不中断、1 分钟硬编码常量、无启用开关、无配置面)并在 `AddInfrastructure()` 注册 `AddHostedService`,验证 `dotnet build` 0 错误(判定逻辑已由 2.2 测试覆盖,壳层经 4.3 走查核对日志)

## 3. Web UI(告警页、铃铛、导航、设置卡)

- [x] 3.1 新增 `Web/Components/Alerts/Pages/Alerts.razor`(`@page "/alerts"` + `@attribute [Authorize(Roles = "Admin")]`;MudTable ServerData 分页、TableState→AlertListRequest 组件边界翻译;状态过滤默认 open;列 = 状态 StatusBadge(告警中/offline · 已解析/online)/ 规则中文 / 集群 `.link-primary` 链集群详情 / 信息(Detail 空显 —)/ 开立与解析时间 mono;空态 `.empty-state`、加载文案)并在 app.css 登记 `.alerts-table` flex-fill 三处选择器;`BunitServiceExtensions` 新增 `AddAlertStack`(AlertService / AlertSettingService 基建),新增 `AlertsPageFlowTests`(默认 open 过滤请求、行渲染与集群链接、resolved 过滤、空态)验证全绿
- [x] 3.2 新增 `Web/Components/Alerts/Shared/AlertBell.razor`(MudIconButton + MudBadge、1 分钟轮询 GetOpenCountAsync、open 数 0 不显角标、点击 NavigateTo `/alerts`、IAsyncDisposable)并接入 `AppBar.razor` Authorized 块内 `Roles="Admin"`(用户名左侧);新增 `AlertBellTests`(0 无角标 / 3 显角标 / 点击导航)与 AppBar 角色门控断言(Admin 渲染 AlertBell、Member 不渲染)验证全绿
- [x] 3.3 `Drawer.razor` AuthorizeView Admin 段「账号管理」之前新增「告警中心」NavLink(`/alerts`,Notifications 图标),验证 `dotnet build` 0 错误(导航入口在 4.3 走查核对)
- [x] 3.4 `Profile.razor` `_isAdmin` 块内同步设置卡之后并列「告警设置」卡(MudNumericField 离线持续阈值分钟 1~1440 + 保存按钮 + 说明文案;OnInitializedAsync 读 GetAlertSettingsAsync;SaveAlertSettingsAsync 镜像 SaveSyncSettingsAsync 的 Snackbar/异常处理口径),验证 `dotnet build` 0 错误 + bUnit 渲染 Profile 断言 Admin 见卡、Member 不见卡

## 4. 集成验证与文档

- [x] 4.1 更新 `AGENTS.md`(Architecture notes 告警中心条目、Database quirks 的 AlertRecord 建表删库重建警示、Drawer 导航与 Profile 设置卡提及),验证描述与实现逐项一致
- [x] 4.2 全量验证:`dotnet build MultiClusterMgmtSys.slnx` 0 错误 + `dotnet test MultiClusterMgmtSys.Tests` 全绿 + `./coverage.ps1` 四程序集合并行覆盖 ≥75%,验证三条命令输出
- [ ] 4.3 端到端走查(`dotnet run`,删库重建后):1)Member 直访 `/alerts` 被拒、AppBar 无铃铛;2)Admin 见铃铛且 open=0 无角标;3)停用同步或使集群离线,评估轮后角标与页面出现对应告警,恢复后 resolved 留痕;4)Profile 告警设置卡改阈值生效;5)Drawer「告警中心」入口可达——逐项核对 spec 场景
