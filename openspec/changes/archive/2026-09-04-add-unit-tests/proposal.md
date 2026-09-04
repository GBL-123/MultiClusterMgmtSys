## Why

项目无任何测试(AGENTS.md 明言 "No test project"),而大量关键契约无人守护:异常映射(404→NotFound/409→Conflict)、查询 sentinel(GroupId 0→NULL、版本过滤器)、密码与备注校验、权限门控、组件对 MudBlazor 的绑定方式(此前 `MudDateRangePicker` 的 `@bind-Value` 静默失效 bug 靠人工排障才发现)。引入测试体系,把这些契约变成可回归的断言。

## What Changes

- 新增测试项目 `MultiClusterMgmtSys.Tests/`(net10.0,xUnit + Moq + EF Sqlite 内存库 + bUnit),挂入 `MultiClusterMgmtSys.slnx`;`dotnet test` 可运行。
- **K8s 客户端工厂改造**(行为零变化):`ConfigMapService` / `ClusterNodeService` / `ClusterService` 的 `new Kubernetes(config)` 改为注入 `Func<KubernetesClientConfiguration, IKubernetes>`,Program.cs 注册一次——使 K8s 异常翻译路径可端到端测试。
- 后端测试**以服务为边界**(仓库经服务覆盖):`K8sExceptionMapper` 映射矩阵、`ClusterService` 查询契约(分组/版本/日期/sentinel)、`AccountService` 密码校验、`ClusterNodeService` 备注校验、`GroupService` sentinel 拒绝、`AuditService` 最近操作、`ConfigMapService` 集群不存在路径、`ExceptionPresenter` 路由。
- bUnit **接线契约**测试(只断言"组件如何使用 MudBlazor",不碰 `.mud-*` 内部 DOM):`ClusterFilterBar` 的 `DateRangePicker` 回写、集群页 Admin/Member 门控、状态徽章三态、最近操作卡空态。
- MudBlazor 分析器告警转构建错误(`MUD0002` 等)——组件 API 误用从编译期拦截(此前 `@bind-Value` 非法属性即属此类)。
- AGENTS.md:Commands 新增 `dotnet test`;新增测试约定(目录镜像、服务边界、bUnit 口径)。

## Capabilities

### New Capabilities

- `unit-testing`: 测试项目结构、服务边界测试口径、K8s 工厂可注入、bUnit 接线契约口径、分析器告警转错误、AGENTS.md 文档化。

### Modified Capabilities

<!-- 纯新增测试,不修改既有功能规格 -->

## Impact

- 新增 `MultiClusterMgmtSys.Tests/`(项目 + 目录镜像 + TestInfrastructure)
- `MultiClusterMgmtSys.slnx` 挂载测试项目
- `Program.cs` + 3 个服务:注入 K8s 客户端工厂(行为不变)
- `MultiClusterMgmtSys.csproj`:`WarningsAsErrors` 配置(MudBlazor 分析器)
- AGENTS.md:测试命令与约定
- 无数据库/API/行为变更