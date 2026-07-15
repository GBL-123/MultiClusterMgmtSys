## Why

现有认证体系完全自建：`Account` 实体 + `AccountRepository` + `AccountService.ValidateCredentialsAsync`（手动调 `PasswordHasher.VerifyHashedPassword`），登录端点自己拼 `ClaimsPrincipal` 后调 `HttpContext.SignInAsync`。`AppRole` 是手写枚举，`SeedAccountsAsync` 启动时仅在 `Accounts` 表为空时插入 admin / guest 两条记录。没有自助注册、没有账号管理 UI、不支持修改密码或角色。`Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.9` 已经在 csproj 里装着但完全没启用，浪费了基础设施。

本次重构切换到 ASP.NET Core Identity 框架（`AddIdentityCore` + EF Core Stores），把账号生命周期交由 `UserManager` / `SignInManager` / `RoleManager` 统一管理，并完成 `Guest → Member` 角色重命名。

## What Changes

- **BREAKING — 角色重命名**：`AppRole.Guest` 枚举被 `IdentityRole` 字符串角色名 `Member` 取代，行为保持"只读"。所有 `<AuthorizeView Roles="Guest">` / `Roles="Member"` 引用统一改名为 `Roles="Member"`；种子账号 `guest` → `member`（同密码 `Changeme_123`）。
- **BREAKING — 数据模型替换**：自定义 `Account` 实体（`int Id`、`Username`、`PasswordHash`、`AppRole Role`、`CreatedAt`）整张表（`Accounts`）被 ASP.NET Core Identity 的 `AspNetUsers` 替换。`AppRole.cs` 删除。新增 `ApplicationUser : IdentityUser<int>`（带 `DisplayName`、`CreatedAt`、`UpdatedAt`），`AppDbContext` 改为继承 `IdentityDbContext<ApplicationUser, IdentityRole<int>>`。
- **BREAKING — 表集合重构**：EF Core 自动创建 `AspNetUsers` / `AspNetRoles` / `AspNetUserRoles` / `AspNetUserClaims` / `AspNetUserLogins` / `AspNetUserTokens` / `AspNetRoleClaims` 等 Identity 标准表。原 `AccountRepository` 删除。
- **BREAKING — 认证装配切换**：`AddAuthentication().AddCookie()` 被 `AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager().AddPasswordValidator<AlphanumericPasswordValidator>()` 取代；`AuthService` 与 `PasswordHasher<string>` 显式注册全部移除。`SignInManager` / `UserManager` / `RoleManager` 自动按 Scoped 注入。
- **Member 自助注册**：新增 `/register` 页面 + `POST /api/register` 端点，未登录访客可注册 Member 账号。`UserManager.CreateAsync` 返回 `IdentityResult`，端点把 `DuplicateUserName` 映射到 `error=duplicate`，密码校验失败映射到 `error=weakpwd`。注册成功 → `SignInManager.SignInAsync` → `Results.LocalRedirect("/")`。
- **Admin 账号管理**：新增 `/accounts` 管理页面（仅 Admin 可见），含列表、新建、编辑、删除、重置密码。页面直接注入 `AccountService`（薄封装 `UserManager` + `RoleManager`），不走 HTTP 端点，与现有 `ClusterService` 同层模式。
- **自助资料管理**：新增 `/profile` 页面，任何已登录用户可改自己的显示名与密码。`AccountService.UpdateProfileAsync` / `ChangePasswordAsync` 直接被页面调用，无需最小 API 端点。
- **自定义密码强度**：实现 `IPasswordValidator<ApplicationUser>` 接口的 `AlphanumericPasswordValidator`，强制长度 ≥ 8 + 至少 1 字母 + 至少 1 数字，通过 `AddPasswordValidator<>` 接入 Identity 校验管线；规则集中一处，注册 / 新建 / 重置 / 自助改密共享。
- **登录流程行为不变**：`/api/login` 端点改用 `SignInManager.PasswordSignInAsync`，输入 / 错误码 / 重定向行为与现状对齐。`/api/logout` 改用 `SignInManager.SignOutAsync`。
- **导航新增**：Drawer 增加 "Accounts" 入口（`AuthorizeView Roles="Admin"` 包裹）。
- **`_Imports.razor` 同步**：新增 `Pages/Accounts/`、`Pages/Profile/`、`Pages/Auth/` 三个子目录的 `@using`。
- **Cookie 登录路径保持 `/login`**：`IdentityOptions` + `OnRedirectToLogin` 显式覆盖 Identity 默认的 `/Account/Login`，保留现有 `returnUrl` 行为。

