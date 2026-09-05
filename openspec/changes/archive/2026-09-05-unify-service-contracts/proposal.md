## Why

服务层方法签名约定目前"半生效":查询类方法大多已用 Request 入参 + ViewModel 返回,但命令类方法(create/update/delete/move/rename/batch)散落大量原语参数,部分 ViewModel 兼职做输入,`TableState`/`DateRange` 等 MudBlazor 类型泄漏进服务层,`ApplicationUser` 实体从服务层直接返回,操作者身份(currentUserId/currentUserName)由前端传入却用于安全判断。签名混乱导致服务契约不可预期、UI 框架类型污染业务层、调用点改动牵连面大。

## What Changes

- 确立服务层输入输出契约:**前端传入的参数一律收拢为 `Requests/` 下的 Request 对象;返回给前端展示的用 `ViewModels/` 下的 ViewModel**。
- **BREAKING** 豁免规则显式化:单原语参数不包 Request;`IdentityResult`/`SignInResult` 等框架类型返回值、裸 `List<string>` 下拉数据、`LogAsync` 内部审计、`IProgress` 进度回调豁免。
- **BREAKING** 操作者上下文统一从 `HttpContext` 获取(沿用 `AuditService.LogAsync` 先例),从服务签名中删除前端传入的 `currentUserId`/`currentUserName` 参数。
- **BREAKING** 服务层清除 MudBlazor 类型依赖:`TableState` 从 `AccountService`/`AuditService`/`AuditLogRepository` 签名中移除,分页排序信息并入各 QueryRequest(对齐 `ClusterQueryRequest` 范本);`ClusterQueryRequest.DateRange` 拆为两个 `DateTime?`。
- **BREAKING** `ClusterCreateViewModel`/`ClusterUpdateViewModel` 拆出 `ClusterCreateRequest`/`ClusterUpdateRequest`;`ClusterEndpointEditItem` 从 ViewModels 挪入 Requests。
- `GetUserByNameAsync` 不再返回 `ApplicationUser` 实体,改返回 ViewModel。
- 目录整理:`AccountBatchResult` 从 Requests 挪入 ViewModels;`NodeListFilter`(纯前端状态,不传服务)挪入 Models;新增 Request 类统一 `Request` 后缀命名。

## Capabilities

### New Capabilities
- `service-contracts`: 服务层输入输出契约约定——Request 入参 / ViewModel 返回、豁免规则、操作者上下文来源、服务层禁入前端框架类型、目录归属。

### Modified Capabilities
<!-- 无现有 spec 的需求级变更;服务契约是新能力,不修改现有能力的需求 -->

## Impact

- `Services/`:`AccountService`(9 处签名)、`AuditService`(2 处)、`ClusterService`(5 处)、`ClusterNodeService`(2 处)、`GroupService`(2 处)、`ConfigMapService`(5 处)、`AuthService`(1 处)。
- `Data/Repositories/AuditLogRepository.cs`:`GetPagedAsync` 移除 `TableState` 依赖。
- `Requests/`:新增约 16 个 Request 类,挪出 2 个类;`ViewModels/`:拆解 2 个类、挪出/挪入各 1 个;`Models/`:挪入 1 个类。
- 调用点:`Components/` 下 ~10 个页面/对话框(Accounts、Profile、Audits、Clusters、EditClusterDialog、EditGroupDialog、ClusterDetail、NodeDetail、ConfigMaps 等)与方法映射扩展类。
- `MultiClusterMgmtSys.Tests`:66 个测试中引用变更签名的部分同步跟进(服务边界测试直接调 Service 公开方法)。
- 无数据库 schema 变更,无 K8s 调用逻辑变更,纯签名与目录重构。