# architecture-layering

## Purpose


定义仓库的四项目分层与依赖方向契约:每个项目的职责与可引用范围、端口与实现的归位,以及 Web 内组件命名空间的越界拦截规则(编译器无法阻断的部分由随测试套件执行的架构测试保证),使分层不再依赖人工评审与文档纪律。

## Requirements

### Requirement: 四项目分层与依赖方向
系统 SHALL 由四个生产项目构成:`MultiClusterMgmtSys.Domain`、`MultiClusterMgmtSys.Application`、`MultiClusterMgmtSys.Infrastructure` 与 `MultiClusterMgmtSys.Web`(宿主 + Blazor UI 同项目)。

项目引用 SHALL 满足:`Domain` 零项目与包依赖;`Application` 引用 `Domain`,`SHALL NOT` 引用 `Infrastructure`、`Web` 或 EF Core/DbContext;`Infrastructure` 引用 `Application` 与 `Domain`,`SHALL NOT` 引用 `Web`;`Web` 引用 `Application` 与 `Infrastructure`,承载组合根(`Program.cs`、DI 注册、`appsettings`、`wwwroot`)。`Application` SHALL NOT 引用 EF Core、DbContext 或任何持久化实现类型。

#### Scenario: Domain 零依赖
- **WHEN** 编译 `MultiClusterMgmtSys.Domain`
- **THEN** 该项目没有任何项目引用或 NuGet 包引用

#### Scenario: 用例层无法触达持久化实现
- **WHEN** 在 `MultiClusterMgmtSys.Application` 中引用 `ApplicationDbContext` 或 `MultiClusterMgmtSys.Infrastructure` 的任意类型
- **THEN** 编译失败(项目既不引用 Infrastructure,也不引用 EF Core)

#### Scenario: 数据层不依赖 Web
- **WHEN** 编译 `MultiClusterMgmtSys.Infrastructure`
- **THEN** 该项目不引用 `MultiClusterMgmtSys.Web`,仓库实现不使用任何 Web/UI 类型

#### Scenario: 框架/SDK 包的务实边界
- **WHEN** 用例服务需要调用 KubernetesClient 或 ASP.NET Identity 类型
- **THEN** `Application` 可直接引用对应包(不强制为每个外部依赖建立端口)

### Requirement: 端口与实现归位
跨层访问的接口(端口)SHALL 位于 `Application`(`Abstractions` 文件夹);实现 SHALL 位于 `Infrastructure`;业务异常层次 SHALL 位于 `Domain`。K8s 异常翻译器 SHALL 与 K8s 调用同层(`Application`),客户端配置与客户端缓存实现 SHALL 位于 `Infrastructure`。K8s 客户端 SHALL 仍仅经 `IClusterClientCache` 获取(`k8s-client-cache` 契约),DI 注册由各层注册扩展与 Web 组合根完成。

#### Scenario: 仓库端口归位
- **WHEN** 查找集群/分组/审计/应用设置仓库的类型
- **THEN** `IClusterRepository`、`IGroupRepository`、`IAuditLogRepository`、`IAppSettingRepository` 位于 `MultiClusterMgmtSys.Application/Abstractions`,对应实现位于 `MultiClusterMgmtSys.Infrastructure/Persistence`

#### Scenario: 缓存与模板端口归位
- **WHEN** 查找 `IClusterClientCache` 与 `IYamlTemplateService`
- **THEN** 接口位于 `Application`,`ClusterClientCache` 与 `YamlTemplateService` 实现位于 `Infrastructure`

#### Scenario: 业务异常位于 Domain
- **WHEN** 服务抛出 `NotFoundException` / `ConflictException` / `ValidationException` / `PermissionException` / `ClusterUnreachableException`
- **THEN** 这些类型位于 `MultiClusterMgmtSys.Domain.Exceptions`,Web 与 Infrastructure 均可引用

#### Scenario: K8s 异常翻译器与调用同层
- **WHEN** 服务捕获 K8s 异常并调用 `K8sExceptionMapper.Translate`
- **THEN** `K8sExceptionMapper` 位于 `MultiClusterMgmtSys.Application`(与 K8s 调用同层,可被服务直接引用)

### Requirement: Web 组件命名空间隔离
`MultiClusterMgmtSys.Web.Components` 命名空间下的类型 SHALL 仅使用 `Application` 契约(`Requests`/`ViewModels`/`Models`/`Abstractions`/用例服务)与框架 UI 类型;SHALL NOT 使用 `MultiClusterMgmtSys.Infrastructure` 类型、EF Core/DbContext、`Domain.Entities` 实体、仓库端口或 `k8s.*` 类型。该约束 SHALL 由架构测试保证(Web 与组合根同程序集,编译器无法阻断);`Program`/`Endpoints`/DI 注册等组合根代码不在该命名空间内,不受此约束。

#### Scenario: 组件注入基础设施即违规
- **WHEN** `MultiClusterMgmtSys.Web.Components` 下的组件注入 `ApplicationDbContext`、仓库实现或 `IClusterClientCache` 实现
- **THEN** 架构测试失败并指出违规类型与规则

#### Scenario: 组件使用实体或 k8s 类型即违规
- **WHEN** 组件类型或方法使用 `Domain.Entities` 中的实体或 `k8s.*` 类型
- **THEN** 架构测试失败并指出违规类型与规则

#### Scenario: 组合根豁免
- **WHEN** `Program.cs` 注册 `AddInfrastructure(configuration)` 等组合根代码引用 Infrastructure 类型
- **THEN** 不属违规(组合根职责)

### Requirement: 架构规则自动化断言
测试项目 SHALL 包含架构测试并随 `dotnet test` 执行,至少断言:Web 组件命名空间不依赖 Infrastructure/EF Core/`Domain.Entities`/仓库端口/k8s;`Application` 不依赖 EF Core/DbContext/`Infrastructure`;`Requests`/`ViewModels`/`Models` 类型归属符合 `service-contracts`;`k8s.Kubernetes` 具体客户端类型仅出现在 Infrastructure 工厂;`Infrastructure` 不引用 Web、`Application` 不引用 Infrastructure(程序集引用兜底)。任一违规 SHALL 使测试失败并给出违规类型与规则名。

#### Scenario: 越界即测试失败
- **WHEN** 人为在 Web 组件中注入 `IClusterRepository` 或使用 `Domain.Entities` 实体
- **THEN** 架构测试失败并指出违规类型与规则

#### Scenario: 分层门禁可执行
- **WHEN** 执行 `dotnet build MultiClusterMgmtSys.slnx` 与 `dotnet test MultiClusterMgmtSys.Tests`
- **THEN** 违规的分层引用导致编译失败或架构测试失败,不依赖人工评审拦截