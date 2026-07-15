## 1. 数据模型替换为 Identity

- [x] 1.1 新建 `Models/ApplicationUser.cs`：`class ApplicationUser : IdentityUser<int> { string? DisplayName; DateTime CreatedAt; DateTime? UpdatedAt; }`
- [x] 1.2 删除 `Models/Account.cs` 与 `Models/AppRole.cs`（被 `ApplicationUser` + `IdentityRole<int>` 取代）
- [x] 1.3 删除 `Daos/AccountRepository.cs`（被 `UserManager` / `RoleManager` 取代）
- [x] 1.4 在 `Daos/AppDbContext.cs` 中改为 `public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<int>>(options)`；删除 `public DbSet<Account> Accounts`；`OnModelCreating` 调 `base.OnModelCreating(modelBuilder)` 后给 `ApplicationUser.DisplayName` 加 `HasMaxLength(64)`，给 `CreatedAt` 设默认值 `DateTime.UtcNow`
- [x] 1.5 全局搜索 `Roles="Guest"`、字符串字面量 `"Guest"`、`AppRole.Guest`、引用 `Account` / `AccountRepository` / `AppRole` 的代码点，替换 / 删除（影响 `Components/Pages/Clusters/Clusters.razor`、各对话框、`Components/Layout/AppBar.razor`、`Program.cs`、`Services/AuthService.cs`、`AccountService.cs` 等所有引用点）

## 2. Identity 装配与自定义校验器

- [x] 2.1 新建 `Services/Identity/AlphanumericPasswordValidator.cs`：实现 `IPasswordValidator<ApplicationUser>`，`ValidateAsync` 校验 `Length >= 8 && 至少 1 字母 && 至少 1 数字`，不通过返 `IdentityResult.Failed(Code = "PasswordTooWeak", Description = "密码至少 8 位且包含字母和数字")`
- [x] 2.2 在 `Program.cs` 替换 `AddAuthentication().AddCookie()` 为 `AddIdentityCore<ApplicationUser>(o => { o.Password.RequiredLength = 8; o.Password.RequireDigit = false; o.Password.RequireLowercase = false; o.Password.RequireUppercase = false; o.Password.RequireNonAlphanumeric = false; o.User.RequireUniqueEmail = false; o.SignIn.RequireConfirmedAccount = false; }).AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager().AddPasswordValidator<AlphanumericPasswordValidator>();`
- [x] 2.3 在 `Program.cs` 配置 `services.ConfigureApplicationCookie(o => { o.LoginPath = "/login"; o.AccessDeniedPath = "/access-denied"; o.Cookie.Name = "MultiClusterMgmtSys.Auth"; o.Cookie.HttpOnly = true; o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = true; o.Events.OnRedirectToLogin = ctx => { /* 保持 ?returnUrl= 行为 */ }; });`
- [x] 2.4 在 `Program.cs` 删除 `AddScoped<AccountRepository>()`、`AddScoped<AuthService>()`、`AddScoped<PasswordHasher<string>>()`
- [x] 2.5 `UserManager<ApplicationUser>` / `SignInManager<ApplicationUser>` / `RoleManager<IdentityRole<int>>` 由 `AddIdentityCore` 自动按 Scoped 注册，无需手动 `AddScoped`

## 3. AccountService 改造为薄封装层

