## Context

现状：账号体系完全自建。`Account` 实体（`int Id / string Username / string PasswordHash / AppRole Role / DateTime CreatedAt`）由 `AccountRepository` 走 EF Core 直接读写；`AccountService.ValidateCredentialsAsync` 拿到 `Account` 后手动调 `PasswordHasher<string>.VerifyHashedPassword` 验证，再自己拼 `ClaimsPrincipal`（`ClaimTypes.Name / .NameIdentifier / .Role`），最后 `HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal)` 写 Cookie。`AppRole` 枚举只有 `Admin / Guest`；`SeedAccountsAsync` 在 `Accounts.CountAsync() == 0` 时插入 admin + guest 两条种子。`Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.9` 已在 csproj 中但完全未启用（无 `AddIdentityCore` / `IdentityDbContext` / `UserManager` 注入）。

约束：
- 切到 ASP.NET Core Identity 后，账号表从 `Accounts` 变为 `AspNetUsers`（主键 `int` via `IdentityUser<int>`），主键类型与现有 `int Id` 对齐。
- 继续 Cookie 认证，登录路径保持 `/login`（Identity 默认 `/Account/Login` 必须显式覆盖）。
- 开发期不写 EF 迁移；`EnsureCreated` 不会迁移已存在库 → 必须删 `clusters.db*` 后重跑以生成 `AspNet*` 七张表。
- 登录 / 登出端点必须在最小 API 中通过 `HttpContext` 调 `SignInManager`（Blazor 交互式 Server 渲染里 `HttpContext` 为 null，Cookie 写入不可用）。注册端点同理（要 `SignInManager.SignInAsync`）。
- Admin 后台与自助资料页面在 Blazor Server 中是同进程，可直接注入 `UserManager` / `SignInManager` / `RoleManager`（不再走 HTTP 端点 + HttpClient）。
- 现有 `<AuthorizeView Roles="Guest">` 包装的"修改类按钮"统一改名为 `Roles="Member"`，角色名仍是字符串（`IdentityRole.Name`），与 `Identity` 自动产出的 `ClaimTypes.Role` 兼容。
- 项目无测试 / 无 lint / 无 CI；变更后靠 `dotnet build` + 手动操作对应页面验证。

## Goals / Non-Goals

**Goals:**
- 启用 ASP.NET Core Identity：`AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager()`，把 `Account` / `AccountRepository` / `AppRole` / `AuthService` 全部清掉，让 `UserManager` / `SignInManager` / `RoleManager` 成为账号生命周期的唯一入口。
- 角色重命名 `Guest → Member`（语义对齐产品命名，行为保持只读）：`IdentityRole.Name = "Member"`，`AspNetUserRoles` 表自动承载。
- Member 自助注册：注册时 `UserManager.CreateAsync(user, password)` 成功后立即 `AddToRoleAsync(user, "Member")` + `SignInManager.SignInAsync(user, isPersistent: false)`。
- Admin 账号管理：新增 `/accounts`，由 Admin 通过 `UserManager` / `RoleManager` 治理账号生命周期。
- 自助资料修改：新增 `/profile`，通过 `UserManager.UpdateAsync` / `ChangePasswordAsync` 改自己的显示名 / 密码。
- 保留种子 admin + member 账号（`Changeme_123`）作为开发期默认凭据，重启幂等；账号与角色在缺失时自动 seed。
- 维持现有 Cookie 登录 / 登出 / 路由保护机制不变，只在端点内部把 `HttpContext.SignInAsync(claims)` 切到 `SignInManager.PasswordSignInAsync` / `SignOutAsync`。

**Non-Goals:**
- 邮箱 / 手机号验证、找回密码、SSO / OAuth、多租户、密码定期强制轮换、审计日志、并发会话管理、2FA、登录失败锁定 — 本次重构不引入。
- 不写 EF 迁移；不改 `EnsureCreated()` 启动逻辑。
- 不引入新 NuGet 依赖（`Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.9` 已装）。
- 不改 `AppBar` 右侧用户信息渲染与登出按钮的交互；不引入 Identity 默认 UI scaffold。

