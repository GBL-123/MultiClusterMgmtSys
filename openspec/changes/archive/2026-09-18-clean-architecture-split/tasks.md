# clean-architecture-split Tasks

## 1. 前置验证(spike)

- [x] 1.1 跑一次现有覆盖率流程,打印 `coverage/report/Summary.txt`,核对程序集行的列布局(程序集名 / Covered / Total 行),确定四程序集合并算法可解析;验证:基线 727 测试全绿 + 行覆盖 78.1%;核对结论:`Summary.txt` 程序集行只有百分比(无 Covered/Total 列),合并算法改为解析 cobertura XML `<package>`/`<class>`/`<line>` 命中数(design D8 已同步)

## 2. 脚手架

- [x] 2.1 创建三个 classlib(`MultiClusterMgmtSys.Domain`/`.Application`/`.Infrastructure`),`dotnet sln MultiClusterMgmtSys.slnx add` 挂载,按 D1 矩阵添加 ProjectReference(Domain 零引用;Application→Domain;Infrastructure→Application+Domain;Web→三个新项目);验证:`dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 2.2 新项目开启 `Nullable`/`ImplicitUsings`/`GenerateDocumentationFile`;Infrastructure 加 `InternalsVisibleTo MultiClusterMgmtSys.Tests`;验证:`dotnet build` 0 错误
- [x] 2.3 测试项目改为引用四个生产项目(替换单一 ProjectReference);验证:`dotnet test MultiClusterMgmtSys.Tests` 现有 727 测试全绿(尚未搬文件,仅多引用)
- [x] 2.4 Dockerfile restore 分层(按当前目录名 COPY 四个 csproj 后 restore 宿主);验证:docker daemon 未运行,已记录并留待任务 10.4;Dockerfile 已加入三个新 csproj 的 COPY 行

## 3. Domain 抽取

- [x] 3.1 移动 `Data/Entities/`(6 实体)、领域枚举(`ClusterStatus`/`ConnectionType`/`ClusterEndpointKind`/`AuditCategory`/`AuditAction`)、`Common/Exceptions/` 业务异常层次到 `Domain/Entities|Enums|Exceptions`,命名空间改 `MultiClusterMgmtSys.Domain.*`,全仓 using 同步;验证:`dotnet build` 0 错误 + `dotnet test` 全绿(727)
- [x] 3.2 核对 Domain 项目无任何项目/包引用(仅 BCL 类型);验证:csproj 无 ProjectReference/PackageReference,构建 0 错误

## 4. Application 抽取

- [x] 4.1 移动 `Services/`(全部用例服务,含 Account/Auth/ClusterSyncSource;**不含** `Identity/IdentityComponentsEndpointRouteBuilderExtensions.cs`)、`Requests/`、`ViewModels/`(含 Mappings)、`Models/`、剩余枚举(`ClusterSortField`/`WorkloadKind`/`WorkloadRolloutState` → `Application/Enums`)、`Common/Time`、`Common/Exceptions/K8sExceptionMapper` 到 Application,命名空间改 `MultiClusterMgmtSys.Application.*`;验证:`dotnet build` 0 错误 + `dotnet test` 全绿(727;另随 D3 将 `ApplicationUser` 移入 `Application/Identity`)
- [x] 4.2 在 `Application/Abstractions` 新建 `IClusterRepository`/`IGroupRepository`/`IAuditLogRepository`/`IAppSettingRepository`(方法签名从现有实现提取,不变),迁移 `IClusterClientCache` 与 `IYamlTemplateService` 接口位置;用例服务构造依赖改接口类型;验证:`dotnet build` 0 错误 + 727 全绿;实际增补第 5 个端口 `IAccountQueryRepository`(AccountService 的 EF 分页/批量查询下沉,否则 Application 将引用 EF Core,违反本 change 的 `architecture-layering` 契约)
- [x] 4.3 Application 提供 `AddApplicationServices()` 注册扩展;宿主 Program.cs 调用;Application csproj 配齐 KubernetesClient、Identity、Logging、Http 等包/FrameworkReference(不引 EF Core);验证:`dotnet build` 0 错误 + `dotnet test` 全绿(727)
- [x] 4.4 核对 `ApplicationUser`/`AccountService`/`AuthService`/`RedirectManager` 的 Identity 使用面未变(零签名改动;`AccountService` 仅构造函数数据访问参数由 `ApplicationDbContext` 改为 `IAccountQueryRepository`);验证:`dotnet build` 0 错误 + bUnit 全绿

## 5. Infrastructure 抽取

- [x] 5.1 移动 `ApplicationDbContext` 与四个仓库实现到 `Infrastructure/Persistence` 并实现 Application 接口;移动 `ClusterClientCache`+`KubernetesClientConfig` 到 `Kubernetes`、`YamlTemplateService` 到 `Templates`、`ClusterSyncBackgroundService` 到 `Sync`、`IdentityRevalidatingAuthenticationStateProvider` 到 `Identity`,命名空间改 `MultiClusterMgmtSys.Infrastructure.*`;验证:`dotnet build` 0 错误(另:AccountService 的 API Server 回填改经 `IClusterClientCache.ResolveApiServer` 端口,避免 Application 触达 infra 内部)
- [x] 5.2 Infrastructure 提供 `AddInfrastructure()`(EF/Sqlite 含连接字符串锚定、Identity、k8s 工厂与缓存、模板、`AddHostedService<ClusterSyncBackgroundService>`);宿主 Program.cs 删除对应注册与 `using k8s`,改为调用 `AddApplicationServices()`/`AddInfrastructure(configuration)`;验证:`dotnet build` 0 错误 + `dotnet test` 全绿(727)
- [x] 5.3 `ServiceHarness` 改为 `new` 实现类并按其接口注册;`BunitServiceExtensions` 各 `Add*Stack` 注册接口类型;验证:`dotnet test` 全绿(727)

## 6. 宿主改名与组件命名空间对齐

- [x] 6.1 宿主目录/程序集 `MultiClusterMgmtSys/` → `MultiClusterMgmtSys.Web/`,原子完成:csproj 改名、slnx、`Dockerfile`(COPY/WORKDIR/ENTRYPOINT `dotnet MultiClusterMgmtSys.Web.dll`)、`docker-compose*.yml`、`build-image.ps1/.sh`、`TestPaths.RepoWwwRoot`、`App.razor` 样式包名 `MultiClusterMgmtSys.Web.styles.css`,并把全仓(含测试)的 `MultiClusterMgmtSys.Components.*` 引用改为 `MultiClusterMgmtSys.Web.Components.*` 及 4 个 `.cs` 命名空间声明;验证:`dotnet build` 0 错误 + `dotnet test` 全绿(727;另修正测试项目 ProjectReference 路径)
- [x] 6.2 `IdentityComponentsEndpointRouteBuilderExtensions.cs` 移入 `MultiClusterMgmtSys.Web/Endpoints/`(命名空间 `MultiClusterMgmtSys.Web.Endpoints`),宿主 csproj 清理已迁走的 `<Folder Include>` 项与不再直接使用的包引用(k8s/Identity.EFCore);验证:`dotnet build` 0 错误 + `dotnet test` 全绿(727)
- [x] 6.3 运行冒烟:`dotnet run --project MultiClusterMgmtSys.Web` 登录页 200、`css/app.css` 200、`js/show-password.js` 200、`/templates/workload/deployment.yaml` 200、`/clusters` 未登录重定向 `/login?returnUrl=/clusters`;scoped CSS 包 URL 在 dev-run 下 500 为**既有问题**(同一 worktree 在 HEAD 基线复现,`StaticAssetDevelopmentRuntimeHandler` 框架行为,非本次引入,范围外);YAML 模板读取已验证

## 7. 测试重映射

- [x] 7.1 测试目录重排为 `Domain/`/`Application/`/`Infrastructure/`/`Components/`(原 `Services/`→`Application/`、`Data/`→`Infrastructure/Persistence`、`Models`/`ViewModels`→`Application/*`、`Common` 按被测层拆分;ClusterClientCache/Sync/YamlTemplate 测试归 `Infrastructure/*`),命名空间改 `MultiClusterMgmtSys.Tests.<Layer>.*`;验证:`dotnet test` 全绿且测试数不变(727;另修复因重排暴露的并行时序用例 `ClustersPageFlowTests2.Refresh_all_updates_status_to_offline`,仅加有界等待,断言不变)
- [x] 7.2 核对 `TestInfrastructure` 全部文件(ServiceHarness/BunitHost/BunitServiceExtensions/K8sMocks/SqliteDbFactory/TestPaths 等)引用与路径;验证:`dotnet test` 全绿(727)

## 8. 工具链与文档同步

- [x] 8.1 `coverage.ps1` 改为四程序集(Domain/Application/Infrastructure/Web)合并行覆盖率门禁(按 1.1 解析方案,测试程序集不计);验证:`./coverage.ps1` 成功且输出合并覆盖率 ≥75%(实测 77.5%,6448/8317)
- [x] 8.2 AGENTS.md 全节同步:Stack(四项目)、Commands、Testing conventions(架构测试规则清单)、Coverage 工具链、Architecture notes(分层 DI 注册规则)、Folder/namespace gotchas、Service contracts、Code style;验证:已整篇重写为四项目布局
- [x] 8.3 README.md「目录结构」节同步为四项目布局与职责说明,构建/部署命令中的程序集名同步;验证:结构与实际项目一一对应
- [x] 8.4 同步两份现有主 spec 的 Purpose(引用旧目录/命名空间):`openspec/specs/service-contracts/spec.md`、`openspec/specs/exception-handling/spec.md`(按 OpenSpec 规则直接编辑主 spec);验证:两份 Purpose 不再出现旧路径
- [x] 8.5 构建脚本核对:`build-image.ps1`/`build-image.sh`、`docker-compose*.yml`、`.dockerignore`、`docker-compose.dcproj` 的路径与程序集名一致;验证:路径已核对(docker daemon 不可用,构建冒烟留 10.4)

## 9. 架构测试

- [x] 9.1 测试项目引入 `NetArchTest.Rules` 1.3.2,新增 `Architecture/ArchitectureTests.cs` 实现 `architecture-layering` 的六条断言(组件命名空间不依赖 Infrastructure/EF/`Domain.Entities`/k8s;组件不注入仓库端口(反射检查含 `IAccountQueryRepository`);Application 不依赖 EF/DbContext/Infrastructure/Web;Requests/ViewModels/Models 归属;Domain/Web 不依赖 k8s;程序集引用兜底);验证:`dotnet test` 全绿。附带修正:架构测试发现 6 个组件(`WorkloadYamlEditView`/`CreateSvcDialog`/`EditSvcYaml`/`CreateNamespaceDialog`/`CreateConfigMapDialog`/`EditConfigMapYaml`)直接用 `KubernetesYaml.Deserialize` 做本地校验 → 按契约新增 `IYamlValidator` 端口(Application/Abstractions + Application/Services 实现,行为不变)并下沉
- [x] 9.2 负向验证:临时新增 `_ArchViolationProbe.razor`(`@inject IClusterRepository` + 使用 `Domain.Entities.ClusterInfo`),确认两条架构测试分别失败且指出探针类型与违规类型,然后删除;验证:失败输出可读且定位到类型;删除后 733 全绿

## 10. 全量回归

- [x] 10.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 10.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线 727 + 6 架构测试 = 733)
- [x] 10.3 `./coverage.ps1` 四程序集合并行覆盖率 ≥75%(实测 77.5%,6448/8317)
- [x] 10.4 Docker 生产构建冒烟:本机 docker daemon 未运行,**环境不可用已记录**;Dockerfile/compose/build 脚本路径已核对一致,留待有 docker 环境时执行 `docker compose -f docker-compose.prod.yml up -d --build`
- [ ] 10.5 端到端手工冒烟:登录 → 集群列表(分组筛选/版本过滤)→ 集群详情 tab → 工作负载列表/详情/YAML 编辑 → ConfigMap/Service/命名空间/事件页 → 账号页 Admin 门控;观察行为与重构前一致(需浏览器会话,自动化已覆盖:build/733 测试/bUnit 接线契约/覆盖率门禁/登录页与静态资源冒烟,此步留待人工)
