# Authentication

## Purpose

Cookie 认证、登录/登出流程、Admin/Guest 角色权限矩阵、页面级与组件级访问控制。

## Requirements

### Requirement: 未登录用户跳转登录页

系统 SHALL 在未登录用户访问任意需认证页面时，自动跳转到 `/login` 页面，并在 URL 中携带 `returnUrl` 参数供登录后回跳。

#### Scenario: 未登录访问受保护页面

- **WHEN** 未登录用户访问 `/clusters` 或其他需认证页面
- **THEN** `AuthorizeRouteView` 检测未认证，渲染 `<RedirectToLogin />`，该组件 `NavigateTo("/login?returnUrl={encodedPath}", forceLoad: true)` 跳转登录页

#### Scenario: 已登录访问登录页

- **WHEN** 已登录用户访问 `/login`
- **THEN** `Login.razor` 的 `OnInitializedAsync` 通过 `AuthenticationStateProvider.GetAuthenticationStateAsync()` 检测已认证，`NavigateTo(returnUrl, forceLoad: true)` 跳走

### Requirement: 登录流程

系统 SHALL 提供独立登录页 `/login`，使用原生 HTML form POST 提交到 `/api/login` 端点，支持用户名 + 密码 + 记住我。

#### Scenario: 登录成功

- **WHEN** 用户在 `/login` 填写正确凭据并提交（form POST 到 `/api/login`）
- **THEN** 端点调 `AccountService.ValidateCredentialsAsync` 校验通过，`SignInAsync` 写 Cookie，返回 `Results.LocalRedirect(returnUrl)` 整页跳转，新电路读 Cookie 已认证

#### Scenario: 登录失败

- **WHEN** 用户填写错误凭据并提交
- **THEN** 端点返回 `Results.Redirect("/login?error=1&returnUrl={encoded}")`，登录页显示"用户名或密码错误"（`showError = true`，由 query string `error=1` 触发）

#### Scenario: 记住我

- **WHEN** 用户勾选"记住我"并登录成功
- **THEN** `SignInAsync` 的 `AuthenticationProperties.IsPersistent = true`，Cookie 持久化（浏览器关闭后仍保持）；不勾选则会话级 Cookie

### Requirement: 登出流程

系统 SHALL 在 `AppBar.razor` 提供登出按钮，点击后通过 GET `/api/logout` 端点清除 Cookie 并跳转登录页。

#### Scenario: 登出

- **WHEN** 已登录用户点击 AppBar 登出按钮
- **THEN** `NavigationManager.NavigateTo("/api/logout", forceLoad: true)` 触发 GET 请求，端点调 `SignOutAsync` 清 Cookie，返回 `Results.LocalRedirect("/login")` 整页跳转登录页

### Requirement: 账号模型与种子

系统 SHALL 在 SQLite 中存储账号（`Account` 实体），密码用 `PasswordHasher<string>`（PBKDF2）哈希，启动时种子 admin/guest 两个账号。

#### Scenario: 启动时种子账号

- **WHEN** 应用启动且 `Accounts` 表为空
- **THEN** `AccountService.SeedAccountsAsync()` 插入 admin（`AppRole.Admin`）与 guest（`AppRole.Guest`）两条账号，密码均为 `Changeme_123` 的 PBKDF2 哈希

#### Scenario: 种子幂等

- **WHEN** 应用启动且 `Accounts` 表已有记录
- **THEN** `SeedAccountsAsync()` 不插入新账号（`CountAsync() > 0` 时直接返回）

### Requirement: 角色权限矩阵

系统 SHALL 按 Admin/Guest 角色控制操作可见性：Guest 看到完整列表/详情但修改类按钮不渲染。

#### Scenario: Guest 不可见修改按钮

- **WHEN** Guest 用户查看集群列表或详情
- **THEN** "添加集群""编辑""删除""刷新状态""新建分组""显示密文"按钮不渲染（`<AuthorizeView Roles="Admin">` 包裹，Guest 角色不匹配）

#### Scenario: Admin 可见全部按钮

- **WHEN** Admin 用户查看集群列表或详情
- **THEN** 所有操作按钮正常渲染

#### Scenario: Guest 可查看分组管理对话框

- **WHEN** Guest 用户点击"分组管理"按钮
- **THEN** 对话框打开并可查看分组列表，但删除按钮不渲染（`AuthorizeView` 包裹）

### Requirement: 页面级访问控制

系统 SHALL 要求所有管理页面需登录后才能访问。

#### Scenario: 需登录页面

- **WHEN** 用户访问 `/clusters`、`/clusters/{id}`、`/nodes`、`/nodes/{id}` 等页面
- **THEN** 页面 `@attribute [Authorize]` 强制认证，未登录触发 `AuthorizeRouteView` 的 `<NotAuthorized>` 分流

### Requirement: AppBar 用户信息与登出

系统 SHALL 在 AppBar 右侧显示当前登录用户名与登出按钮（仅已登录时可见）。

#### Scenario: 已登录显示用户名与登出

- **WHEN** 用户已登录
- **THEN** AppBar 右侧显示 `@context.User.Identity?.Name`（用户名）与登出图标按钮（`AuthorizeView` 包裹）

#### Scenario: 未登录不显示

- **WHEN** 用户未登录（如在登录页）
- **THEN** AppBar 不渲染用户名与登出按钮（`AuthorizeView` 的 `Authorized` 模板不匹配）