## Decisions

### D1. 角色重命名 `AppRole.Guest` → Identity 字符串角色名 `Member`

**Why**：`AppRole` 枚举整张被 Identity 取代，角色名改为 `IdentityRole.Name` 字符串（"Admin" / "Member"），与 Identity 自动写入的 `ClaimTypes.Role` 对齐，`IsInRole("Member")` / `<AuthorizeView Roles="Member">` 行为不变。

**Alternatives considered**：
- 保留 `AppRole` + 单独的 `IdentityRole` 双轨映射：拒绝。`Roles="..."` 引用无法两边都跟，且类型转换无意义。
- 用 "Viewer" 命名：拒绝。用户在需求里明确说 "member"。

**Impact**：所有 `Roles="Guest"`（`Clusters.razor`、对话框、`AppBar` 等）改为 `Roles="Member"`；`Models/AppRole.cs` 整文件删除；种子账号 `guest` → `member`（`IdentityRole.Name = "Member"`）。`EnsureCreated` 不会改已存在库 → 必须删 `clusters.db*` 重跑以建 `AspNet*` 表。

### D2. 注册走最小 API 端点 `POST /api/register`

**Why**：与 `POST /api/login` 保持一致 — 注册后要立即 `SignInManager.SignInAsync` 写 Cookie，必须在最小 API 里有 `HttpContext`。

**How**：
- 表单用原生 HTML `<form method="post" action="/api/register">`（与 `Login.razor` 同风格）。
- 端点流程：读 `FormCollection` → `AccountService.RegisterAsync(username, password, displayName)` 内部 `UserManager.CreateAsync` → `AddToRoleAsync("Member")` → `SignInManager.SignInAsync(user, isPersistent: false)`。
- 失败映射：`IdentityResult.Errors` 中 `Code = "DuplicateUserName"` → `?error=duplicate`；密码相关 code（`PasswordTooShort` / `PasswordRequiresDigit` / `PasswordRequiresLetter` 等）→ `?error=weakpwd`；其他 → `?error=unknown`。
- 成功：`Results.LocalRedirect("/")` 整页跳转，新电路读 Cookie 已认证。

### D3. Admin 账号管理走独立页面 `/accounts` + 同一进程服务调用

**Why**：账号治理是高频运维操作，必须有专页 + 列表 / 编辑对话框；Blazor Server 是同进程，直接注入 `UserManager` / `RoleManager` / `AccountService` 比绕一圈 HTTP + JSON 简单很多，与现有 `ClusterService` / `GroupService` 同模式。

**How**：
- 页面 `Components/Pages/Accounts/Accounts.razor`：`@attribute [Authorize(Roles = "Admin")]` 路由级保护 + 页面内 `<AuthorizeView Roles="Admin">` 包住新建 / 编辑 / 删除按钮。
- 页面 `OnInitializedAsync` 调 `AccountService.GetAllAccountsAsync()` 拿 `AccountViewModel[]`；`CreatedAt` 倒序。
- 新建 / 编辑用 `AccountEditDialog.razor`（与 `AddClusterDialog` / `GroupEditDialog` 风格一致）；`AccountService.CreateAccountAsync` / `UpdateAccountAsync` 内部用 `UserManager.CreateAsync` + `AddToRoleAsync` / `RemoveFromRoleAsync` + `UpdateAsync`。
- 删除：`AccountService.DeleteAccountAsync` 先做护栏校验（不能删自己、不能删最后一个 Admin），再 `UserManager.DeleteAsync`。
- 重置密码：`ResetPasswordDialog.razor` 收集新密码 → `AccountService.ResetPasswordAsync` 内部用 `UserManager.RemovePasswordAsync` + `UserManager.AddPasswordAsync`（不走 token，Admin 直接覆盖）。

### D4. 自助资料修改走独立页面 `/profile`，不新增 HTTP 端点

