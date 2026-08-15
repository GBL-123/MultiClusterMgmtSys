## Why

系统目前没有任何可审计的操作记录：谁在什么时候对集群、分组、账号、配置做了变更，只能靠代码日志（ILogger）事后翻，且日志里没有操作人。需要一个面向 UI 的审计日志功能，让管理员可追溯全部操作、普通用户可查看自己的操作。

## What Changes

- 新增 `AuditLog` 实体与 `AuditLogs` 表（同一 SQLite 库，`EnsureCreated` 重建 schema，开发期删库可接受）。
- 新增 `AuditService`（含 `LogAsync` 写入口）与 `AuditLogRepository`（分页查询），各 Service 的变更操作（集群/分组/账号/配置/节点/认证事件）落审计记录。
- 新增"审计日志"页面 `/audit-logs`（MudTable 分页，沿用 `PagedResult<>` 模式），Drawer 导航入口对**所有登录用户可见**。
- 权限模型：Admin 查看全部日志并可筛选操作人；非 Admin（Member）只能看到自己账号的操作记录，过滤在服务/仓储层强制，不依赖 UI 隐藏。
- 记录内容：操作人（冗余存用户名）、类别、操作、目标；不记录详情（旧值/新值）、不记录密码等敏感信息。
- 记录范围：仅**人发起的变更操作与认证事件**（登录/登出/注册）；`RefreshStatus` 等自动状态刷新与启动播种不记。
- 日志为追加只读，不提供删除/清空功能。

## Capabilities

### New Capabilities
- `audit-log`: 操作日志的记录与查询能力（记录范围、审计事件写入、分页查询、按角色过滤、导航与页面呈现）

### Modified Capabilities
<!-- 无现有 spec 的 REQUIREMENTS 变更 -->

## Impact

- `Data/ApplicationDbContext.cs`：新增 `AuditLogs` DbSet 与实体配置（无迁移，`EnsureCreated` 重建，需删除本地 `MultiClusterMgmtSys.db`）。
- 新增文件：`Data/Entities/AuditLog.cs`、`Data/Repositories/AuditLogRepository.cs`、`Components/AuditLogs/{Services/ViewModels/Mappings/Pages}/**`、`Common/Enums/AuditCategory.cs`、`Common/Enums/AuditAction.cs`。
- 修改文件：`Program.cs`（注册 `AddHttpContextAccessor()`、`AuditService`、`AuditLogRepository`）、`Components/Layout/Drawer.razor`（新增"审计日志"导航入口）、六个既有 Service（`AuthService`、`AccountService`、`ClusterService`、`GroupService`、`ConfigMapService`、`ClusterNodeService`）的变更方法注入审计写入。
- 不引入新依赖；无数据库迁移；UI 文案保持中文。
