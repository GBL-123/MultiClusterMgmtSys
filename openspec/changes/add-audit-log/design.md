## Context

MultiClusterMgmtSys 是一个 .NET 10 / Blazor Server (interactive server) + MudBlazor + SQLite 的单项目解决方案。现有架构：`Data/Repositories` 负责持久化，`Components/<Feature>/Services` 组合逻辑与 K8s 调用，`.razor` 页面绑定 ViewModels，经 `*.ViewModels.Mappings` 映射。所有变更操作集中在 6 个 Service 中，且已有 `logger.LogInformation` enter/done 的日志惯例。schema 由 `EnsureCreated()` 在启动时创建，无 EF migrations；项目处于开发期，删库可接受。Identity 使用 `int` 主键，角色 `Admin`/`Member`。UI 文案为中文。

## Goals / Non-Goals

**Goals:**
- 以 SQLite 同库新表存储审计日志，`EnsureCreated` 重建 schema（开发期删库可接受）。
- 覆盖全部"人发起"的变更操作与认证事件（登录/登出/注册），不覆盖自动状态刷新。
- Admin 可见全部日志并可按操作人筛选；Member 仅见自己（服务层强制过滤）。
- 提供分页查询页面 `/audit-logs`，对所有登录用户可见，MudTable 呈现。
- 审计写入容错：失败不影响主操作。
- 追加只读，无删除/清空入口。

**Non-Goals:**
- 不记录变更详情（旧值/新值），不序列化请求体。
- 不做日志保留策略/归档/清理。
- 不使用 EF SaveChangesInterceptor 或 HTTP Middleware 捕获（见 Decisions）。
- 不记录 `RefreshStatus`/`RefreshAllClustersStatus` 与启动播种（`CreateAdminAsync`）。

## Decisions

### D1：显式调用 `AuditService`（而非 EF Interceptor / Middleware）
在各 Service 的变更方法中显式调用 `AuditService.LogAsync(...)`。

- 理由：Service 层是全部变更的汇聚点（天然钩子）；能记录语义化的"操作人/类别/操作/目标"；能覆盖 K8s 变更（ConfigMap YAML 不落库，Interceptor 捕不到）；启动播种可自然排除。
- 备选 A：`SaveChangesInterceptor` —— 自动但无用户上下文、Identity 表噪音大、捕不到 K8s 变更、播种期误记。
- 备选 B：HTTP Middleware —— Blazor Server 变更走 SignalR 电路，中间件几乎捕获不到，排除。

### D2：`AuditLog` 同库新表（`MultiClusterMgmtSys.db`）
`AuditLog` 实体 + `ApplicationDbContext.AuditLogs` DbSet + `OnModelCreating` 配置。无迁移，`EnsureCreated` 重建时自动建表，开发期删除本地 `*.db` 即可。

- 备选：独立 `audit.db` 第二 DbContext —— 不动现有库但多一套接线；当前无保留需求，排除。JSONL 文件日志 —— UI 分页查询别扭，排除。

### D3：用户身份解析 —— 混合方案
`AuditService` 注入 `IHttpContextAccessor`（需在 `Program.cs` 增加 `AddHttpContextAccessor()`），通过 `HttpContext.User.Identity?.Name` 解析操作人（Blazor Server 电路内可用）。

- 认证事件例外：登录/登出/注册瞬间 `HttpContext.User` 无身份或为匿名，由调用方**显式传用户名**（`LoginAsync` 已接收 `request.UserName`、`LogoutAsync` 已有 `userName` 参数、`RegisterAsync` 有 `request.UserName`），`AuditService` 提供带显式 user 参数的重载。
- 备选：页面传 `currentUserId`（`AccountService.BatchDeleteAsync` 先例）—— 每个变更点都要改签名传参，噪音大。