**Why**：页面是 Blazor，同进程直接调 `UserManager` 比走 HTTP 简单；改密成功不需要新 Cookie（现有 Cookie 仍有效），`UserManager.ChangePasswordAsync` 自动校验旧密码。

**How**：
- 页面 `Components/Pages/Profile/Profile.razor`：`@attribute [Authorize]`，注入 `UserManager<ApplicationUser>` + `AuthenticationStateProvider`。
- 改显示名：`FindByNameAsync(currentUser)` → 修改 `DisplayName` → `UserManager.UpdateAsync`。
- 改密码：`UserManager.ChangePasswordAsync(user, currentPassword, newPassword)`；`IdentityResult` 失败时把 `Code = "PasswordMismatch"` 映射到 UI 错误。
- 失败信息：Snackbar 提示；成功 Snackbar 提示"资料已更新 / 密码已更新"，不强制重新登录。

### D5. `ApplicationUser : IdentityUser<int>` 替换 `Account`

**Why**：
- 切到 Identity 必须有 `IdentityUser` 派生类（自带 Id / UserName / PasswordHash / SecurityStamp 等字段）。
- 选 `IdentityUser<int>` 让主键保持 `int`，与现有 `Account.Id` 类型对齐，ViewModel / URL / 前端代码改动最小。
- `DisplayName` 用于 UI 展示（避免暴露登录用户名），`CreatedAt` / `UpdatedAt` 用于管理列表排序。

**How**：
- `Models/ApplicationUser.cs`：`class ApplicationUser : IdentityUser<int> { string? DisplayName; DateTime CreatedAt; DateTime? UpdatedAt; }`。
- `AppDbContext` 继承 `IdentityDbContext<ApplicationUser, IdentityRole<int>>`，`OnModelCreating` 调 `base.OnModelCreating` 后给 `DisplayName` 加 `HasMaxLength(64)`，`CreatedAt` 默认 `DateTime.UtcNow`（或 `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` 的默认值）。
- `EnsureCreated` 不迁移旧库 → 必须删 `clusters.db*`。

### D6. `AddIdentityCore` + 自定义 `IPasswordValidator`

**Why**：
- `AddIdentityCore` 只注册 Identity 的核心（user store、role store、user manager、role manager、sign-in manager、token providers），不挂默认 UI / 默认 EF 数据上下文之外的额外服务；与本项目自定义 Blazor UI 完全契合。
- 配 `AddRoles<IdentityRole<int>>` 启用角色；`AddEntityFrameworkStores<AppDbContext>` 把 store 接到现有 `AppDbContext`；`AddSignInManager` 提供 `SignInManager<ApplicationUser>`；`AddPasswordValidator<AlphanumericPasswordValidator>` 把自定义强度规则接入 UserManager 的校验管线。
- Identity 的内置 `IdentityErrorDescriber` 与 `PasswordOptions`（`RequiredLength` 等）只覆盖长度、不覆盖"必须含字母 + 数字"，所以自定义一个最小实现。

**Alternatives considered**：
- 用 `AddIdentity`（含默认 UI scaffold 与 default token provider）：拒绝。默认 UI scaffold 注册的路由会污染现有 `Login.razor` / `Register.razor`，且本项目没有用 Identity UI。
- 写一个手写 `IPasswordValidator` 加 `RequireNonAlphanumeric = false` 凑合：可，但需同时关掉 `RequireUppercase` / `RequireLowercase` / `RequireDigit` 等内置规则，配置散乱。直接一个 `IPasswordValidator` 更清晰。

**How**：
- `Services/Identity/AlphanumericPasswordValidator.cs`：`class AlphanumericPasswordValidator : IPasswordValidator<ApplicationUser>`，`ValidateAsync` 校验 `Length >= 8 && 至少 1 字母 && 至少 1 数字`，不通过返回 `IdentityResult.Failed(new IdentityError { Code = "PasswordTooWeak", Description = "..." })`。
- `Program.cs`：`builder.Services.AddIdentityCore<ApplicationUser>(o => { o.Password.RequiredLength = 8; o.Password.RequireDigit = false; o.Password.RequireLowercase = false; o.Password.RequireUppercase = false; o.Password.RequireNonAlphanumeric = false; o.User.RequireUniqueEmail = false; }).AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager().AddPasswordValidator<AlphanumericPasswordValidator>();`（内建 `Password` 选项放宽，让自定义 validator 单一来源）。
- `IdentityOptions.SignIn.RequireConfirmedAccount = false`；`IdentityOptions.Lockout` 暂不启用。