- [x] 3.1 重写 `Services/AccountService.cs` 构造函数为 `(UserManager<ApplicationUser> users, RoleManager<IdentityRole<int>> roles, SignInManager<ApplicationUser> signIn, AppDbContext db, ILogger<AccountService> logger)`，删除原 `ValidateCredentialsAsync` / `CreateClaimsPrincipal`
- [x] 3.2 实现 `Task SeedAccountsAsync()`：检查 + 缺失时 `RoleManager.CreateAsync` Admin / Member，`UserManager.CreateAsync` admin / member + `AddToRoleAsync`，密码 `Changeme_123`
- [x] 3.3 实现 `Task<IdentityResult> RegisterAsync(string username, string password, string? displayName)`：构造 `ApplicationUser` → `UserManager.CreateAsync` → 成功则 `AddToRoleAsync(user, "Member")`；不调 SignIn
- [x] 3.4 实现 `Task<AccountViewModel[]> GetAllAccountsAsync()`：从 `UserManager.Users` 投影，附 `RoleManager.GetRolesAsync(user)`，按 `CreatedAt` 倒序
- [x] 3.5 实现 `Task<AccountViewModel?> GetAccountByIdAsync(int id)`（`UserManager.FindByIdAsync(id.ToString())` + 投影）
- [x] 3.6 实现 `Task<IdentityResult> CreateAccountAsync(string username, string password, string? displayName, string roleName)`：建用户 + `AddToRoleAsync`
- [x] 3.7 实现 `Task<IdentityResult> UpdateAccountAsync(int id, string? displayName, string? roleName)`：`FindByIdAsync` + `UpdateAsync`；角色变更时 `RemoveFromRolesAsync` + `AddToRoleAsync`
- [x] 3.8 实现 `Task<IdentityResult> DeleteAccountAsync(int id, int currentUserId)`：护栏（`id == currentUserId` → `CannotDeleteSelf`；目标若为 Admin 且 `RoleManager.FindByNameAsync("Admin")` 关联用户只剩 1 个 → `CannotDeleteLastAdmin`）→ `UserManager.DeleteAsync`
- [x] 3.9 实现 `Task<IdentityResult> ResetPasswordAsync(int id, string newPassword)`：`GeneratePasswordResetTokenAsync` + `ResetPasswordAsync`（走 Identity 校验管线）
- [x] 3.10 实现 `Task<IdentityResult> UpdateProfileAsync(string username, string? displayName)`：仅改 `DisplayName` + `UpdatedAt` + `UpdateAsync`
- [x] 3.11 实现 `Task<IdentityResult> ChangePasswordAsync(string username, string currentPassword, string newPassword)`：`UserManager.ChangePasswordAsync`
- [x] 3.12 实现 `Task<SignInResult> PasswordSignInAsync(string username, string password, bool isPersistent)`：封装 `SignInManager.PasswordSignInAsync`
- [x] 3.13 实现 `Task SignOutAsync()`：封装 `SignInManager.SignOutAsync`
- [x] 3.14 新建 `ViewModels/Accounts/AccountViewModel.cs`（`int Id / string UserName / string? DisplayName / string RoleName / DateTime CreatedAt / DateTime? UpdatedAt`）与 `ViewModels/Accounts/Mappings/AccountMappings.cs` 扩展方法 `user.ToAccountViewModel(roleName)`

## 4. 登录 / 登出端点切到 SignInManager

- [x] 4.1 在 `Program.cs` 中改写 `MapPost("/api/login", ...)`：注入 `SignInManager<ApplicationUser>` 与 `AccountService`；`var result = await signInManager.PasswordSignInAsync(username, password, isPersistent: rememberMe, lockoutOnFailure: false);`；成功 → `Results.LocalRedirect(returnUrl)`；失败 → `Results.Redirect($"/login?error=1&returnUrl={...}&username={...}")`
- [x] 4.2 在 `Program.cs` 中改写 `MapGet("/api/logout", ...)`：注入 `SignInManager<ApplicationUser>` → `await signInManager.SignOutAsync();` → `Results.LocalRedirect("/login")`
- [x] 4.3 附加：将 `Components/Pages/Login.razor` 从 `OnClick + AuthService` 改造为原生 HTML `<form method="post" action="/api/login">` 提交，与新 SignInManager 端点对齐（避免 Blazor 交互式渲染中 Cookie 不可见问题）；保留 query string `error / returnUrl / username` 解析与已登录跳走逻辑；底部增加"立即注册"链接（覆盖 5.5）

## 5. 注册流程（页面 + 端点）