### D4：枚举与目标约定
- `Common/Enums/AuditCategory.cs`：`Authentication`(认证) / `Account`(账号) / `Cluster`(集群) / `Group`(分组) / `Configmap`(配置) / `Node`(节点)。
- `Common/Enums/AuditAction.cs`：`Login` / `Logout` / `Register` / `Create` / `Update` / `Delete` / `Move` / `Rename`。
- `Target` 为调用点拼接的中文描述串，如 `"集群: prod-k8s"`、`"账号: alice"`、`"配置: default/app-config"`、`"分组: 生产"`、`"节点: node-1@prod-k8s"`。
- 枚举以 int 存库（EF 默认）；展示时映射为中文（页面/映射扩展处统一处理）。

### D5：查询权限在仓储/服务层强制
`AuditLogRepository.GetPagedAsync(TableState, filter, currentUserName, isAdmin)`：`isAdmin == false` 时无条件追加 `WHERE UserName == currentUserName`，调用方无法绕过。页面传入当前用户与角色判定（基于 `AuthenticationStateProvider`/`AuthorizeView` 之上的服务层判定，如 `AccountService` 查询模式）。

### D6：写入时序与容错
审计写入仅在主操作成功后进行；`AuditService.LogAsync` 内部 `try/catch`，失败仅 `logger.LogWarning`，不抛出。认证事件（登录成功/登出/注册成功）写入亦遵循此原则（登出虽名为 "SignOutAsync"，仍在调用后写入，用户上下文依赖显式参数不依赖身份）。

### D7：页面与导航
- 新增 `Components/AuditLogs/Pages/AuditLogs.razor`（路由 `/audit-logs`），沿用 `Accounts.razor` 的 MudTable + `PagedResult<>` + `TableState` 模式；Member 无操作人筛选框，Admin 有。
- 表格呈现复刻集群管理页（`ClusterTable.razor` + `wwwroot/css/app.css` 的 `.clusters-table` flex-fill 模式）：表格平铺于页面（不包 MudPaper 卡片）、填满剩余高度、时间列用 `MudTableSortLabel` 可排序；样式类 `audit-logs-table` 与 `.clusters-table` 规则一致。
- Drawer 在"账号管理"之后新增入口：`审计日志`，`Icons.Material.Filled.History`，无角色限制（所有登录用户可见）。
- 命名空间遵循 `Components/AuditLogs/**` → `MultiClusterMgmtSys.Components.AuditLogs.*`（与 `Components/Clusters` 等物理路径一致的模式）；枚举在 `Common/Enums`。

## Risks / Trade-offs

- [枚举新增需改 `Common/Enums` 与映射，若将来类别/操作扩展，枚举 + 映射扩展点需同步维护] → 在 `AuditLogViewModel` 映射扩展集中处理中文显示，扩展点单一。
- [审计写失败（如磁盘/DB 异常）导致日志缺失] → D6 容错设计确保主操作不受影响；服务日志保留 `LogWarning` 供排查。
- [`IHttpContextAccessor` 在 Blazor Server 个别重连/后台场景下 HttpContext 可能为 null] → `LogAsync` 对 null 身份降级为不记操作人（`UserName = null`）或跳过，不抛异常。
- [删库重建会清空现有数据] → 开发期可接受（已确认）；`AuditLog` 随 `EnsureCreated` 自动建表，无需手工步骤。
- [同库追加写入与主操作共享 scoped DbContext，主操作 SaveChanges 与审计写入在同一事务上下文] → 顺序为先主后审，审计独立 `SaveChangesAsync`；主操作异常时审计不写入（符合"成功后才记"）。

## Migration Plan

1. 开发环境：实现完成后删除本地 `MultiClusterMgmtSys.db`（与 `*.db-shm`/`*.db-wal`），下次启动 `EnsureCreated()` 重建含 `AuditLogs` 表。
2. 无线上环境，无回滚需求；如需回退，删除 `AuditLog` 相关代码并再次删库即可。

## Open Questions

- 无（设计决策已在探索阶段确认）。
