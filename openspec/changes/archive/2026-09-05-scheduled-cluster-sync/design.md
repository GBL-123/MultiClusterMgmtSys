## Context

集群状态探测的最小工作单元已经存在且是幂等的:`ClusterService.ProbeAsync`(私有,`ClusterService.cs:231`)成功置 `Online` + 版本 + 节点数 + `LastCheckedAt`,失败置 `Offline` + 清空快照,**吞掉异常不抛**(设计上的优雅降级);`RefreshAllClustersStatusAsync`(`:187`)逐集群调用它、单集群失败 catch-and-continue、返回成功数,目前仅由 UI 手动触发。仓库中不存在任何 BackgroundService / Timer / cron 基础设施。所有集群服务为 Scoped,`Func<KubernetesClientConfiguration, IKubernetes>` 为 Singleton;SQLite 单写者,`EnsureCreated` 启动即建库。

约束:
- 异常契约(`openspec/specs/exception-handling/spec.md`):K8s 失败走 `K8sExceptionMapper`,UI 层 `ExHandler`;但 probe 是刻意的静默降级分支,保持不弹错。
- 服务契约:审计用 `AuditService.LogAsync(AuditCategory.X, AuditAction.Y, 中文描述)`;`IProgress` 参数为既有豁免。
- 无 EF migrations:任何模型变更都意味着删库重建——本设计明确避开。
- 测试:服务边界 + Moq `IKubernetes`(`TestServices.ThrowingFactory`);bUnit 不测 `.mud-*` 内部 DOM。

## Goals / Non-Goals

**Goals:**

- 集群 `Status`/`Version`/`NodeCount`/`LastCheckedAt` 快照按固定间隔自动刷新,无需用户干预。
- 手动刷新与定时刷新互斥且共享同一套探测/审计语义。
- 仅在状态翻转时产生审计记录,避免高频刷屏。
- 用户能从 UI 看出自动同步的存在与间隔。
- 零模型变更、零新依赖、Docker 部署零改动。

**Non-Goals:**

- 不把节点列表、namespace 等更深的数据落库(那是另一档模型变更)。
- 不做配置的管理界面之外的修改通道(如 API 直改)、不做按用户维度的个性化设置。
- 不做分布式调度、错过补偿(misfire catch-up)、抖动(jitter)。
- 不在前端推送"后台正在同步"的实时状态(列表页提示为静态文案)。
- 不为 `AppSetting` 引入完整 migrations:已存在的库用启动时 `CREATE TABLE IF NOT EXISTS` shim 补表(见 D8),保留 `EnsureCreated` 现状。

## Decisions

### D1: `BackgroundService` + `PeriodicTimer`,不引 Quartz/Hangfire

单机单进程、单一固定间隔的需求,`PeriodicTimer`(内置、异步、`CancellationToken` 友好)完全够用;引调度框架是过度设计。备选 `System.Threading.Timer` 需手工处理重入与Dispose,不如 `PeriodicTimer` 干净;备选 `Hangfire` 引新依赖 + 新存储表,被否。

### D2: 配置三层来源——DB 生效值优先,appsettings 作种子默认

`Enabled`(默认 `true`)与 `IntervalMinutes`(默认 `5`,合法范围 1~1440)的实际生效值存 DB(`AppSetting` 键值表);读取时 DB 值优先,无记录或值非法时回退 appsettings,appsettings 再缺失/非法时回退代码内默认。appsettings 因此退化为"首次启动的种子默认值"(Docker 可用 `ClusterSync__IntervalMinutes` 调默认)。最初版本曾是纯静态 appsettings(见 What Changes 历史),运行时可配是后续追加决策:个人信息页 Admin 卡片写入 DB,后台任务每轮读取,保存后下一轮生效。

### D3: 定时器复用 `RefreshAllClustersStatusAsync`,每轮建 scope

