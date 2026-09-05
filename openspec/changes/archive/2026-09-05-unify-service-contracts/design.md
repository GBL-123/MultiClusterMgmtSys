## Context

服务层(`Services/`)目前 7 个服务、约 35 个公开方法,签名约定半生效:

- **已合规**:`ClusterService.GetPagedAsync(ClusterQueryRequest)`、`AuthService.RegisterAsync/LoginAsync(Request)`、全部查询方法返回 VM。
- **违规输入**:~20 个方法散落原语参数(`CreateAccountAsync(string,string,string)` 等);`AddClusterAsync(ClusterCreateViewModel)`、`UpdateClusterAsync(ClusterUpdateViewModel)`、`UpdateClusterEndpointsAsync(List<ClusterEndpointEditItem>)` 用 ViewModel 当输入;`NodeIpNoteEditItem` 已正确放 Requests(不对称,`ClusterEndpointEditItem` 却放 ViewModels)。
- **违规输出**:`GetUserByNameAsync` 返回 `ApplicationUser` 实体;`AccountBatchResult`(输出)误放 Requests/。
- **UI 框架泄漏**:`GetPagedAccountsAsync(TableState, ...)`、`AuditService.GetPagedAsync(TableState, ...)`、`AuditLogRepository.GetPagedAsync(state, ...)` 依赖 MudBlazor `TableState`,且服务内"双通道"优先用 TableState、无视 Request 已有分页字段(`AccountQueryRequest` 已含 Page/PageSize/SortDescending);`ClusterQueryRequest.DateRange` 是 MudBlazor `DateRange?`。
- **操作者身份由前端传入**:`BatchDeleteAsync(ids, currentUserId)`、`DeleteAccountAsync(id, currentUserId)`、`ChangePasswordAsync(username, ...)` 等,服务拿前端给的"我是谁"做防自删等安全判断;而 `AuditService.LogAsync` 已示范 `httpContextAccessor.HttpContext?.User` 正确做法。
- 全项目无 DataAnnotations 校验注解,校验全在组件层(`RequiredError` 内联 + 手写条件),拆 Request 无注解搬迁成本。

`ClusterQueryRequest`(过滤+分页+排序全含,服务零 MudBlazor 依赖)是现成范本。

## Goals / Non-Goals

**Goals:**
- 服务方法签名统一:输入 = `Requests/` 下的 Request 对象(单原语参数豁免);输出 = `ViewModels/` 下的 ViewModel。
- 服务层彻底清除 MudBlazor 类型(`TableState`、`DateRange`)。
- 操作者上下文(当前用户身份)一律服务端从 `HttpContext` 获取,签名中删除前端传入的 currentUserId/currentUserName。
- 目录整理:`Requests/`=输入、`ViewModels/`=输出、`Models/`=其余(纯前端状态、哨兵等)。
- 契约写入 `openspec/specs/service-contracts/spec.md` 供后续会话遵守。

**Non-Goals:**
- 不改业务逻辑、K8s 调用、审计行为、UI 表现;纯签名与类型归属重构。
- 不引入 DataAnnotations 校验(维持组件层校验现状)。
- 不重构 `LogAsync`(内部审计,豁免);不处理 `RefreshAllClustersStatusAsync` 的 `IProgress`(豁免)。
- 不改数据库 schema,不新增 EF 模型。

## Decisions

### D1: 契约规则

- 输入:前端传入的参数 → Request 对象。**单原语参数豁免**(如 `GetClusterDetailAsync(int id)` 保持 `int`)。
- 输出:展示用 → ViewModel。
- 豁免:框架类型返回值(`IdentityResult`/`SignInResult`)、裸 `List<string>` 下拉数据(`GetAvailableVersionsAsync`/`GetNamespacesAsync`)、`LogAsync` 内部审计、`IProgress` 进度回调、单原语参数。
- 替代方案(不选):把 Create/Update ViewModel 直接定为"输入输出双用"——与"展示=VM、提交=Request"的分离原则冲突,且 update 回显(EditViewModel)与提交(UpdateRequest)字段语义不同,合并会重新埋雷。

### D2: 操作者上下文走 HttpContext

`AccountService`/`AuditService` 构造注入 `IHttpContextAccessor`(AuditService 已有),服务内通过 `httpContextAccessor.HttpContext?.User` 取当前用户名/ID 与管理员角色:

- `ChangePasswordAsync` 的目标用户名 = 当前登录用户名(Profile 页场景固定如此);
- `DeleteAccountAsync`/`BatchDeleteAsync` 的防自删/防批量删自己校验改用 HttpContext 用户 ID;
- `GetPagedAccountsAsync`/`AuditService.GetPagedAsync` 的 `isAdmin`/`currentUserName` 从 `HttpContext.User` 的 Claims 推导(服务内提取,不暴露为参数);
- `LogoutAsync` 的审计用户名改从 HttpContext 取。

替代方案(不选):保留前端传参但仅作审计展示——无法消除"前端可伪造身份"的安全隐患,且参数瘦身收益丢失。

### D3: TableState 清除,分页并入 Request

- `AccountQueryRequest` 补 `SortBy`(字符串,对齐 `state.SortLabel` 现有取值 `UserName`/`LastLoginAt`/默认 CreatedAt)或枚举;选**字符串**,与现服务端 switch 分支零改造成本。
- `AuditLogQueryRequest` 已含 Page/PageSize,补 `SortDescending`/排序字段(看 Repository 现有排序逻辑决定字段名)。
- `AuditLogRepository.GetPagedAsync(state, query, currentUserName, isAdmin)` → `GetPagedAsync(query, currentUserName, isAdmin)`。
- `ClusterQueryRequest.DateRange`(MudBlazor `DateRange?`)拆为 `DateTime? CreatedFrom` / `DateTime? CreatedTo`,服务内 UTC 处理逻辑保留。
- 服务内删除"双通道"回退逻辑(`state.Page > 0 ? ... : query.Page`),分页只信 Request。
- 调用页(Accounts.razor / Audits.razor)从 `TableState` 取值改为填充 Request 字段。

### D4: Cluster 输入拆分

- 新增 `ClusterCreateRequest`(字段=现 `ClusterCreateViewModel` 全部)与 `ClusterUpdateRequest`(多 `Id`)。
- `ClusterCreateViewModel` 删除;`ClusterUpdateViewModel` 删除,回显继续用 `ClusterEditViewModel`(`GetClusterForEditAsync` 返回),`UpdateClusterAsync` 吃 `ClusterUpdateRequest`——EditClusterDialog 表单回显绑定 `ClusterEditViewModel`,提交时组装 Request。
- `ClusterEndpointEditItem` 从 ViewModels 挪入 Requests(它是编辑器提交的输入行,`NodeIpNoteEditItem` 同款已住 Requests)。
- 无注解搬迁成本(全项目无 DataAnnotations)。

### D5: 其余服务签名映射(完整清单)

| 服务 | 现签名 | 新签名 |
|---|---|---|
| AccountService | GetPagedAccountsAsync(TableState, AccountQueryRequest) | GetPagedAccountsAsync(AccountQueryRequest) |
| | BatchDeleteAsync(IReadOnlyList\<int\>, int) | BatchDeleteAsync(IReadOnlyList\<int\>) — HttpContext 取操作者 |
| | BatchUpdateRoleAsync(ids, int, string) | BatchUpdateRoleAsync(BatchRoleUpdateRequest) |
| | CreateAccountAsync(string, string, string) | CreateAccountAsync(AccountCreateRequest) |
| | UpdateAccountAsync(int, string?) | UpdateAccountAsync(AccountUpdateRequest) |
| | DeleteAccountAsync(int, int) | DeleteAccountAsync(int id) — HttpContext 取操作者 |
| | ResetPasswordAsync(int, string) | ResetPasswordAsync(ResetPasswordRequest) |
| | ChangePasswordAsync(string, string, string) | ChangePasswordAsync(ChangePasswordRequest) — 用户名走 HttpContext |
| | GetUserByNameAsync(string) → ApplicationUser? | GetUserByNameAsync(string) → AccountViewModel? |
| AuditService | GetRecentAsync(string, int) | GetRecentAsync(int count) — 用户名走 HttpContext |
| | GetPagedAsync(TableState, AuditLogQueryRequest, string?, bool) | GetPagedAsync(AuditLogQueryRequest) — 用户/角色走 HttpContext |
| ClusterService | AddClusterAsync(ClusterCreateViewModel) | AddClusterAsync(ClusterCreateRequest) |
| | UpdateClusterAsync(ClusterUpdateViewModel) | UpdateClusterAsync(ClusterUpdateRequest) |
| | UpdateClusterEndpointsAsync(int, List\<ClusterEndpointEditItem\>) | UpdateClusterEndpointsAsync(ClusterEndpointsUpdateRequest) |
| ClusterNodeService | GetNodeDetailAsync(int, string) | GetNodeDetailAsync(NodeDetailQueryRequest) |
| | UpdateNodeIpNotesAsync(int, string, List\<NodeIpNoteEditItem\>) | UpdateNodeIpNotesAsync(NodeIpNotesUpdateRequest) |
| GroupService | RenameGroupAsync(int, string) | RenameGroupAsync(GroupRenameRequest) |
| | MoveClustersToGroupAsync(IEnumerable\<int\>, int?) | MoveClustersToGroupAsync(MoveClustersRequest) |
| ConfigMapService | ListConfigMapsAsync(int, string?) | ListConfigMapsAsync(ConfigMapQueryRequest) |
| | GetConfigMapAsync(int, string, string) | GetConfigMapAsync(ConfigMapKeyRequest) |
| | DeleteConfigMapAsync(int, string, string) | DeleteConfigMapAsync(ConfigMapKeyRequest) — 与 Get 共用 |
| | UpdateConfigMapFromYamlAsync(int, string, string, string) | UpdateConfigMapFromYamlAsync(ConfigMapUpdateRequest) |
| | CreateConfigMapFromYamlAsync(int, string) | CreateConfigMapFromYamlAsync(ConfigMapCreateRequest) |
| AuthService | LogoutAsync(string) | LogoutAsync() — 审计用户名走 HttpContext |

