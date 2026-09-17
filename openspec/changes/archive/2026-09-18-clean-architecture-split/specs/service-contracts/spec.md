# service-contracts Delta

## MODIFIED Requirements

### Requirement: 服务方法输入统一为 Request 对象
`MultiClusterMgmtSys.Application` 下所有公开服务方法的入参,凡来源于前端调用方(页面/对话框/组件)的,SHALL 收拢为 `Application/Requests` 下的 Request 对象(类名带 `Request` 后缀),不得散落多个原语参数。以下情形 SHALL 豁免:入参只有一个(单原语参数,如 `GetClusterDetailAsync(int id)`);`RegisterAsync`/`LoginAsync` 已符合;`AuditService.LogAsync`(内部审计调用)。

#### Scenario: 多原语参数收拢为 Request
- **WHEN** 服务方法接收 2 个及以上来自前端传参的原语参数(如 `CreateAccountAsync(string, string, string)`)
- **THEN** 该方法入参改为单个 Request 对象,字段与原参数一一对应

#### Scenario: 单原语参数豁免
- **WHEN** 服务方法仅接收 1 个原语参数(如 `GetClusterDetailAsync(int id)`)
- **THEN** 保持原样,不包 Request

#### Scenario: ViewModel 不得充当服务入参
- **WHEN** 服务方法入参类型位于 `Application/ViewModels` 目录
- **THEN** 该类型拆出对应 `*Request` 后由 Request 承接输入职责,ViewModel 仅用于展示/回显

### Requirement: 服务方法输出统一为 ViewModel
服务方法返回给前端展示的数据 SHALL 使用 `Application/ViewModels` 下的 ViewModel(含 `PagedResult<T>` 包装),不得直接返回实体类(`Domain/Entities`)。以下情形 SHALL 豁免:框架类型(`IdentityResult`/`SignInResult`)、裸 `List<string>` 下拉数据(如 `GetAvailableVersionsAsync`)、`IProgress` 进度回调、无返回值的审计/写操作。

#### Scenario: 实体不外泄
- **WHEN** 服务方法需要向调用方返回数据库实体(如 `ApplicationUser`)
- **THEN** 方法改为返回对应 ViewModel,实体仅在服务内部使用

#### Scenario: 框架类型与下拉数据豁免
- **WHEN** 返回 `IdentityResult`/`SignInResult` 或裸 `List<string>`
- **THEN** 保持原样,不包 ViewModel

### Requirement: 服务层禁止前端框架类型
`MultiClusterMgmtSys.Application` 的服务与 `MultiClusterMgmtSys.Infrastructure` 的仓库,其公开方法签名 SHALL NOT 出现前端框架(MudBlazor)类型(`TableState`、`DateRange` 等)。分页与排序信息 SHALL 并入对应 `*QueryRequest` 对象(对齐 `ClusterQueryRequest` 范本:Page/PageSize/SortBy/SortDescending)。

#### Scenario: 分页状态并入 Request
- **WHEN** 调用方发起分页查询(账号列表、审计列表)
- **THEN** 分页、排序信息从 Request 读取,服务签名无 `TableState`

#### Scenario: 日期区间用普通类型
- **WHEN** 查询请求含日期区间过滤(集群列表创建时间)
- **THEN** Request 使用两个 `DateTime?` 字段(起始/截止),不使用 MudBlazor `DateRange`

### Requirement: 目录归属约定
`Application/Requests` SHALL 只放输入类(Request 及输入编辑行,如 `NodeIpNoteEditItem`、`ClusterEndpointEditItem`);`Application/ViewModels` SHALL 只放展示输出类(含输出结果类如 `AccountBatchResult`);不属于两者的类(纯前端状态、查询对象与哨兵常量等)SHALL 放 `Application/Models`。

#### Scenario: 输出结果类归 ViewModels
- **WHEN** 类用于承载服务返回结果(如 `AccountBatchResult`)
- **THEN** 该类位于 `Application/ViewModels` 而非 `Application/Requests`

#### Scenario: 前端状态类归 Models
- **WHEN** 类仅为前端组件内部过滤状态、不传递给服务(如 `NodeListFilter`)
- **THEN** 该类位于 `Application/Models`

#### Scenario: 输入编辑行归 Requests
- **WHEN** 类承载编辑器提交行数据(如 `ClusterEndpointEditItem`)
- **THEN** 该类位于 `Application/Requests`