后台服务构造函数只注入 `IServiceScopeFactory` + `IConfiguration` + `ILogger`;每轮 `CreateAsyncScope()` 取 `ClusterService` 执行——与所有集群服务 Scoped 的 DI 形状一致,也避免 BackgroundService 单例捕获 Scoped 服务。`IProgress` 传 null(既有豁免)。后台服务暴露一个内部入口 `RunOnceAsync(CancellationToken)`(执行一轮同步),供测试直接驱动,不真等定时器。

启动时机:第一轮在服务启动后立即执行(此时 `EnsureCreated`/`CreateAdminAsync` 已在宿主启动的更早阶段完成),之后 `await timer.WaitForNextTickAsync(ct)` 进入间隔循环。`OperationCanceledException` 正常退出。

### D4: `ClusterService` 内 `SemaphoreSlim(1, 1)` 串行化刷新

信号量放在 `ClusterService` 实例字段,`RefreshAllClustersStatusAsync` 进入时 `await _syncGate.WaitAsync()`/finally `Release()`。手动与定时各自拿到不同的 Scoped `ClusterService` 实例——**因此信号量不能是实例字段,必须是 `static readonly`**,否则互斥失效(这是本设计最容易被写错的一点)。占用时"等待而非跳过":手动刷新进行中定时器到点 → 定时轮次排队,保证新加集群不漏探,代价仅是本轮延迟。

### D5: 状态翻转审计,含来源标注

现状:`RefreshClusterStatusAsync` 先 `repo.GetByIdAsync` 再 probe 后 `UpdateAsync`,旧状态在 probe 前后都在手上。设计:probe 循环处捕获 `previousStatus = entity.Status`(probe 前)与 `entity.Status`(probe 后),不等时 `auditService.LogAsync(AuditCategory.Cluster, AuditAction.Update, $"集群 {name} 状态由 {previousStatus} 变为 {newStatus}({source})")`。`source` 为 `"定时同步"` 或 `"手动刷新"`——`RefreshAllClustersStatusAsync` 增加内部来源参数(单集群手动「刷新状态」不记翻转审计,保持现状)。状态无变化不记。枚举转中文映射(Unknown→未知 / Online→在线 / Offline→离线)集中一处。

### D6: 列表页静态提示行,读取同一配置

`Clusters.razor` 顶部注入 `IConfiguration` 读取 `ClusterSync:Enabled` / `IntervalMinutes`,渲染 mono 风格注释行(贴 `.empty-state` 视觉语言):启用时 `// 每 {N} 分钟自动同步`,禁用时 `// 自动同步已停用`。纯静态文本,不与后台状态联动(Non-Goal)。

### D7: 单元测试边界

- 后台服务:mock `IServiceScopeFactory`→mock scope→mock/真实 `ClusterService`?——**直接测 `RunOnceAsync`**,用真实 SQLite 仓库 + `ThrowingFactory`(K8s 调用即抛)验证"单集群失败不影响整体、状态置离线";用 Moq 验证翻转发审计、不翻转不发。
- 互斥:并发两个 `RefreshAllClustersStatusAsync`,断言串行(信号量 static,可用完成顺序/计数验证)。
- 配置回退:传非法 `IntervalMinutes`/`Enabled` 值,断言回退默认且不抛。
- 运行时配置(追加):`ClusterSyncSettingService` 直测——默认回退、DB 覆盖、非法 DB 值回退、Admin 权限(`FakeHttpContextAccessor` 角色)、1~1440 校验、审计写入。

### D8: `AppSetting` 键值表 + 启动建表 shim

新增 `AppSetting`(Id / Key / Value / UpdatedAt,`Key` 唯一索引)存运行时设置。`EnsureCreated` 对已存在的库**不会补建新表**,直接上实体会让老库查询报错;删库重建会丢集群数据,不可接受。因此在 `Program.cs` 的 `EnsureCreated` 之后执行 `CREATE TABLE IF NOT EXISTS AppSettings(...)` + `CREATE UNIQUE INDEX IF NOT EXISTS`(与模型列定义保持一致,集中一处并注释说明)。全新库走 `EnsureCreated` 天然建表,shim 的 `IF NOT EXISTS` 幂等,两边兼容。备选"把设置写成 `db/` 下的 JSON 文件"被否:绕开 EF/DB 约定,备份与测试路径都变歪。