豁免保持原样:`AddGroupAsync(string)`、`DeleteGroupAsync(int)`、`GetGroupsAsync()`、`GetClusterDetailAsync(int)`、`GetClusterForEditAsync(int)`、`DeleteClusterAsync(int)`、`RefreshClusterStatusAsync(int)`、`GetClusterNodesAsync(int)`、`GetNamespacesAsync(int)`、`GetAvailableVersionsAsync()`、`GetUngroupedClusterCountAsync()`、`RefreshAllClustersStatusAsync(IProgress<...>)`、`RegisterAsync/LoginAsync(已有 Request)`。

### D6: 目录搬迁

- `AccountBatchResult` → `ViewModels/`(输出)。
- `NodeListFilter` → `Models/`(纯前端过滤状态,不传服务,查证无服务引用)。
- 新增 Request 全部 `*Request` 后缀(与现有 `RegisterRequest` 等一致),`ConfigMapKeyRequest` 供 get/delete 共用。

### D7: 测试同步

`MultiClusterMgmtSys.Tests` 以服务为边界直调公开方法,签名变更处同步改:组装 Request 入参、断言返回类型不变(VM/框架类型断言不受影响)。新增针对"操作者上下文走 HttpContext"的测试——用 `TestServices` 已有模式注入可构造身份的 HttpContext(或 Mock `IHttpContextAccessor`)。

## Risks / Trade-offs

- [R1: HttpContext 在测试中为空] → 测试基建提供 `IHttpContextAccessor` 的可控 Mock(默认带 Identity Claims),服务代码空引用保护(`?.User`)沿用 `AuditService.LogAsync` 现有写法。
- [R2: 签名批量变更编译面广(6 服务 + 1 仓库 + ~10 页面 + 测试)] → 按 D5 清单逐服务改完即 `dotnet build`,再统一跑 `dotnet test`;页面调用点一次改完,避免中间态编译失败。
- [R3: 防自删/防批量删逻辑从"前端传 ID"改 HttpContext 后,若 HttpContext 取不到用户] → 服务端在取不到身份时抛 `PermissionException`(走现有异常体系),不静默放行。
- [R4: `DateRange` 拆字段后 ClusterFilterBar 回写逻辑变化] → 过滤条显式绑定日期区间,组装 Request 时映射两个字段(改动局限在 Clusters.razor/ClusterFilterBar 一处)。
- [R5: 66 测试中遗漏某签名引用] → 最终验证 = `dotnet build` 0 错误 + `dotnet test` 全绿,遗漏者由编译器兜底找出。

## Open Questions

- `AuditLogQueryRequest` 排序字段名待定:对齐 `AccountQueryRequest.SortBy` 字符串取值(看 Audits 页现排序标签)。
- `ClusterEndpointsUpdateRequest` 与 `NodeIpNotesUpdateRequest` 的"定位参数"(clusterId/name/ns)是独立属性并入 Request,还是保留方法参数——D5 已定并入 Request(2+ 参数即包),此处仅是字段放置,无分歧。