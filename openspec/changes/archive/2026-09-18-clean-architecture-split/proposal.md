# Clean Architecture 分层重构

## Why

项目所有分层规则目前只靠约定:单一 Web 程序集内 `Components/`、`Services/`、`Data/` 全凭命名空间纪律维持依赖方向,编译器不提供任何约束。越界已经真实发生:`Components/Common/RedirectManager` 直接消费 Identity 持久化类型(`UserManager<ApplicationUser>`)、数据层仓库依赖应用输入 DTO(`AuditLogRepository` 用 `Requests/`)、服务与 UI 共享 k8s SDK 类型。对以 AGENTS.md + AI agent 协作的仓库而言,约定会随每次变更缓慢腐化,把边界交给自动化门禁是最经济的防腐手段。

## What Changes

- **BREAKING**(仅对内部代码组织与程序集名,对外行为零变化):单程序集拆为 4 个项目——`MultiClusterMgmtSys.Domain`(实体/枚举/业务异常,零依赖)、`MultiClusterMgmtSys.Application`(用例服务/Requests/ViewModels/Models/端口)、`MultiClusterMgmtSys.Infrastructure`(EF/SQLite/Identity/k8s 客户端缓存与配置/模板读取/后台同步)、`MultiClusterMgmtSys.Web`(现有宿主改名并保留全部 UI:`Program.cs`、`Components/`、`wwwroot` 同项目)。
- **组件隔离由命名空间级架构测试保证**:不引入独立 UI RCL(理由见 design D1:编译器只能拦 Infrastructure 一类越界,实体/端口/k8s 类型本就拦不住);`MultiClusterMgmtSys.Web.Components.*` 命名空间不得接触 Infrastructure/EF/`Domain.Entities`/仓库端口/k8s,由 NetArchTest 规则随 `dotnet test` 与 `./coverage.ps1` 拦截。
- **Application 务实边界**:允许引用 k8s/Identity/Http 框架包,禁止引用 EF Core/DbContext/仓库实现/Infrastructure/Web。`ApplicationUser` 与 `AccountService`/`AuthService` 留 Application,UI 现有注入面零签名改动。
- **端口提取**:`IClusterRepository`、`IGroupRepository`、`IAuditLogRepository`、`IAppSettingRepository` 移入 Application/Abstractions(实现在 Infrastructure/Persistence);`IClusterClientCache`、`IYamlTemplateService` 接口在 Application、实现在 Infrastructure。
- **分层 DI**:每层提供自己的注册扩展(`AddApplicationServices()`、`AddInfrastructure(config)`;Domain 无注册),`Program.cs` 只做组合与 Web 专属注册(MudBlazor/认证/UI 辅助服务)。
- **命名与路径**:宿主目录/程序集 `MultiClusterMgmtSys` → `MultiClusterMgmtSys.Web`;组件命名空间 `MultiClusterMgmtSys.Components.*` → `MultiClusterMgmtSys.Web.Components.*`;同步 `slnx`、`Dockerfile`(ENTRYPOINT `MultiClusterMgmtSys.Web.dll`)、compose、`build-image.ps1/.sh`、`TestPaths.RepoWwwRoot`、scoped CSS 包名、README/AGENTS。
- **架构测试**:新增命名空间与程序集级断言(组件越界、Application 不触 EF/Infrastructure、Requests/ViewModels/Models 归位、k8s 客户端仅在 Infrastructure),随测试套件每次执行并给出违规类型与规则名。
- **工具链与文档**:`coverage.ps1` 门禁改为 Domain+Application+Infrastructure+Web 四程序集合并行覆盖率;AGENTS.md / README.md 全量同步;受影响 spec 文本同步。
- **迁移方式**:一个 change 内分阶段任务,每阶段结束 `dotnet build` + `dotnet test` 全绿(纯搬移,行为零变化;727 测试为回归底线)。

## Capabilities

### New Capabilities
- `architecture-layering`: 四项目分层与依赖方向契约——程序集清单、各层可引用范围、端口与实现归位、Web 组件命名空间隔离、架构测试断言规则。

### Modified Capabilities
- `service-contracts`: 输入/输出/操作者/框架类型规则不变,目录引用从 `Services/`、`Data/Repositories/`、`Requests/`、`ViewModels/`、`Models/` 改为分层项目路径(`Application/`、`Infrastructure/`)。
- `code-style`: XML 文档与成员风格的适用范围从"主项目单程序集"改为四个生产项目(含 `MultiClusterMgmtSys.Web`);路径引用同步。
- `cluster-query-layering`: `ClusterPageQuery` 与仓库的位置迁移(`Application/Models`、`Infrastructure/Persistence`),反向依赖断言语义改为由项目引用保证(Infrastructure 不引用 Web);哨兵语义不变。
- `unit-testing`: 测试项目引用四个生产项目、测试目录镜像四层、覆盖率口径改为四程序集合并、排除清单路径同步。
- `coverage-report-script`: 门禁从"主程序集 `MultiClusterMgmtSys`"改为四程序集(Domain/Application/Infrastructure/Web)合并行覆盖率,其余脚本契约不变。
- `exception-handling`: 业务异常命名空间从 `MultiClusterMgmtSys.Common.Exceptions` 迁移到 `MultiClusterMgmtSys.Domain.Exceptions`(K8s 翻译器随调用归位);异常行为契约不变。

## Impact

- **代码**:主项目约 150 个 `.cs` + 124 个 `.razor` 的命名空间/`using` 重写;宿主目录与程序集改名(UI 文件原地保留);新增 3 个 csproj;4 个仓库接口;`Program.cs` 改为分层注册扩展。
- **测试**:单一 `MultiClusterMgmtSys.Tests` 项目保留,引用四个生产项目;84 个测试文件的 `using`/命名空间同步;新增架构测试;`ServiceHarness`/`BunitServiceExtensions` 适配接口注册。
- **工具链**:`MultiClusterMgmtSys.slnx`、`coverage.ps1`、`MultiClusterMgmtSys.Web/Dockerfile`(COPY/ENTRYPOINT)、`docker-compose*.yml`、`build-image.ps1/.sh`、`TestPaths.RepoWwwRoot`、scoped CSS 包名。
- **文档/规格**:AGENTS.md(Stack/Commands/Testing/Architecture/覆盖/Folder gotchas)与 README.md 目录结构;本 change 的 6 份 spec delta + 1 份新增随归档合并。
- **范围外**:不改变任何用户可见行为;不保留独立 UI RCL;不做严格端口化(不把 k8s SDK/Identity 抽象成端口);不引入 `src/` 目录布局;不逐份清理历史 feature spec 中已存在或即将过时的路径提及(nodes-page/configmaps-page 等,另起 spec 卫生变更);数据库文件名 `MultiClusterMgmtSys.db` 与认证 Cookie 名不变。