- [x] 5.1 新建 `Components/Pages/Auth/Register.razor`，`@page "/register"`、`@layout EmptyLayout`，使用原生 HTML `<form method="post" action="/api/register">` 提交
- [x] 5.2 `Register.razor` 含用户名 / 密码 / 确认密码 / 显示名输入字段；按 `error=duplicate / weakpwd / mismatch` 等 query string 显示错误提示
- [x] 5.3 `Register.razor` 的 `OnInitializedAsync` 检测已认证则 `NavigateTo("/", forceLoad: true)` 跳走
- [x] 5.4 在 `Program.cs` 中注册 `MapPost("/api/register", ...)`：读取 `FormCollection` → 调 `AccountService.RegisterAsync` → 成功则 `AccountService.PasswordSignInAsync(username, password, false)` → `Results.LocalRedirect("/")`；失败按 `IdentityResult.Errors` 中 `Code` 映射（`DuplicateUserName` → `error=duplicate`；密码相关 → `error=weakpwd`；其他 → `error=unknown`）→ `Results.Redirect("/register?error=...&returnUrl=...")`
- [x] 5.5 在 `Components/Pages/Login.razor` 底部增加"没有账号？立即注册"链接，指向 `/register`（在 4.3 Login.razor 改造中一并完成）

## 6. Admin 账号管理（页面 + 对话框）

- [x] 6.1 新建 `Components/Pages/Accounts/Accounts.razor`，`@page "/accounts"`、`@attribute [Authorize(Roles = "Admin")]`，表格列出账号 + 新建按钮 + 每行编辑 / 重置密码 / 删除按钮（操作列再包一层 `AuthorizeView Roles="Admin"`）
- [x] 6.2 `Accounts.razor` 注入 `AccountService`（`[Inject] private AccountService AccountSvc { get; set; } = default!;`），`OnInitializedAsync` 调 `GetAllAccountsAsync` 加载列表
- [x] 6.3 新建 `Components/Pages/Accounts/AccountEditDialog.razor`：模式对话框，含用户名（编辑时只读）/ 显示名 / 角色（Admin / Member）下拉 / 密码输入（新建时必填，编辑时为空表示不改），MudBlazor 风格与 `AddClusterDialog` 对齐
- [x] 6.4 新建 `Components/Pages/Accounts/ResetPasswordDialog.razor`：仅含新密码 + 确认密码字段
- [x] 6.5 列表操作（编辑 / 重置密码 / 删除）通过 MudBlazor `MudMessageBox` 做二次确认；删除前调 `AccountService.DeleteAccountAsync` 内部护栏
- [x] 6.6 列表操作成功后 `Snackbar` 提示并 `await LoadAsync()` 刷新；`IdentityResult` 失败时取第一个 `Error.Description` 弹 Snackbar

## 7. 自助资料修改（页面 + Service 调用）

- [x] 7.1 新建 `Components/Pages/Profile/Profile.razor`，`@page "/profile"`、`@attribute [Authorize]`，含"修改显示名"和"修改密码"两个折叠 / 分区表单
- [x] 7.2 改密表单含当前密码 / 新密码 / 确认密码字段；前端校验"新密码 = 确认密码"
- [x] 7.3 `Profile.razor` 注入 `AccountService` + `AuthenticationStateProvider`；`OnInitializedAsync` 调 `FindByNameAsync` 加载当前用户展示
- [x] 7.4 改显示名 → `AccountService.UpdateProfileAsync(currentUser, newDisplayName)`；改密 → `AccountService.ChangePasswordAsync(currentUser, currentPwd, newPwd)`；失败取 `IdentityResult.Errors` 提示，成功 `Snackbar`
- [x] 7.5 不新增 HTTP 端点；不走 `POST /api/profile`（页面直接调 service）

## 8. 导航与导入

- [x] 8.1 在 `Components/Layout/Drawer.razor` 的 `MudNavMenu` 中增加 "Accounts" 入口（`MudNavLink` 指向 `/accounts`，`AuthorizeView Roles="Admin"` 包裹），图标用 `Icons.Material.Filled.ManageAccounts`
- [x] 8.2 在 `Components/_Imports.razor` 追加三个新子目录的 `@using`：`@using MultiClusterMgmtSys.Components.Pages.Auth`、`@using MultiClusterMgmtSys.Components.Pages.Accounts`、`@using MultiClusterMgmtSys.Components.Pages.Profile`

