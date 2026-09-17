# clean-architecture-split Design

## Context

见 `proposal.md` — Why。设计相关的现状事实与约束:

- 单一 Web 程序集:143 个 `.cs` + 124 个 `.razor`;测试 84 文件 / 727 测试;29 份 spec。
- Blazor interactive server 下**宿主即组合根**:`Program.cs` 必须引用 Infrastructure 注册 DI;因此只要 UI 与组合根同程序集,"编译器强制 UI 隔离"就不成立,除非 UI 再拆 RCL——而 RCL 也只能拦住 Infrastructure 一类越界(见 D1 实测矩阵)。
- 现有服务已直接调用 k8s SDK(`client.CoreV1.*` + `V1*` 类型),异常翻译 `K8sExceptionMapper` 由服务直接调用;约 8~10 个测试文件以 `*WithHttpMessagesAsync` mock k8s SDK。
- `coverage.ps1` 硬编码 `Summary.txt` 中 `MultiClusterMgmtSys` 单行做门禁;`Dockerfile` 以 `MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 为 restore 入口并在 ENTRYPOINT 启动同名 dll;测试用 `TestPaths.RepoWwwRoot` 相对定位宿主 `wwwroot`。
- 工具链与约定(AGENTS.md、specs)大量以物理路径、程序集名与命名空间为锚点,改名/移动必须同步更新。
- 已存在的 spec 漂移(与本 change 无关,不修):`cluster-query-layering` 中 typed `VersionFilter` 与实现里的 `VersionFilterSentinel` 字符串哨兵不一致;`nodes-page`/`configmaps-page` 残留 `Features.*`/`Components.*` 命名空间声明已过时。

## Goals / Non-Goals

**Goals:**

- 主要依赖方向由编译器强制(Domain 零依赖、Application 不触 EF/Infrastructure、Infrastructure 不依赖 Web)。
- Web 内组件命名空间的越界(Infrastructure / EF / 实体 / 仓库端口 / k8s)由随测试套件执行的架构测试拦截。
- 类文件落点唯一且可判定(Domain/Application/Infrastructure/Web),消除"该放哪"的歧义;宿主改名为 `MultiClusterMgmtSys.Web`。
- 保持单部署单元、单测试项目、行为零变化;每个迁移阶段 `build` + `test` 全绿。
- 覆盖率门禁、Docker 构建、bUnit 测试栈在拆分与改名后继续工作。

**Non-Goals:**

- 严格端口化:不为 k8s SDK/Identity/HttpContext 建立端口层(Application 允许引用这些包)。
- 不保留独立 UI RCL(不引入 `AdditionalAssemblies` 接线与 RCL 静态资源处理)。
- 不引入 `src/` 布局;不改镜像名、容器名、数据库文件名、认证 Cookie 名。
- 不改变任何服务公开签名语义、不改路由、不改 UI 行为。
- 不逐份清理历史 feature spec 的过时路径提及(另起 spec 卫生变更)。

## Decisions

### D1: 四项目与依赖矩阵(含"为什么不要 RCL")

```
Domain <-- Application <-- Infrastructure <-- Web
              ^                                |
              +--------------------------------+
        (Web 只把 Application 当契约消费;Infrastructure 仅为组合根注册)