### D7. 登录端点切到 `SignInManager.PasswordSignInAsync`

**Why**：原 `AccountService.ValidateCredentialsAsync` 自建拼 `ClaimsPrincipal` 的逻辑等价于 `SignInManager.PasswordSignInAsync`；切到 Identity 后统一从 `SignInManager` 走，自动获取 `IdentityUser` 的 `Id`（NameIdentifier）、UserName、角色 Claim 等。

**How**：
- `/api/login` 端点：`SignInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false)`。
- 成功：`Results.LocalRedirect(returnUrl)`。
- 失败：`SignInResult` 返回 `IsNotAllowed` / `IsLockedOut` / `Failed`，当前只区分 `Failed` → `?error=1`（与现状一致）；预留 `IsLockedOut` 留作后续启用 `Lockout` 时映射 `?error=locked`。
- `/api/logout` 改用 `SignInManager.SignOutAsync()`（内部仍是清 Cookie）。

### D8. Identity schema 替换 `Accounts` 表

**Why**：`IdentityDbContext.OnModelCreating` 会建 `AspNetUsers` / `AspNetRoles` / `AspNetUserRoles` / `AspNetUserClaims` / `AspNetUserLogins` / `AspNetUserTokens` / `AspNetRoleClaims` 七张表；`EnsureCreated()` 在空库上一次性建齐。`Accounts` 表整张消失。

**Impact**：
- 现有 `Accounts` 表里 admin / guest 两行（PBKDF2 哈希）随 schema 重建丢失；`SeedAccountsAsync` 重新建 admin + member 账号，密码仍是 `Changeme_123`。
- `EnsureCreated` 不迁移已存在库 → 必须删 `clusters.db*` 后重跑。
- 任何依赖 `Account` 类型 / `AccountRepository` 的下游代码（暂无）需迁移到 `ApplicationUser` + `UserManager`；现有 `AccountService.ValidateCredentialsAsync` / `CreateClaimsPrincipal` 方法删除。

### D9. Cookie 登录路径覆盖：`/login`（保留现有行为）

**Why**：`AddIdentityCore` 默认 cookie `LoginPath = "/Account/Login"`、`AccessDeniedPath = "/Account/AccessDenied"`，与现有 `Login.razor` (`/login`) 不一致。

**How**：在 `Program.cs` 中通过 `services.ConfigureApplicationCookie(o => { o.LoginPath = "/login"; o.AccessDeniedPath = "/access-denied"; o.Events.OnRedirectToLogin = ctx => { /* 保留 returnUrl */ }; })` 覆盖。`OnRedirectToLogin` 重新写回 `?returnUrl={encoded}` query。

### D10. `AccountService` 变成薄封装层（不直接服务 HTTP 端点）

**Why**：
- 与现有 `ClusterService` / `GroupService` / `ConfigMapService` 模式一致：UI 层只与 `AccountService` 交互，service 负责协调 `UserManager` / `SignInManager` / `RoleManager` / `AppDbContext`。
- 避免页面直接耦合四个 Identity 服务的复杂签名；也方便在 service 内集中实现"不能删最后一个 Admin"等业务规则。