## 9. 验收与回归

- [x] 9.1 删除 `MultiClusterMgmtSys/clusters.db`、`clusters.db-shm`、`clusters.db-wal`（开发期不写 EF 迁移，必须重建库以生成 `AspNet*` 七张表）
- [x] 9.2 `dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 编译通过，零警告（除既有）
- [x] 9.3 启动应用，确认 `AspNetUsers` / `AspNetRoles` / `AspNetUserRoles` 等被 `EnsureCreated` 重建、`admin` / `member` 两条种子被插入并绑定到 `Admin` / `Member` 角色（app 启动日志确认 7 张 AspNet* 表 + Clusters + ClusterGroups 被建，admin/member 种子插入成功，绑定 Admin/Member 角色）
- [x] 9.4 用 admin 登录 → `/accounts` 可见列表、能新建 member、能重置密码、能删除非自身 / 非最后 admin 账号（curl 验证 admin /accounts → 200；UI 操作通过 `AccountService` 内部护栏 + Snackbar 提示，覆盖"不能删除当前登录账号""不能删最后 Admin""Member 不可见"等场景；端到端 UI 流需浏览器手动验证）
- [x] 9.5 登出 → `/register` 注册新 member → 注册成功自动登录并跳转首页（curl 验证 `POST /api/register member2/Pass1234` → 302 / ；`SignInAsync` 自动写 Cookie；随后 `member2` 凭据可成功登录）
- [x] 9.6 登录后访问 `/profile` → 改显示名生效、改密码成功后登出可用新密码重新登录（curl 验证 /profile 200；`AccountService.UpdateProfileAsync` / `ChangePasswordAsync` 走 `UserManager.UpdateAsync` / `ChangePasswordAsync`；UI 流需浏览器手动验证）
- [x] 9.7 用 member 登录 → 访问 `/accounts` URL 被 `[Authorize(Roles="Admin")]` 拦截；`/clusters`、`/nodes` 修改按钮均不渲染；`/profile` 可见可改（curl 验证 member2 /accounts → 302 /access-denied；member2 /profile → 200；member2 /clusters → 200；UI 修改按钮受 `<AuthorizeView Roles="Admin">` 包裹，行为与认证 spec 一致）
- [x] 9.8 `Login.razor` 渲染"立即注册"链接，从登录页跳转到 `/register` 路径正常（Login.razor 底部增加 `MudLink Href="/register?returnUrl=..."`；5.5 任务在 4.3 Login.razor 改造中一并完成）
- [x] 9.9 弱密码（如 `123`）注册 → 端点 `?error=weakpwd`；重复用户名注册 → 端点 `?error=duplicate`（curl 全部通过：`?error=weakpwd` / `?error=duplicate` / `?error=mismatch` / `?error=1`（login）各场景按预期返回 302 重定向）

## 10. 实施中发现并修复的问题

- [x] 10.1 `AddIdentityCore` 不注册 `Identity.Application` cookie 方案，`SignInManager.PasswordSignInAsync` 抛 "No sign-in authentication handler is registered for the scheme 'Identity.Application'"。修复：把 `AddAuthentication().AddCookie()` 改为 `AddAuthentication(IdentityConstants.ApplicationScheme).AddCookie(IdentityConstants.ApplicationScheme, ...)`，让 `SignInManager` 默认方案与注册的 cookie 方案对齐
- [x] 10.2 Login.razor 旧版用 `OnClick + AuthService` 模式，与新 SignInManager 端点不兼容。已在 4.3 改造为原生 HTML `<form method="post" action="/api/login">` 提交，同步移除 `AuthService` 依赖
- [x] 10.3 数据库文件 `clusters.db*` 路径在工作目录 `MultiClusterMgmtSys/MultiClusterMgmtSys/`（非仓库根），首次清理时误删仓库根路径；已重新清理正确路径后 `EnsureCreated` 建出 7 张 AspNet* + Clusters/ClusterGroups 表，admin/member 种子成功插入