```

| 项目 | 引用 | 约束 |
|---|---|---|
| `MultiClusterMgmtSys.Domain` | 无(含包) | 实体/枚举/业务异常 |
| `MultiClusterMgmtSys.Application` | Domain;允许 k8s/Identity/HttpContext/Logging 包 | 禁止 EF Core/DbContext/Infrastructure/Web |
| `MultiClusterMgmtSys.Infrastructure` | Application + Domain;EF/Sqlite/Identity/k8s | 禁止 Web |
| `MultiClusterMgmtSys.Web` | Application + Infrastructure(组合根) | 组件命名空间受架构测试约束(D2/R3) |

组件越界的拦截矩阵(实测对照):

| 组件里的越界写法 | 五项目+UI RCL | 四项目 Web + 架构测试 |
|---|---|---|
| `@inject ApplicationDbContext` | 编译失败 | 测试失败 |
| `@inject IClusterRepository` | 编译通过(端口在 Application) | 测试失败 |
| 使用 `Domain.Entities` 实体 | 编译通过(传递可见) | 测试失败 |
| 使用 `k8s` 类型(V1Node) | 编译通过(包传递) | 测试失败 |
| `new ClusterClientCache` | 编译失败 | 测试失败 |

RCL 只拦得住 2/5,另 3 类本来就要架构测试兜底;合并方案以"错误晚一步(测试时而非编译时)"换取少一个项目、无 `AdditionalAssemblies`、无 RCL 静态资源坑,且规则面更全。**替代方案**:五项目 + UI RCL(前版设计)——否决,理由如上。

### D2: 宿主改名、命名空间与 Web 布局(N1)

- 目录/程序集:`MultiClusterMgmtSys/` → `MultiClusterMgmtSys.Web/`,csproj 同名;`Components/`、`wwwroot/`、`Endpoints/`(由 `Services/Identity/IdentityComponentsEndpointRouteBuilderExtensions.cs` 迁入)、`Program.cs`、`appsettings.json` 同项目。
- 命名空间:默认根 `MultiClusterMgmtSys.Web`,组件命名空间变为 `MultiClusterMgmtSys.Web.Components.*`(N1 对齐);涉及的显式引用约 85 个 razor + 4 个 `.cs` 命名空间声明 + `_Imports.razor`/`Routes.razor`/`Program.cs` + 约 40 个测试文件;隐式命名空间自动派生。
- `Routes.razor` 的 `AppAssembly="typeof(Program).Assembly"`、`MapRazorComponents<App>()`、`@Assets` 全部保持单程序集语义,**无需 `AdditionalAssemblies`**;`ReconnectModal.razor.js` 的 `@Assets` 相对路径不变。
- 改名波及清单(机械):`slnx`、`Dockerfile`(COPY/WORKDIR/ENTRYPOINT `dotnet MultiClusterMgmtSys.Web.dll`)、`docker-compose*.yml`(dockerfile 路径)、`build-image.ps1/.sh`(Dockerfile 路径)、`TestPaths.RepoWwwRoot`、`App.razor` 的 `MultiClusterMgmtSys.Web.styles.css`、README、AGENTS。
- **不随改名的**:镜像/容器名 `multiclustermgmtsys`、数据库文件 `MultiClusterMgmtSys.db`、认证 Cookie `MultiClusterMgmtSys.Auth`、Docker 挂载路径语义。
- **替代方案**:`<RootNamespace>MultiClusterMgmtSys</RootNamespace>` 保持组件命名空间零改动——全仓唯一"程序集名与命名空间根不一致"的例外,且组件文件本就要为后端 `using` 改动,边际成本近零,否决。

### D3: Application 的务实边界与 Identity 归属

`ApplicationUser`、`AccountService`、`AuthService` 留 Application(引用 Identity 包),`IdentityRevalidatingAuthenticationStateProvider` 归 Infrastructure/Identity。理由:`AccountService` 多个公开方法返回 `IdentityResult`(已被 `service-contracts` 豁免),UI 8 处 `@inject` 与 `RedirectManager.RedirectToInvalidUser(UserManager<ApplicationUser>)` 保持不变;端口化到 Infrastructure 时接口仍要暴露 Identity 类型(除非改契约),churn 大收益低。**替代方案**:Identity 端口化——未来若引入第二宿主再评估,本次否决。

### D4: 端口清单与分层 DI 注册

端口(全部 `Application/Abstractions`):`IClusterRepository`、`IGroupRepository`、`IAuditLogRepository`、`IAppSettingRepository`、`IClusterClientCache`(迁移位置)、`IYamlTemplateService`(迁移位置)。实现:`Infrastructure/Persistence`(仓库)、`Infrastructure/Kubernetes`(缓存与配置)、`Infrastructure/Templates`(YAML 模板读取)。

每层提供自己的注册扩展,Domain 无注册:

```csharp
// Application/ApplicationServiceCollectionExtensions.cs
public static IServiceCollection AddApplicationServices(this IServiceCollection services)

// Infrastructure/InfrastructureServiceCollectionExtensions.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
//   EF/Sqlite(含连接字符串相对路径锚定)、Identity、k8s 工厂 + 缓存、模板、AddHostedService<ClusterSyncBackgroundService>

// Web/Program.cs —— 只做组合
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);
// Web 专属:MudBlazor、Razor Components、认证 Cookie、ExceptionPresenter、ClusterSelectionState
```

k8s 客户端工厂注册从 Program.cs 移入 `AddInfrastructure()`。规则:新增服务注册进所属层扩展,`Program.cs` 只做组合(写入 AGENTS)。

### D5: 文件归属表

```
Domain/
  Entities/   ClusterInfo ClusterGroup ClusterEndpoint NodeIpRemark AuditLog AppSetting
  Enums/      ClusterStatus ConnectionType ClusterEndpointKind AuditCategory AuditAction
  Exceptions/ BusinessException NotFound Conflict Validation Permission ClusterUnreachable