**How**：`AccountService` 公开方法：
- `Task SeedAccountsAsync()`：检查 + 缺失时 `RoleManager.CreateAsync` Admin / Member，`UserManager.CreateAsync` admin / member + `AddToRoleAsync`。
- `Task<IdentityResult> RegisterAsync(string username, string password, string? displayName)`：建用户 + 角色 + 返结果（不 SignIn，由端点调）。
- `Task<AccountViewModel[]> GetAllAccountsAsync()`：投影 `UserManager.Users` + roles。
- `Task<AccountViewModel?> GetAccountByIdAsync(int id)`。
- `Task<IdentityResult> CreateAccountAsync(string username, string password, string? displayName, string roleName)`。
- `Task<IdentityResult> UpdateAccountAsync(int id, string? displayName, string? roleName)`。
- `Task<IdentityResult> DeleteAccountAsync(int id, int currentUserId)`：先做护栏（`id == currentUserId` → 失败；目标若是 Admin 且系统只剩一个 Admin → 失败）。
- `Task<IdentityResult> ResetPasswordAsync(int id, string newPassword)`。
- `Task<IdentityResult> UpdateProfileAsync(string username, string? displayName)`。
- `Task<IdentityResult> ChangePasswordAsync(string username, string currentPassword, string newPassword)`。
- `Task<SignInResult> PasswordSignInAsync(string username, string password, bool isPersistent)`。
- `Task SignOutAsync()`。
- `Task EnsureAtLeastOneAdminAsync(int excludingUserId)`：删除前的护栏辅助。

页面 / 端点只注入 `AccountService`，不直接接触 `UserManager` / `SignInManager` / `RoleManager`（保持分层）。

### D11. Admin 删除护栏

**Why**：避免误操作导致系统无 Admin 账号可用，是账号管理的基本安全护栏。

**Where**：`AccountService.DeleteAccountAsync` 内部：拒绝 `id == currentUserId`（业务错误 "不能删除当前登录账号"）；拒绝目标用户是 Admin 且 `RoleManager` 中 "Admin" 角色下只剩 1 个用户（业务错误 "系统中必须至少保留一个 Admin 账号"）。`IdentityResult` 通过 `IdentityError` 表达。

### D12. 端到端认证矩阵保持 `<AuthorizeView Roles="Admin">` / `Roles="Member"`

**Why**：Identity 自动给 `ClaimsPrincipal` 注入 `ClaimTypes.Role`，`Roles="..."` 字符串匹配机制不变。现有 UI 包装模式（`AppBar`、`Clusters.razor`、各对话框）只做"角色名替换"，不引入新授权机制。

**AppBar 显示**：当前显示 `@context.User.Identity?.Name`（登录用户名）。如 `User.FindFirstValue("DisplayName")` 存在可优先显示（不强制要求本期实现，留 Open Question）。

## Risks / Trade-offs

- **[AspNet* 表 schema 替换导致旧数据丢失]**：`EnsureCreated` 不迁移已存在库 → 删 `clusters.db*` 后旧 admin/guest 账号及其数据全部丢失。**Mitigation**：dev 期不存业务数据，删除可接受；tasks 7.1 强制；handover 文档明确告知。
- **[角色重命名漏改]**：所有 `Roles="Guest"` 引用未同步会编译失败（硬错误），但 Razor 编译时是字面量，不会漏；任何字符串字面量 `"Guest"`（如 `RequireAssertion(ctx => ctx.User.IsInRole("Guest"))`）需全量替换为 `"Member"`。**Mitigation**：tasks 1.4 列"全局 grep `Roles="Guest"` 与字符串字面量 `Guest` 并替换"为强制项。
- **[AddIdentityCore vs AddIdentity]**：选 `AddIdentityCore` 不挂默认 UI 与默认 token provider（如 Email confirmation token）。当前不需要这些。**Mitigation**：若后续需要找回密码 / 邮件确认，迁到 `AddIdentity` 即可（一行配置）。
- **[Email / PhoneNumber 字段保留但未启用]**：`IdentityUser<int>` 自带 `Email` / `PhoneNumber` / `EmailConfirmed` / `TwoFactorEnabled` 等列。本次不消费这些字段，仅禁用相关验证（`RequireUniqueEmail = false`）。**Mitigation**：在 `ApplicationUser` 上不强制要求；后续可单独引入"找回密码"功能再用。
- **[登录失败无锁定 / 无邮箱验证]**：注册开放 + 无 lockout 有弱密码 / 撞库风险。**Mitigation**：D6 自定义密码强度 + 后续 `IdentityOptions.Lockout`（Open Question）；本次不启用。
- **[`SignInManager.PasswordSignInAsync` 与现有 ClaimsPrincipal 兼容]**：Identity 默认会把 `User.Id` 写为 `ClaimTypes.NameIdentifier`（值为 `int.ToString()`），`User.UserName` 写为 `ClaimTypes.Name`，每个 role 写一条 `ClaimTypes.Role`。这与原 `AccountService.CreateClaimsPrincipal` 行为完全等价。**Mitigation**：实测 `AppBar` 显示用户名、`AuthorizeView` 角色匹配仍正确。
- **[HttpClient 在 Blazor Server 里调同源最小 API 的 `Set-Cookie` 不可见]**：注册成功在最小 API 端点内 `SignInManager.SignInAsync` 直接写 Cookie，绕开 HttpClient；自助改密后 Cookie 无需变化。**Mitigation**：N/A。
- **[Admin CRUD 与自助资料改用 `UserManager` 直接注入，不再走 HTTP + JSON]**：与原 D3 / D4 的 HttpClient 方案不同；`AccountService` 仍按 ViewModel 投影。**Mitigation**：service 层把 `IdentityResult` 转 `IdentityError` 信息给 UI，UI 用 Snackbar 展示，不引入新序列化层。

