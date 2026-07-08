## Context

系统最初无认证/授权：`Program.cs` 未注册 `AddAuthentication`/`AddAuthorization`，`Routes.razor` 直接渲染无 `AuthorizeRouteView`，所有页面对匿名用户开放。

技术约束：.NET 10 Blazor Server 交互式渲染时 `HttpContext` 为 null（SignalR 电路事件不携带 HTTP 上下文），因此登录/登出必须通过 HTTP 端点（最小 API），不能在 Blazor 事件处理器里直接调 `SignInAsync`/`SignOutAsync`。Cookie 由 `UseAuthentication` 中间件在建立 SignalR 连接的 HTTP 请求上读取，`ClaimsPrincipal` 经 `ComponentHub.StartCircuit` 注入到 `ServerAuthenticationStateProvider`，Blazor 电路内 `AuthorizeView`/`[Authorize]` 即可读取。框架自动注册 `ServerAuthenticationStateProvider`，无需自定义 `AuthenticationStateProvider`。

## Goals / Non-Goals

**Goals:**

- 未登录用户访问任意页 → 跳转 `/login`；登录后回跳原 URL。
- Admin 账号：全部增删改查操作。Guest 账号：只读（列表/详情/节点列表），修改类按钮不渲染。
- 账号存 SQLite，密码 PBKDF2 哈希；启动时种子 admin/guest。
- Cookie 持久化（记住我）或会话级（不勾选），8 小时滑动续期。

**Non-Goals:**

- 账号自助注册/改密/找回密码。
- 多用户管理界面（增删账号、分配角色）。
- 第三方登录（OAuth/OIDC）。
- 细粒度资源级权限。
- 账号锁定/登录失败次数限制。
- Service/Repository 层角色校验（UI 层 `AuthorizeView` 隐藏按钮已足够，因写操作走 Blazor 事件 → Service → Repository，不经过 HTTP 写端点）。

## Decisions

### D1: Cookie 认证 + 最小 API 端点

**选择：** ASP.NET Core Cookie 认证（`CookieAuthenticationDefaults.AuthenticationScheme`），`/api/login`（POST）与 `/api/logout`（GET）为最小 API 端点。

**理由：** Blazor Server 交互式渲染 `HttpContext` 为 null，`SignInAsync`/`SignOutAsync` 必须在 HTTP 端点调用。Cookie 经 `UseAuthentication` → `ComponentHub` → 电路注入。

### D2: 登录页用原生 HTML form POST（非 HttpClient）

**选择：** `Login.razor` 使用 `<form method="post" action="/api/login">` + 隐藏 input 提交，而非 `HttpClient.PostAsJsonAsync`。

**理由：** 原生 form POST 直接由浏览器发起 HTTP 请求并处理重定向响应，无需在 Blazor 侧手动 `NavigateTo(forceLoad: true)`。`/api/login` 端点成功时返回 `Results.LocalRedirect(returnUrl)`（整页跳转），失败时返回 `Results.Redirect("/login?error=1&returnUrl=...")`（回到登录页带错误标志）。这比 HttpClient 方案更简洁，且天然处理了 `forceLoad` 语义。

**替代方案（已拒绝）：** `HttpClient.PostAsJsonAsync` + 前端 `NavigateTo(forceLoad: true)`——更复杂，需手动处理 200/401 响应和跳转。

### D3: 登出为 GET 端点 + `NavigateTo(forceLoad: true)`

**选择：** `app.MapGet("/api/logout", ...)`，`AppBar.razor` 中 `NavigationManager.NavigateTo("/api/logout", forceLoad: true)` 触发。

**理由：** GET 端点可直接通过 URL 导航触发，无需 HttpClient。`forceLoad: true` 销毁当前电路，登出后整页跳转到 `/login`。

### D4: `EmptyLayout` 用于登录页（非 `@layout null`）

**选择：** 新增 `EmptyLayout.razor`（仅含 `MudThemeProvider` + `MudPopoverProvider` + `MudDialogProvider` + `MudSnackbarProvider` + `@Body`），登录页 `@layout EmptyLayout`。

**理由：** 登录页需要 MudBlazor 主题与组件服务（ThemeProvider、PopoverProvider 等），但不能有 AppBar/Drawer。`@layout null` 会丢失这些 provider，导致 MudBlazor 组件渲染异常。

### D5: 登出按钮在 `AppBar.razor`（非 `MainLayout.razor`）

**选择：** 登出按钮与用户名显示在 `AppBar.razor` 中，由 `AuthorizeView` 包裹。

**理由：** `MainLayout.razor` 委托给 `<AppBar>` 组件，AppBar 是全局顶栏的真正实现位置。

### D6: `AddCascadingAuthenticationState` 显式注册

**选择：** `Program.cs` 中显式调用 `builder.Services.AddCascadingAuthenticationState()`。

**理由：** 不被 `AddInteractiveServerComponents()` 自动注册，必须显式添加，否则 `AuthorizeRouteView`/`AuthorizeView` 拿不到级联 `AuthenticationState`。

### D7: 未注册 `AddHttpContextAccessor`

**选择：** 不注册 `IHttpContextAccessor`。

**理由：** `AccountService` 不依赖 `HttpContext`（仅用 `AccountRepository` + `PasswordHasher`），登录/登出在最小 API 端点内直接用端点的 `HttpContext` 参数，无需全局注入。

### D8: 无 `AccessDenied.razor`——`NotAuthorized` 内联处理

**选择：** `Routes.razor` 的 `<NotAuthorized>` 内联判断 `context.User.Identity?.IsAuthenticated`：已登录但角色不足显示文案，未登录渲染 `<RedirectToLogin />`。不建独立 `AccessDenied.razor`。

**理由：** 角色不足场景通过 `<AuthorizeView>` 隐藏按钮避免触发，`[Authorize]` 仅做"需登录"门槛，`NotAuthorized` 内联处理足够简洁。

### D9: Cookie 配置

**选择：** `Cookie.Name = "MultiClusterMgmtSys.Auth"`、`HttpOnly = true`、`SameSite = Lax`、`ExpireTimeSpan = 8h`、`SlidingExpiration = true`、`LoginPath = "/login"`、`AccessDeniedPath = "/access-denied"`。不设 `LogoutPath`（登出通过 GET 端点处理）。

## Risks / Trade-offs

- **[默认密码风险]** → admin/guest 默认密码均为 `Changeme_123`，仅适用开发/内部工具。生产环境需改密功能（本期未做）。
- **[Guest 可调 API 端点]** → UI 层隐藏按钮，未在 Service 层做角色校验。本期无 HTTP 写端点（写操作走 Blazor 事件 → Service → Repository），UI 隐藏已足够。后续若加 HTTP 写端点需在端点层加 `[Authorize(Roles="Admin")]`。
- **[模型变更须重建库]** → 新增 `Account` 表触发 schema 变更，必须删除 `clusters.db*` 重跑（无迁移，`EnsureCreated()` 建表）。