### D9: `ClusterSyncSettingService` 独立服务

读取(`GetClusterSyncSettingsAsync`,无权限要求)与更新(`UpdateClusterSyncSettingsAsync`)收拢到一个服务:更新时经 `IHttpContextAccessor` 校验 `IsInRole("Admin")`,不满足抛 `PermissionException("仅管理员可修改定时同步设置")`(对齐 `AccountService` 的操作者上下文模式);间隔校验 1~1440,非法抛 `ValidationException`(中文);成功后 upsert 两行并写审计(`AuditCategory.Cluster` + `Update`,描述含新值)。输入走 `Requests/ClusterSyncSettingsUpdateRequest`,输出走 `ViewModels/ClusterSyncSettingsViewModel`。后台任务与列表页提示都消费 `GetClusterSyncSettingsAsync`,单一事实来源。

### D10: 后台循环从 `PeriodicTimer` 改为每轮读设置 + `Task.Delay(动态间隔)`

`PeriodicTimer` 的间隔在构造时固定,无法反映运行时修改。改为 `while` 循环:每轮先 scope 读设置(顺带覆盖"停用后不跑"与"间隔已改"),`Enabled` 为 true 才执行 `RunOnceAsync`,然后 `await Task.Delay(间隔, stoppingToken)`。代价是间隔改短时要等当前延迟结束才感知(最多一个旧间隔),可接受;不再需要构造时注入 `IConfiguration`,服务只剩 `IServiceScopeFactory` + `ILogger`。

## Risks / Trade-offs

- [信号量写成实例字段导致互斥静默失效] → 设计明示 `static readonly`;测试覆盖并发场景。
- [SQLite 写锁偶发 `database is locked`] → 互斥信号量已把"刷新路径"串行化;刷新外的写(如审计)频率极低,SQLite 默认 busy timeout 内消化;不引入 WAL 迁移。
- [探测量大时单轮耗时超过间隔] → `PeriodicTimer` 语义天然串行(一轮未完不 tick 下一轮,等待的是下一个 tick),不会堆积;N 集群 × 每集群两次 API 调用,串行延迟可接受。
- [定时审计刷屏] → 仅翻转时记录,常态为 0 条/轮。
- [BackgroundService 异常导致宿主退出] → 整轮 try/catch + `LogError`,`RunOnceAsync` 内部单集群已有 catch-and-continue;轮外层再兜底。
- [配置被误设为极小间隔(如 1 分钟)] → 探测本身轻量(Version + ListNode),可接受;更新入口限制 1~1440,appsettings/DB 手改超出范围时读取侧钳制回默认。
- [启动 shim 的列定义与 EF 模型漂移] → shim 与模型集中在 Program.cs / ApplicationDbContext 两处,变更模型时必须同步 shim;`IF NOT EXISTS` 保证幂等。
- [个人信息页保存后老间隔的 `Task.Delay` 还没结束,新设置延迟生效] → 文案明示"保存后下一轮同步生效";最多滞后一个旧间隔。

## Migration Plan

数据迁移:已有 SQLite 库由启动 shim 补建 `AppSettings` 表(`IF NOT EXISTS` 幂等,不删库不丢数据);全新库由 `EnsureCreated` 按模型建表。部署即生效:新配置节缺省值合理,旧 `appsettings.json` 不加节也能跑(代码内默认)。回滚 = 移除 `AddHostedService` 注册、设 `ClusterSync:Enabled=false` 或在页面停用定时同步;`AppSettings` 表残留无害。

## Open Questions

无——探索阶段已定:翻转才记审计、列表页加静态提示;运行时配置的三个分叉(配置项含开关、间隔为 1~1440 自由数字、扩展现有 change)由用户拍板。
