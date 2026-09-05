## Why

集群状态目前只在用户手动点击「刷新所有集群」或单个「刷新状态」时才更新,页面关闭期间状态会逐渐失真(`LastCheckedAt` 越来越旧,`Online/Offline` 可能早已不对)。需要一个可配置间隔的后台定时任务,周期性对全部集群执行与手动刷新相同的探测,让集群状态、版本、节点数快照保持新鲜,无需用户干预。

## What Changes

- 新增 `ClusterSyncBackgroundService`(`BackgroundService` + `PeriodicTimer`):按配置间隔循环调用现有 `ClusterService.RefreshAllClustersStatusAsync()`,复用既有 `ProbeAsync` 语义(逐集群 catch-and-continue,失败置 Offline,不向用户弹错)。
- 新增配置节 `ClusterSync`:`Enabled`(bool,默认 true)、`IntervalMinutes`(int,默认 5)。静态 appsettings 配置作为**首次默认值**(种子);运行时实际生效值存 DB,可被管理员覆盖。
- `ClusterService.RefreshAllClustersStatusAsync` 内部加 `SemaphoreSlim(1, 1)` 串行化,使手动刷新与定时刷新互斥(定时器到点时若手动刷新占用,则等待而非跳过)。
- 审计:探测过程中**仅当集群 `Status` 发生翻转**(Online↔Offline↔Unknown 边界)时记一条审计日志(描述如 `集群 {name} 状态由 Online 变为 Offline(定时同步)`);状态无变化不记,避免刷屏。
- UI:集群列表页(Clusters.razor)增加一行自动同步提示(如 mono 风格的 `// 每 {N} 分钟自动同步`,禁用时显示 `// 自动同步已停用`),让用户理解 `LastCheckedAt` 为何自行变化。
- (运行时配置,追加)个人信息页新增 **Admin-only「定时同步设置」卡片**:可修改启用开关与间隔(1~1440 分钟自由数字),保存经服务校验后写入 DB 并记审计;后台任务每轮从 DB 读取最新设置,保存后下一轮生效;`Clusters.razor` 提示行同步读取 DB 生效值。
- (存储,追加)新增 `AppSetting` 键值实体与唯一索引;因 `EnsureCreated` 不给已有库补建新表,`Program.cs` 启动时执行 `CREATE TABLE IF NOT EXISTS` 升级 shim,**不删库**。
- 数据模型:新增 `AppSetting` 表(键值设置);既有表结构不变。

## Capabilities

### New Capabilities

- `cluster-scheduled-sync`: 定时刷新集群状态的后台任务能力——调度周期与开关配置、与手动刷新的互斥语义、状态翻转审计、失败静默降级。

### Modified Capabilities

- `cluster-scheduled-sync`(本 change 自身,追加迭代): 配置从"仅静态 appsettings"升级为"DB 运行时可配 + appsettings 种子默认",后台任务从固定 `PeriodicTimer` 改为每轮读设置 + 动态延迟;列表页提示与个人信息页 Admin 配置卡片读取同一份生效设置。

## Impact

- `MultiClusterMgmtSys/Program.cs`:注册 `AddHostedService<ClusterSyncBackgroundService>()`(一行)+ `AppSettingRepository` / `ClusterSyncSettingService` + `AppSettings` 建表 shim。
- 新文件 `MultiClusterMgmtSys/Services/ClusterSyncBackgroundService.cs`(:`BackgroundService`,经 `IServiceScopeFactory` 建scope 取服务;每轮读设置,动态间隔)。
- 新文件 `MultiClusterMgmtSys/Services/ClusterSyncSettingService.cs`(设置读取/更新:DB 优先、appsettings 兜底、Admin 权限校验、1~1440 校验、审计)。
- 新文件 `MultiClusterMgmtSys/Data/Entities/AppSetting.cs` + `Data/Repositories/AppSettingRepository.cs`。
- `MultiClusterMgmtSys/Services/ClusterService.cs`:`RefreshAllClustersStatusAsync` 加互斥信号量;probe 循环增加状态翻转对比与审计写入。
- `MultiClusterMgmtSys/appsettings.json`:新增 `ClusterSync` 配置节(种子默认值)。
- `MultiClusterMgmtSys/Components/Clusters/Pages/Clusters.razor`:提示行,读取生效设置(DB 优先)。
- `MultiClusterMgmtSys/Components/Profile/Pages/Profile.razor`:Admin-only 定时同步设置卡片。
- `MultiClusterMgmtSys.Tests/`:后台服务、设置服务(权限/校验/审计/回退)、串行化的单元测试;调度逻辑用注入的委托驱动,不真等定时器。
- 运维:已存在的 SQLite 库通过启动 shim 补建 `AppSettings` 表,无数据丢失;如需调整默认值,加环境变量 `ClusterSync__IntervalMinutes`。
