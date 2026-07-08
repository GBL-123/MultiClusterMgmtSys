## 1. 模型与数据层

- [x] 1.1 创建 `Models/AppRole.cs`（`enum AppRole { Admin, Guest }`）
- [x] 1.2 创建 `Models/Account.cs`（`Id`、`Username`、`PasswordHash`、`Role`、`CreatedAt`）
- [x] 1.3 `Daos/AppDbContext.cs` 新增 `DbSet<Account> Accounts` + `OnModelCreating` 配置（`Username` 唯一索引 + `PasswordHash` Required）
- [x] 1.4 创建 `Daos/AccountRepository.cs`（`GetByUsernameAsync`、`CountAsync`、`AddAsync`）

## 2. 服务层

- [x] 2.1 创建 `Services/AccountService.cs`（注入 `AccountRepository`、`PasswordHasher<string>`、`ILogger`）：`ValidateCredentialsAsync`、`CreateClaimsPrincipal`、`SeedAccountsAsync`
- [x] 2.2 `Program.cs` 注册 `AccountRepository`、`AccountService`、`PasswordHasher<string>`（Scoped）
- [x] 2.3 `Program.cs` 启动时在 `EnsureCreated()` 后调 `AccountService.SeedAccountsAsync()`

## 3. 认证基础设施

- [x] 3.1 `Program.cs` 注册 `AddAuthentication(Cookie)` + `AddCookie`（Cookie 名 `MultiClusterMgmtSys.Auth`、HttpOnly、SameSite Lax、8h 滑动续期、LoginPath `/login`、AccessDeniedPath `/access-denied`）+ `AddAuthorization` + `AddCascadingAuthenticationState`
- [x] 3.2 `Program.cs` 添加 `app.MapPost("/api/login", ...)`：接收 form（username/password/rememberMe/returnUrl），校验后 `SignInAsync` + `LocalRedirect(returnUrl)`，失败 `Redirect("/login?error=1&returnUrl=...")`
- [x] 3.3 `Program.cs` 添加 `app.MapGet("/api/logout", ...)`：`SignOutAsync` + `LocalRedirect("/login")`
- [x] 3.4 `Program.cs` 中间件顺序：`UseAuthentication` → `UseAuthorization` → `UseAntiforgery` → `MapRazorComponents`
- [x] 3.5 `Components/_Imports.razor` 新增 `@using Microsoft.AspNetCore.Authorization` + `@using Microsoft.AspNetCore.Components.Authorization`

## 4. 路由保护

- [x] 4.1 `Components/Routes.razor` 改用 `AuthorizeRouteView` + `<NotAuthorized>`（已登录角色不足 → 文案提示；未登录 → `<RedirectToLogin />`）
- [x] 4.2 创建 `Components/RedirectToLogin.razor`：`OnInitialized` 中 `NavigateTo("/login?returnUrl={encoded}", forceLoad: true)`

## 5. 登录页

- [x] 5.1 创建 `Components/Layout/EmptyLayout.razor`（仅 `MudThemeProvider` + providers + `@Body`，无 AppBar/Drawer）
- [x] 5.2 创建 `Components/Pages/Login.razor`：`@page "/login"` + `@layout EmptyLayout`，原生 `<form method="post" action="/api/login">` + 隐藏 input（returnUrl/username/password/rememberMe），MudBlazor 表单控件，错误提示由 query string `error=1` 触发
- [x] 5.3 `Login.razor` `OnInitializedAsync`：解析 returnUrl 与 error 参数；已登录则 `NavigateTo(returnUrl, forceLoad: true)` 跳走

## 6. AppBar 登出与用户名

- [x] 6.1 `Components/Layout/AppBar.razor` 新增 `AuthorizeView`：已登录时显示用户名（`@context.User.Identity?.Name`）+ 登出按钮（`NavigateTo("/api/logout", forceLoad: true)`）

## 7. 页面权限控制

- [x] 7.1 `Clusters.razor` 加 `@attribute [Authorize]`；"添加集群""新建分组""编辑""删除""刷新"按钮包 `<AuthorizeView Roles="Admin">`
- [x] 7.2 `ClusterDetail.razor` 加 `@attribute [Authorize]`；"编辑""删除""刷新""显示密文"按钮包 `<AuthorizeView Roles="Admin">`
- [x] 7.3 `Nodes.razor` 与 `NodeDetail.razor` 加 `@attribute [Authorize]`（只读页面，无 Admin 限制）

## 8. 验证

- [x] 8.1 删除 `clusters.db*` 重建库，`dotnet build` 通过
- [x] 8.2 未登录访问 `/clusters` → 跳转 `/login`
- [x] 8.3 admin/guest 均能登录并回跳
- [x] 8.4 guest 看不到修改类按钮，admin 全权限
- [x] 8.5 登出后回到登录页
- [x] 8.6 记住我勾选后关闭浏览器仍保持登录
