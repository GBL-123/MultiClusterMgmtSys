## 1. 配置与后台服务骨架

- [x] 1.1 `appsettings.json` 新增 `ClusterSync` 节(`Enabled: true`,`IntervalMinutes: 5`)
- [x] 1.2 新建 `Services/ClusterSyncBackgroundService.cs`:`BackgroundService`,构造注入 `IServiceScopeFactory` + `IConfiguration` + `ILogger<ClusterSyncBackgroundService>`;解析配置(缺省/非法值回退默认并 `LogWarning`)
- [x] 1.3 实现 `ExecuteAsync`:启动后立即执行一轮 `RunOnceAsync`,随后 `PeriodicTimer` 循环等待下一 tick;`OperationCanceledException` 正常退出;每轮整层 try/catch + `LogError` 兜底
- [x] 1.4 实现公开测试入口 `RunOnceAsync(CancellationToken)`:建 scope 取 `ClusterService`,调用 `RefreshAllClustersStatusAsync(progress: null)`

## 2. ClusterService 改造

- [x] 2.1 增加静态 `SemaphoreSlim(1, 1)` 字段,`RefreshAllClustersStatusAsync` 进入时 `WaitAsync`/finally `Release`(等待不跳过;单集群 `RefreshClusterStatusAsync` 不加锁)
- [x] 2.2 `RefreshAllClustersStatusAsync` 增加来源参数(默认 `手动刷新`,定时同步传 `定时同步`);probe 循环捕获翻转前 `entity.Status`(在 `RefreshClusterStatusAsync` 内 `GetByIdAsync` 之后、`ProbeAsync` 之前)与翻转后状态,不一致时 `AuditService.LogAsync(AuditCategory.Cluster, AuditAction.Update, $"集群 {名称} 状态由 {原} 变为 {新}({来源})")`
- [x] 2.3 新增状态枚举→中文映射(Unknown→未知 / Online→在线 / Offline→离线),集中一处
- [x] 2.4 调用点适配:`Clusters.razor` 手动刷新传/沿用默认来源;`ClusterSyncBackgroundService` 传 `定时同步`

## 3. Program.cs 注册

- [x] 3.1 `builder.Services.AddHostedService<ClusterSyncBackgroundService>()`

## 4. 列表页提示

- [x] 4.1 `Components/Clusters/Pages/Clusters.razor` 注入 `IConfiguration`,读取 `ClusterSync:Enabled` / `IntervalMinutes`,在页面头部渲染 mono 风格提示行:`// 每 {N} 分钟自动同步` 或 `// 自动同步已停用`(贴 `.empty-state` 视觉语言,中英文案遵循 spec)

## 5. 单元测试(MultiClusterMgmtSys.Tests)

- [x] 5.1 `Services/ClusterSyncBackgroundServiceTests`:配置解析回退(缺省节 / 非法 `IntervalMinutes` / `Enabled=false` 时 `RunOnceAsync` 不触发探测)
- [x] 5.2 `Services/ClusterSyncBackgroundServiceTests`:`RunOnceAsync` 用真实 SQLite 仓库 + `ThrowingFactory`(K8s 即抛)验证单集群失败置 `Offline`、其余继续、返回成功数
- [x] 5.3 `Services/ClusterServiceTests`:状态翻转写审计(Online→Offline 记一条,含 `定时同步`/`手动刷新` 来源文案)、状态不变不记审计
- [x] 5.4 `Services/ClusterServiceTests`:并发两个 `RefreshAllClustersStatusAsync` 串行执行(static 信号量互斥)

## 6. 验证

- [x] 6.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 6.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿(含新增用例)
- [x] 6.3 启动应用,验证列表页提示文案与 `LastCheckedAt` 随定时自动更新(可临时把间隔设为 1 分钟观察)

## 7. 运行时配置存储(追加)

- [x] 7.1 新建 `Data/Entities/AppSetting.cs`(Id/Key/Value/UpdatedAt)+ `ApplicationDbContext` 注册(IsRequired + `Key` 唯一索引);`Program.cs` 在 `EnsureCreated` 后执行 `CREATE TABLE IF NOT EXISTS AppSettings` + 唯一索引 shim(老库不删库)
- [x] 7.2 新建 `Data/Repositories/AppSettingRepository.cs`(`GetByKeysAsync` / `SetAsync` upsert)并注册 Scoped
- [x] 7.3 新建 `Requests/ClusterSyncSettingsUpdateRequest.cs` 与 `ViewModels/ClusterSyncSettingsViewModel.cs`
- [x] 7.4 新建 `Services/ClusterSyncSettingService.cs`:`GetClusterSyncSettingsAsync`(DB 优先 → appsettings → 代码默认,非法值钳制)与 `UpdateClusterSyncSettingsAsync`(Admin 校验抛 `PermissionException`、间隔 1~1440 校验抛 `ValidationException`、upsert、审计 `集群/更新: 定时同步设置…`)并注册 Scoped
- [x] 7.5 `ClusterSyncBackgroundService` 改造:去掉 `IConfiguration`/`LoadConfiguration`,循环每轮 scope 读设置,`Enabled` 判定移入循环,`Task.Delay(间隔, ct)` 动态等待
- [x] 7.6 `Clusters.razor` 提示行改为注入 `ClusterSyncSettingService` 读取生效设置(移除 `IConfiguration` 注入)

## 8. 个人信息页配置卡片(追加)

- [x] 8.1 `Profile.razor` 新增 Admin-only「定时同步设置」卡片(`AuthorizeView Roles="Admin"`):`MudSwitch` 启用开关 + `MudNumericField` 间隔(1~1440)+ 保存按钮(成功 Snackbar,失败走 `ExHandler`);仅 Admin 加载当前设置

## 9. 测试更新(追加)

- [x] 9.1 新建 `Services/ClusterSyncSettingServiceTests`:默认回退(无 DB 行无配置)/ DB 覆盖 / 非法 DB 值回退 / 非法 appsettings 回退 / Admin 保存持久化+审计 / Member 抛权限 / 无身份抛权限 / 间隔越界抛校验
- [x] 9.2 更新 `ClusterSyncBackgroundServiceTests`:删除 `LoadConfiguration_*` 与 `RunOnceAsync_Disabled` 用例(职责移至设置服务),保留 `RunOnceAsync` 探测降级用例并适配
- [x] 9.3 `BunitHost` 注册 `AppSettingRepository` + `ClusterSyncSettingService`;确认 `ProfilePageTests` / `ClustersPageTests` 通过

## 10. 验证(追加)

- [x] 10.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 10.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿
- [x] 10.3 冒烟:页面保存新间隔 → 日志确认下一轮按新间隔执行;老库启动 shim 生效(AppSettings 表补建)