## Migration Plan

无生产数据迁移（开发期不写 EF 迁移）。落地步骤：

1. 代码落地（`Models / Daos / Services / Program.cs / Components/Pages/*`）。
2. 删 `MultiClusterMgmtSys/clusters.db`、`clusters.db-shm`、`clusters.db-wal`。
3. `dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 验证编译通过。
4. `dotnet run --project MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 启动 → 验证 `AspNetUsers` / `AspNetRoles` / `AspNetUserRoles` 被 `EnsureCreated` 建出来，admin + member 两条种子被插入。
5. 用 admin 登录 → `/accounts` 可见列表、能新建 member、能重置密码、能删除非自身 / 非最后 admin 账号。
6. 登出 → `/register` 注册新 member → 注册成功自动登录。
7. 登录后访问 `/profile` → 修改显示名 / 密码成功，登出可用新密码重新登录。
8. 切换到新注册的 member → 访问 `/accounts` 应跳登录页 / 403；`/clusters`、`/nodes` 修改按钮不渲染。

回滚：把代码 revert 到变更前，删 `clusters.db*` 重建（admin + guest 种子会重新生成）。

## Open Questions

- **Q1**：Member 自助改密是否需要"旧密码二次验证"？当前用 `UserManager.ChangePasswordAsync`，其内部已经要求传入 `currentPassword` 并校验。**当前决策**：要；页面表单必填"当前密码"。
- **Q2**：注册是否需要 admin 审核 / 邮件激活？**当前决策**：不要，开放注册、注册成功即成为 Member（`SignInManager.SignInAsync` 立即生效）。
- **Q3**：是否启用 `IdentityOptions.Lockout`（连续 N 次失败锁定 X 分钟）？**当前决策**：本次不启用；后续可单独立项。
- **Q4**：账号管理列表是否需要搜索 / 过滤 / 分页？**当前决策**：先做最简列表 + 按 `CreatedAt` 倒序，量大了再加分页。
- **Q5**：`AppBar` 当前显示 `User.Identity.Name`（登录用户名），是否优先显示 `DisplayName`？**当前决策**：暂不实现；`IdentityUser` 的 `UserName` 已是 UI 友好字符串，必要时后续加 `FindFirstValue("DisplayName")` 优先。
- **Q6**：`AddIdentityCore` vs `AddIdentity` 选用理由（前者不挂默认 UI）；若后续要 Email confirmation token / 找回密码，是否升到 `AddIdentity`？**当前决策**：选 `AddIdentityCore`，最小启动；后续按需升级。
