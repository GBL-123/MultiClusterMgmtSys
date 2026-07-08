## Why

系统最初无任何访问控制，所有页面对匿名用户完全开放，任何人打开应用即可增删集群，存在凭据泄露风险。需要增加认证/授权基础设施：未登录用户跳转登录页，登录后按角色（Admin/Guest）决定可执行的操作。

## What Changes

- 新增 `Account` 实体与 `AppRole` 枚举（`Models/`），账号存 SQLite，密码用 `PasswordHasher<string>`（PBKDF2）哈希存储。
- 新增 `AccountRepository`（`Daos/`）与 `AccountService`（`Services/`），启动时种子 admin/guest 两个账号。
- `Program.cs` 注册 Cookie 认证（`AddAuthentication` + `AddCookie` + `AddAuthorization` + `AddCascadingAuthenticationState`），添加 `/api/login`（POST）与 `/api/logout`（GET）最小 API 端点。
- `Routes.razor` 改用 `AuthorizeRouteView` + `<NotAuthorized>` 分流：未登录 → `RedirectToLogin` 组件跳转 `/login`；已登录但角色不足 → 内联提示文案。
- 新增 `Login.razor`（`@layout EmptyLayout`，原生 HTML form POST 提交到 `/api/login`）与 `RedirectToLogin.razor`。
- 新增 `EmptyLayout.razor`（登录页专用，仅含 `MudThemeProvider` + providers，无 AppBar/Drawer）。
- `AppBar.razor` 新增登出按钮（`NavigateTo("/api/logout", forceLoad: true)`）与当前登录用户名显示。
- 所有需登录页面加 `@attribute [Authorize]`；修改类按钮用 `<AuthorizeView Roles="Admin">` 包裹（Guest 不可见）。

## Capabilities

### New Capabilities

- `authentication`: 账号模型、Cookie 认证、登录/登出流程、角色权限矩阵、页面级与组件级访问控制。

### Modified Capabilities

无。

## Impact

- **新增文件**：`Models/Account.cs`、`Models/AppRole.cs`、`Daos/AccountRepository.cs`、`Services/AccountService.cs`、`Components/Pages/Login.razor`、`Components/RedirectToLogin.razor`、`Components/Layout/EmptyLayout.razor`。
- **修改文件**：`Daos/AppDbContext.cs`（新增 `DbSet<Account>` + `OnModelCreating` 配置）、`Program.cs`（认证注册 + 端点 + 种子）、`Components/Routes.razor`（`AuthorizeRouteView`）、`Components/Layout/AppBar.razor`（登出按钮 + 用户名）、`Components/_Imports.razor`（`@using Authorization`）、`Components/Pages/Clusters/Clusters.razor`（`[Authorize]` + `AuthorizeView`）、`Components/Pages/Clusters/ClusterDetail.razor`（同上）。
- **数据库**：新增 `Account` 表，需删除 `clusters.db*` 重建库（无 EF 迁移，`EnsureCreated()` 建表）。
- **依赖**：无新增 NuGet 包（`PasswordHasher<T>` 属于 `Microsoft.AspNetCore.Identity`，已随 ASP.NET Core 提供）。
