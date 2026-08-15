## 1. 数据层与枚举

- [x] 1.1 新增 `Common/Enums/AuditCategory.cs`：Authentication(认证)/Account(账号)/Cluster(集群)/Group(分组)/Configmap(配置)/Node(节点)
- [x] 1.2 新增 `Common/Enums/AuditAction.cs`：Login/Logout/Register/Create/Update/Delete/Move/Rename
- [x] 1.3 新增 `Data/Entities/AuditLog.cs`：Id、UserName(string?)、Category、Action、Target(string)、CreatedAt(DateTime)
- [x] 1.4 `ApplicationDbContext` 增加 `AuditLogs` DbSet，并在 `OnModelCreating` 配置实体（UserName 可空、Target 必填）
- [x] 1.5 新增 `Data/Repositories/AuditLogRepository.cs`：`AddAsync(AuditLog)`、`GetPagedAsync(TableState, AuditLogQueryRequest, string? currentUserName, bool isAdmin)`（非 Admin 强制 `UserName == currentUserName` 过滤，时间倒序分页，返回总数）

## 2. 审计服务

- [x] 2.1 新增 `Components/AuditLogs/Services/AuditService.cs`：注入 `ApplicationDbContext`（或 `AuditLogRepository`）、`IHttpContextAccessor`、`ILogger<AuditService>`；提供 `LogAsync(AuditCategory, AuditAction, string target, string? userName = null)` 重载（显式 user 用于认证事件；默认从 HttpContext 解析）；内部 try/catch，失败仅 `LogWarning` 不抛出
- [x] 2.2 `Program.cs`：注册 `builder.Services.AddHttpContextAccessor()`、`AddScoped<AuditLogRepository>()`、`AddScoped<AuditService>()`

## 3. Service 落点接入（主操作成功后写审计）

- [x] 3.1 `AuthService`：RegisterAsync 成功→注册；LoginAsync 成功→登录；LogoutAsync→登出（均显式传用户名）
- [x] 3.2 `AccountService`：CreateAccountAsync→创建账号；UpdateAccountAsync→修改账号；DeleteAccountAsync→删除账号；BatchDeleteAsync→批量删除账号；BatchUpdateRoleAsync→批量修改角色；ResetPasswordAsync→重置密码；ChangePasswordAsync→修改密码；UpdateProfileAsync→修改资料
- [x] 3.3 `ClusterService`：AddClusterAsync→创建集群；UpdateClusterAsync→修改集群；DeleteClusterAsync→删除集群；UpdateClusterEndpointsAsync→更新端点（不接入 RefreshStatus/RefreshAllClustersStatus）
- [x] 3.4 `GroupService`：AddGroupAsync→创建分组；RenameGroupAsync→重命名分组；DeleteGroupAsync→删除分组；MoveClustersToGroupAsync→移动集群
- [x] 3.5 `ConfigMapService`：CreateConfigMapFromYamlAsync→创建配置；UpdateConfigMapFromYamlAsync→修改配置；DeleteConfigMapAsync→删除配置
- [x] 3.6 `ClusterNodeService`：UpdateNodeIpNotesAsync→更新节点 IP 备注

## 4. 查询与页面

- [x] 4.1 新增 `Components/AuditLogs/Requests/AuditLogQueryRequest.cs`：可选 `SearchName`（Admin 筛选操作人）
- [x] 4.2 新增 `Components/AuditLogs/ViewModels/AuditLogViewModel.cs` 与 `ViewModels/Mappings/AuditLogMappingExtensions.cs`（枚举→中文显示映射、时间格式化）
- [x] 4.3 新增 `Components/AuditLogs/Pages/AuditLogs.razor`：路由 `/audit-logs`，MudTable 分页（沿用 Accounts 页 `PagedResult<>` + `TableState` 模式），列：时间/操作人/类别/操作/目标；Admin 显示操作人筛选框，Member 不显示；页面标题"审计日志"
- [x] 4.4 `Drawer.razor` 在"账号管理"之后新增"审计日志"导航入口（所有登录用户可见，`Icons.Material.Filled.History`）

## 5. 验证

- [x] 5.1 删除本地 `MultiClusterMgmtSys.db`（含 `-shm`/`-wal`），`dotnet build MultiClusterMgmtSys.slnx` 通过后启动，确认 `AuditLogs` 表由 `EnsureCreated()` 自动创建
- [ ] 5.2 以 admin 登录执行若干操作（登录/登出、创建集群、重命名分组、创建账号、修改配置、更新节点备注），在审计日志页确认全部可见且按时间倒序
- [ ] 5.3 以 Member 账号登录审计日志页，确认仅见自身记录且无操作人筛选框；尝试筛选也不返回他人记录