Application/
  Services/     全部用例服务(含 AccountService/AuthService、ClusterSyncSource)
  Abstractions/ 4 个仓库接口 + IClusterClientCache + IYamlTemplateService
  Requests/ ViewModels/(Mappings/) Models/(ClusterPageQuery 含 VersionFilterSentinel)
  Enums/        ClusterSortField WorkloadKind WorkloadRolloutState
  Common/       Time/RelativeTimeFormatter, Exceptions/K8sExceptionMapper

Infrastructure/
  Persistence/ ApplicationDbContext + 4 个仓库实现
  Identity/    IdentityRevalidatingAuthenticationStateProvider
  Kubernetes/  ClusterClientCache, KubernetesClientConfig(internal)
  Templates/   YamlTemplateService
  Sync/        ClusterSyncBackgroundService

Web/ (改名后的宿主,UI 原地保留)
  Program.cs appsettings.json wwwroot/ Properties/ Dockerfile
  Components/ Account/ … Workloads/, App.razor Routes.razor _Imports.razor
  Endpoints/  IdentityComponentsEndpointRouteBuilderExtensions
```

`Common/` 目录消失(实体枚举异常→Domain,查询枚举/时间/映射→Application)。`ClusterSelectionState`/`ExceptionPresenter`/`RedirectManager`/`ThemeManager` 随 `Components/Common` 原地留 Web。

### D6: Templates / Sync / 异常翻译的归位理由

- `YamlTemplateService` 是**文件系统适配器**(`IWebHostEnvironment.WebRootPath` + `File` I/O),归 Infrastructure/Templates;模板文件保留在 Web 的 `wwwroot/templates`(Docker volume 覆盖语义不变);端口在 Application 供组件消费。
- `ClusterSyncBackgroundService` 是**时间驱动入口点/机制**(零 I/O,经 scope 调 Application 用例),归 Infrastructure/Sync:Web 保持组合根,且未来可脱离 Web 宿主复用。
- `K8sExceptionMapper` 依赖 k8s 类型且被 Application 服务直接调用,必须与调用同层 → `Application/Common/Exceptions`;仅**业务异常层次**在 `Domain/Exceptions`。
- **替代方案**:两者都放 Web("入口点最外层")——Web 不再是组合根职责分离,测试需引用宿主类型;独立 Worker 项目同前版否决。

### D7: 改名/命名空间机械修正点(已核实)

| 位置 | 处理 |
|---|---|
| 全仓 `MultiClusterMgmtSys.Services/Data/Common/ViewModels/Requests/Models` using | 改为 `MultiClusterMgmtSys.Application.*` / `Infrastructure.*` / `Domain.*` |
| 组件显式引用 `MultiClusterMgmtSys.Components.*`(~85 razor + 4 `.cs` + 测试) | 改为 `MultiClusterMgmtSys.Web.Components.*` |
| `App.razor` scoped CSS 包名字符串 | `MultiClusterMgmtSys.styles.css` → `MultiClusterMgmtSys.Web.styles.css` |
| `Dockerfile` | COPY 新 csproj、WORKDIR、ENTRYPOINT `dotnet MultiClusterMgmtSys.Web.dll` |
| `docker-compose*.yml` / `build-image.ps1/.sh` | Dockerfile 路径改 `MultiClusterMgmtSys.Web/` |
| `TestPaths.RepoWwwRoot` | 路径段改 `MultiClusterMgmtSys.Web` |
| 本地运行产物 `MultiClusterMgmtSys/db`、`logs` | gitignored;改名后按需重建(开发态,无迁移) |
| `Routes.razor` / `MapRazorComponents<App>()` / `ReconnectModal.razor.js` | **不需要改**(单程序集语义不变) |

### D8: 覆盖率口径与脚本

四个生产程序集 `MultiClusterMgmtSys.Domain` / `.Application` / `.Infrastructure` / `.Web` 的 **covered/total 行数跨程序集求和**后计算合并行覆盖率,阈值默认 75;排除 4 个文件(`MultiClusterMgmtSys.Web/Program.cs`、`MultiClusterMgmtSys.Web/Endpoints/IdentityComponentsEndpointRouteBuilderExtensions.cs`、`MultiClusterMgmtSys.Web/Components/App.razor`、`MultiClusterMgmtSys.Web/Components/Routes.razor`);测试程序集不计入。`coverage.ps1` **直接解析 cobertura XML**(`<package>` 的 `name` 定位四个程序集,逐 `<class>` 累加 `<line>` 命中数)——任务 1.1 已核对:`Summary.txt` 程序集行只有百分比、无 Covered/Total 列,不可作为数据源。**替代方案**:逐程序集门禁——Domain(纯 POCO)与 Web(razor 代码)各自达标会变成维护负担,否决。

### D9: 架构测试

测试项目新增 `Architecture/` 与 `NetArchTest.Rules`(测试期依赖),规则:

1. `MultiClusterMgmtSys.Web.Components` 命名空间不依赖 `MultiClusterMgmtSys.Infrastructure`、`Microsoft.EntityFrameworkCore`、`MultiClusterMgmtSys.Domain.Entities`、`MultiClusterMgmtSys.Application.Abstractions`(端口)、`k8s`。
2. `Application` 不依赖 EF Core / `ApplicationDbContext` / `Infrastructure`。
3. `Requests`/`ViewModels`/`Models` 类型归属:按命名后缀抽查所在程序集。
4. `k8s.Kubernetes`(具体客户端类型)仅出现在 Infrastructure(工厂)。
5. 程序集引用兜底:Infrastructure 不引用 Web,Application 不引用 Infrastructure(防未来误加 `ProjectReference`)。

NetArchTest 检测真实类型使用(签名/属性/IL),非 `using` 指令;`Program`/`Endpoints`/DI 注册不在 `Components` 命名空间,组合根职责不受规则约束。**替代方案**:手写反射——方法体内局部使用会漏检,否决。

### D10: 不采用 PrivateAssets 切断 k8s 包传递

Application 的 `KubernetesClient` 包引用不做 `PrivateAssets` 截断:`IClusterClientCache.GetOrCreate` 公共签名返回 `IKubernetes`,切割会造成"成员可见但类型缺失"的怪异编译错误;k8s 在组件中的使用由架构测试覆盖。

### D11: 分阶段迁移(每阶段绿)

任务组 1 spike(仅核对 `Summary.txt` 列布局)→ 2 脚手架(3 个 classlib + slnx + 引用 + 测试项目多引用)→ 3 Domain → 4 Application(+端口+注册扩展)→ 5 Infrastructure → 6 宿主改名与组件命名空间对齐(`MultiClusterMgmtSys.Web` + 工具链路径)→ 7 测试重映射 → 8 工具链/文档/spec 同步 → 9 架构测试 → 10 全量回归。纯搬移不夹带行为改动;回滚 = 逐阶段 revert。

## Risks / Trade-offs

- **[R1] 宿主改名波及工具链路径,易遗漏** → D2/D7 清单 + 全仓 grep 扫描 `MultiClusterMgmtSys/`、`MultiClusterMgmtSys.dll`、`MultiClusterMgmtSys.styles.css` + Docker 构建冒烟。
- **[R2] 命名空间重写面广(全仓约 350 文件)** → 编译器兜底 + 分阶段绿;批量替换后逐阶段 `dotnet build`。
- **[R3] 组件隔离仅剩测试关卡** → 规则面覆盖 D1 矩阵全部 5 类越界;负向验证任务(临时越界确认测试红);规则维护写入 AGENTS。
- **[R4] 合并覆盖率在重构中途可能短暂低于 75%** → 门禁只在阶段 7 最终验证;中间阶段以测试全绿为准。
- **[R5] 覆盖率数据源与解析假设不符** → 已核对并解决:任务 1.1 确认 `Summary.txt` 程序集行只有百分比,改为解析 cobertura XML 行命中数(D8 已更新)。
- **[R6] 既有 spec 漂移被移动放大** → 本 change 只更新承载契约的 6 份 + 新增 1 份;feature spec 路径卫生另案处理。
- **[R7] 本地 db/logs 目录随改名重建** → 开发态数据,删除/重建即可;生产 Docker 挂载路径不变。

## Migration Plan

见 D11 与 `tasks.md`。回滚策略:每个阶段是独立、机械、无数据变更的提交,任阶段失败可 `git revert` 并保持上一阶段绿。生产部署仅 Dockerfile 路径与入口 dll 名变化,镜像/端口/挂载/数据库文件名不变。

## Open Questions

- 无(阻塞性未知已在 R5 的验证任务中前置)。
