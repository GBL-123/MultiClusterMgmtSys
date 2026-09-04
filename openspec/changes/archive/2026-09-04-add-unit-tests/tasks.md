## 1. 测试项目脚手架

- [x] 1.1 新建 `MultiClusterMgmtSys.Tests/MultiClusterMgmtSys.Tests.csproj`(net10.0,xunit + Microsoft.NET.Test.Sdk + xunit.runner.visualstudio + Moq + Microsoft.EntityFrameworkCore.Sqlite + bunit),ProjectReference 主项目
- [x] 1.2 `dotnet sln MultiClusterMgmtSys.slnx add MultiClusterMgmtSys.Tests`
- [x] 1.3 TestInfrastructure:`SqliteDbFactory`(每测新开 :memory: + EnsureCreated)、`TestData`(集群/分组/审计造数)、`SeedUserAsync`(真实 UserManager 建用户)
- [x] 1.4 空冒烟测试:`dotnet test` 跑通 1 个占位 Fact

## 2. K8s 客户端工厂改造

- [x] 2.1 `ConfigMapService` / `ClusterNodeService` / `ClusterService` 构造函数加 `Func<KubernetesClientConfiguration, IKubernetes> clientFactory`,`new Kubernetes(config)` 全量替换
- [x] 2.2 Program.cs 注册 `AddSingleton<Func<_,_>>(cfg => new Kubernetes(cfg))`
- [x] 2.3 `dotnet build` 0 错误(行为不变)

## 3. 逻辑层测试

- [x] 3.1 `K8sExceptionMapperTests`:404/409/403/401/400(含 Status.Message 回退)/5xx 原样/超时→ClusterUnreachable 全矩阵
- [x] 3.2 `ExceptionPresenterTests`(Moq ISnackbar + NullLogger):Conflict→Warning、其他业务异常→Error、非业务→通用文案且 LogError

## 4. 服务层测试(SQLite 内存库)

- [x] 4.1 `ClusterServiceTests`:查询契约(GroupId 0→未分组、版本 OnlyNull、日期区间、排序/分页)、GetClusterDetailAsync 不存在→null、UpdateClusterEndpointsAsync/DeleteClusterAsync 不存在→NotFoundException
- [x] 4.2 `ClusterNodeServiceTests`:备注 64 字符→ValidationException、备注合并(增/改/删)、集群不存在→NotFoundException、mock 工厂 404/超时翻译
- [x] 4.3 `ConfigMapServiceTests`:各方法集群不存在→NotFoundException;mock 工厂验证 List/Get/Delete/Update/Create 的 404→NotFound、409→Conflict、400→Validation 端到端
- [x] 4.4 `AuditServiceTests`:GetRecentAsync 本人过滤/倒序/5 条上限
- [x] 4.5 `GroupServiceTests`:分组 CRUD、重命名不存在→NotFoundException、MoveClustersToGroupAsync sentinel 0→ValidationException
- [x] 4.6 `AccountServiceTests`(真实 Identity + SQLite):新旧密码相同→ValidationException、成功改密(密码已更新)、错误当前密码→PasswordMismatch 消息
- [x] 4.7 `dotnet test` 全绿

## 5. bUnit 接线契约测试

- [x] 5.1 TestInfrastructure:`TestHarness.razor`(MudThemeProvider/MudPopoverProvider/MudDialogProvider/MudSnackbarProvider + auth)+ `AuthTestHelper`(Admin/Member)
- [x] 5.2 `ClusterFilterBarTests`:触发 MudDateRangePicker 的 `DateRangeChanged` → 断言 Query.DateRange 回写
- [x] 5.3 集群页门控(Member 无「刷新所有集群/添加集群」,Admin 有)
- [x] 5.4 状态徽章三态映射(ClusterTable:Online→online、Offline→offline、Unknown→unknown)与最近操作卡空态(Profile)
- [x] 5.5 `dotnet test` 全绿

## 6. 分析器告警转错误

- [x] 6.1 处理存量 MudBlazor 分析器告警(`Title=` 等约 10 处 → aria-label/Tooltip 或合法属性),构建清零
- [x] 6.2 主项目 csproj 配置 `WarningsAsErrors`(MudBlazor 分析器告警),`dotnet build` 0 错误
- [x] 6.3 全量 `dotnet test` 通过

## 7. 文档与验收

- [x] 7.1 AGENTS.md:Commands 增 `dotnet test`;新增 Testing 约定(服务边界/bUnit 口径/TestInfrastructure)
- [x] 7.2 最终:`dotnet build` 0 错误 + `dotnet test` 全绿