## Capabilities

### New Capabilities

- `account-management`: Member 自助注册、Admin 后台账号 CRUD（列表 / 新建 / 编辑 / 删除 / 重置密码）、用户自助资料与密码修改、密码强度策略（`AlphanumericPasswordValidator`）、用户 / 角色生命周期统一由 ASP.NET Core Identity 管理。

### Modified Capabilities

- `authentication`: 角色从枚举 `AppRole.Guest` 改为 Identity 字符串角色名 `Member`、种子账号 `guest` → `member` 重命名、`AppBar` 用户名与登出按钮逻辑保持不变、登录端点从自建 `SignInAsync(claims)` 切到 `SignInManager.PasswordSignInAsync`。

## Impact

- **代码**：
  - `Models/Account.cs`（删除）→ `Models/ApplicationUser.cs` 新增（`class ApplicationUser : IdentityUser<int>`，含 `DisplayName` / `CreatedAt` / `UpdatedAt`）
  - `Models/AppRole.cs`（删除，替换为 Identity 内置 `IdentityRole<int>`）
  - `Daos/AppDbContext.cs` 改为 `IdentityDbContext<ApplicationUser, IdentityRole<int>>`，`Accounts` DbSet 删除；`OnModelCreating` 调 `base.OnModelCreating(modelBuilder)` 后再加 Cluster 配置 + `ApplicationUser` 字段索引
  - `Daos/AccountRepository.cs`（删除，UserManager 取代）
  - `Services/AccountService.cs` 大幅重写：构造函数变为 `(UserManager<ApplicationUser> users, RoleManager<IdentityRole<int>> roles, SignInManager<ApplicationUser> signIn, AppDbContext db, ILogger<AccountService> logger)`，方法集合见 design D10
  - `Services/AuthService.cs`（删除）
  - `Services/Identity/AlphanumericPasswordValidator.cs`（新增）实现 `IPasswordValidator<ApplicationUser>`
  - `Components/Pages/Login.razor`（不动文件，但端点行为由 SignInManager 接管）
  - `Components/Pages/Auth/Register.razor`（新增）
  - `Components/Pages/Accounts/Accounts.razor` + `Components/Pages/Accounts/AccountEditDialog.razor` + `Components/Pages/Accounts/ResetPasswordDialog.razor`（新增）
  - `Components/Pages/Profile/Profile.razor`（新增）
  - `Components/Layout/Drawer.razor`（新增 Admin-only "Accounts" 入口）
  - `Components/_Imports.razor`（三个新子目录的 `@using`）
  - `Program.cs`：`AddIdentityCore<...>().AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager().AddPasswordValidator<AlphanumericPasswordValidator>()`；配置 `IdentityOptions`（`Password`、`User`、`Lockout`、`SignIn`）；cookie 路径与 `OnRedirectToLogin` 覆盖；删除 `AddScoped<AccountRepository>()` / `<AuthService>()` / `<PasswordHasher<string>>()`；`/api/login` 端点改用 `SignInManager.PasswordSignInAsync`；新增 `/api/register` / `/api/logout` 改 `SignInManager.SignOutAsync`
- **数据**：开发期不写 EF 迁移，需删除 `MultiClusterMgmtSys/clusters.db*` 后重跑以应用新 schema。`Accounts` 表整张消失，改为 Identity 的 `AspNet*` 七张表。
- **安全**：密码走 Identity 内置 `PasswordHasher`（PBKDF2）+ 自定义 `AlphanumericPasswordValidator`；UserManager 强制 `UserName` 唯一；改密由 `UserManager.ChangePasswordAsync` 自动校验旧密码；登录失败由 `SignInManager` 返回 `SignInResult` 区分错误（`IsLockedOut` / `IsNotAllowed` / `Failed`），端点据此给出不同提示码。
- **依赖**：**无新增 NuGet 包**，`Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.9` 已在 csproj 中，本次只是启用。
- **种子**：启动时 `AccountService.SeedAccountsAsync` 检查 `IdentityRole` 表中是否存在 `Admin` / `Member` 角色、UserManager 中是否存在 `admin` / `member` 用户，缺失则用 `RoleManager.CreateAsync` 与 `UserManager.CreateAsync(user, "Changeme_123")` + `AddToRoleAsync` 创建。重启幂等。
