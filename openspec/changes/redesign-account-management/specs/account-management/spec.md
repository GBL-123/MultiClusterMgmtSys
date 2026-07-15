## ADDED Requirements

### Requirement: Member 自助注册

系统 SHALL 提供独立注册页 `/register`，未登录访客可注册 Member 账号，注册成功后自动登录并跳转。

#### Scenario: 注册成功

- **WHEN** 未登录用户在 `/register` 提交合法用户名、密码（满足 `AlphanumericPasswordValidator` 规则）、显示名（可选，0-64 字符）后端点 `POST /api/register` 校验通过
- **THEN** `AccountService.RegisterAsync` 内部 `UserManager.CreateAsync(ApplicationUser, password)` 返回成功 → `UserManager.AddToRoleAsync(user, "Member")` → `SignInManager.SignInAsync(user, isPersistent: false)` 写 Cookie，端点返回 `Results.LocalRedirect("/")` 整页跳转

#### Scenario: 用户名重复

- **WHEN** 提交的用户名已存在
- **THEN** `UserManager.CreateAsync` 返回 `IdentityResult.Failed(DuplicateUserName)`，端点映射为 `Results.Redirect("/register?error=duplicate&returnUrl=...")`，注册页显示"该用户名已被占用"

#### Scenario: 密码强度不达标

- **WHEN** 提交的密码长度 < 8 或不含字母 / 不含数字
- **THEN** `AlphanumericPasswordValidator` 或 Identity 内置规则产生 `IdentityError`（Code 包含 `PasswordTooShort` / `PasswordRequiresLetter` / `PasswordRequiresDigit`），端点映射为 `Results.Redirect("/register?error=weakpwd&returnUrl=...")`，注册页显示"密码至少 8 位且包含字母和数字"

#### Scenario: 已登录用户访问注册页

- **WHEN** 已登录用户访问 `/register`
- **THEN** 页面 `OnInitializedAsync` 通过 `AuthenticationStateProvider.GetAuthenticationStateAsync()` 检测已认证，`NavigateTo("/", forceLoad: true)` 跳走

#### Scenario: 登录页提供"立即注册"入口

- **WHEN** 未登录用户在 `/login`
- **THEN** 登录页渲染"没有账号？立即注册"链接，指向 `/register`

### Requirement: Admin 账号管理

系统 SHALL 提供 Admin 专属账号管理页 `/accounts`，含列表、新建、编辑、删除、重置密码能力，路由级 `Roles = "Admin"` 保护。

#### Scenario: Admin 查看账号列表

- **WHEN** Admin 访问 `/accounts`
- **THEN** 页面调 `AccountService.GetAllAccountsAsync()` 拿到 `AccountViewModel[]`（按 `CreatedAt` 降序），渲染账号列表（用户名、显示名、角色、创建时间、操作列），每行含"编辑""重置密码""删除"按钮（受 `Roles="Admin"` 包裹）

#### Scenario: Admin 新建账号

- **WHEN** Admin 在 `/accounts` 点击"新建账号"并填写用户名、初始密码、显示名、角色（"Admin" / "Member"）后提交
- **THEN** `AccountService.CreateAccountAsync` 内部 `UserManager.CreateAsync(ApplicationUser, password)` + `AddToRoleAsync` 成功，`Snackbar` 提示"账号已创建"，列表刷新

#### Scenario: Admin 编辑账号

- **WHEN** Admin 在列表点击"编辑"修改显示名或角色后提交
- **THEN** `AccountService.UpdateAccountAsync` 内部 `UserManager.UpdateAsync` + 必要时 `RemoveFromRoleAsync` / `AddToRoleAsync`，`Snackbar` 提示"账号已更新"，列表刷新

#### Scenario: Admin 重置密码

- **WHEN** Admin 在列表点击"重置密码"输入新密码后提交
- **THEN** `AccountService.ResetPasswordAsync` 内部 `UserManager.RemovePasswordAsync` + `AddPasswordAsync(user, newPassword)`，`Snackbar` 提示"密码已重置"

#### Scenario: Admin 删除账号

- **WHEN** Admin 在列表点击"删除"经二次确认后提交
- **THEN** `AccountService.DeleteAccountAsync` 通过护栏后调 `UserManager.DeleteAsync` 成功，`Snackbar` 提示"账号已删除"，列表刷新

#### Scenario: 不能删除当前登录用户

- **WHEN** Admin 在列表点击自己的"删除"并确认
- **THEN** `AccountService.DeleteAccountAsync` 检测 `targetUserId == currentUserId` 后返回 `IdentityResult.Failed`（`Code = "CannotDeleteSelf"`），UI 提示"不能删除当前登录账号"

#### Scenario: 不能删除最后一个 Admin

- **WHEN** Admin 在列表点击系统中最后一个 Admin 账号的"删除"并确认
- **THEN** `AccountService.DeleteAccountAsync` 调 `RoleManager.FindByNameAsync("Admin")` + 统计用户数后返回 `IdentityResult.Failed`（`Code = "CannotDeleteLastAdmin"`），UI 提示"系统中必须至少保留一个 Admin 账号"

#### Scenario: Member 访问 `/accounts`

- **WHEN** Member 用户直接访问 `/accounts` URL
- **THEN** `[Authorize(Roles = "Admin")]` 触发 `AuthorizeRouteView` 的 `<NotAuthorized>` 分流，渲染禁止访问页或跳回首页

### Requirement: 自助资料与密码修改

系统 SHALL 提供 `/profile` 页面，任何已登录用户可修改自己的显示名与密码，路由级 `[Authorize]` 保护。

#### Scenario: 修改显示名

- **WHEN** 已登录用户在 `/profile` 提交新显示名
- **THEN** 页面从 `AuthenticationStateProvider` 取当前用户名 → `UserManager.FindByNameAsync` → 修改 `DisplayName` → `UserManager.UpdateAsync` 成功，`Snackbar` 提示"资料已更新"

#### Scenario: 修改自己的密码

- **WHEN** 已登录用户在 `/profile` 提交当前密码 + 新密码 + 确认密码，且新密码满足 `AlphanumericPasswordValidator` 规则
- **THEN** 页面调 `UserManager.ChangePasswordAsync(user, currentPassword, newPassword)` 成功（Identity 内部校验当前密码与哈希），`Snackbar` 提示"密码已更新"，不强制重新登录

#### Scenario: 当前密码错误

- **WHEN** 用户在 `/profile` 改密时提交的当前密码与库中哈希不匹配
- **THEN** `UserManager.ChangePasswordAsync` 返回 `IdentityResult.Failed`（`Code = "PasswordMismatch"`），UI 提示"当前密码错误"

#### Scenario: 新密码与确认密码不一致

- **WHEN** 用户在 `/profile` 改密时新密码与确认密码字段不相等
- **THEN** 前端表单校验失败，不提交；UI 提示"两次输入的密码不一致"

### Requirement: 账号模型与 Identity 表

系统 SHALL 使用 ASP.NET Core Identity 存储账号与角色：`ApplicationUser : IdentityUser<int>` 写入 `AspNetUsers`，`IdentityRole<int>` 写入 `AspNetRoles`，关联写 `AspNetUserRoles`，`AppDbContext` 继承 `IdentityDbContext<ApplicationUser, IdentityRole<int>>`。

#### Scenario: ApplicationUser 字段映射

- **WHEN** `EnsureCreated` 首次建库
- **THEN** `AspNetUsers` 表包含 `Id`（int 主键）、`UserName`、`NormalizedUserName`、`Email`（可空）、`PasswordHash`、`SecurityStamp`、`ConcurrencyStamp` 与自定义 `DisplayName`（`HasMaxLength(64)`）、`CreatedAt`（默认 `DateTime.UtcNow`）、`UpdatedAt`（可空）

#### Scenario: UserName 唯一

- **WHEN** 服务层创建用户导致 `UserName` 重复
- **THEN** `UserManager.CreateAsync` 返回 `IdentityResult.Failed(DuplicateUserName)`，UI 给出"该用户名已被占用"提示

#### Scenario: 列表按创建时间倒序

- **WHEN** Admin 查看 `/accounts` 列表
- **THEN** 列表默认按 `CreatedAt` 降序排列，新注册 / 新建账号排在前

### Requirement: 密码强度策略

系统 SHALL 通过自定义 `IPasswordValidator<ApplicationUser>` (`AlphanumericPasswordValidator`) 强制所有写入账号的明文密码（注册、新建、重置、自助改密）满足"长度 ≥ 8 + 至少 1 字母 + 至少 1 数字"。

#### Scenario: 强密码接受

- **WHEN** 密码满足长度 ≥ 8 + 含字母 + 含数字
- **THEN** `AlphanumericPasswordValidator.ValidateAsync` 返回 `IdentityResult.Success`，`UserManager.CreateAsync` 继续后续流程

#### Scenario: 弱密码拒绝

- **WHEN** 密码长度 < 8 或缺字母 / 缺数字
- **THEN** `AlphanumericPasswordValidator.ValidateAsync` 返回 `IdentityResult.Failed`（`Code = "PasswordTooWeak"`），`UserManager.CreateAsync` 整体失败，端点 / 页面映射到 `error=weakpwd` / Snackbar 提示

### Requirement: 侧边栏自助资料入口

系统 SHALL 在 `Components/Layout/Drawer.razor` 的 `MudNavMenu` 中对所有已登录用户（含 Member）暴露"个人资料"入口（`MudNavLink` 指向 `/profile`），允许 Member 通过侧边栏进入 `/profile` 修改自己的显示名与密码；Admin 同时可见"账号管理"与"个人资料"两个入口。

#### Scenario: Member 在侧边栏看到"个人资料"

- **WHEN** Member 用户登录后展开 Drawer
- **THEN** Drawer 渲染"个人资料" `MudNavLink`（`Icons.Material.Filled.Person`，`Href="/profile"`，无 `AuthorizeView Roles` 角色限制），点击后 `NavigationManager.NavigateTo("/profile")` 跳转到 `/profile` 页

#### Scenario: Member 在侧边栏看不到"账号管理"

- **WHEN** Member 用户登录后展开 Drawer
- **THEN** Drawer 不渲染"账号管理" `MudNavLink`（仍受 `<AuthorizeView Roles="Admin">` 包裹，Member 不匹配），Member 直接访问 `/accounts` 仍被 `[Authorize(Roles="Admin")]` 拦截

#### Scenario: Admin 在侧边栏看到两个入口

- **WHEN** Admin 用户登录后展开 Drawer
- **THEN** Drawer 同时渲染"账号管理"（→ `/accounts`，Admin 后台）与"个人资料"（→ `/profile`）两个 `MudNavLink`，两者均可点击跳转

#### Scenario: 未登录不渲染入口

- **WHEN** 未登录用户访问任意页面
- **THEN** Drawer 的两个账号入口都不渲染（"账号管理"受 Admin `AuthorizeView` 保护、"个人资料"虽无角色限制但 Drawer 整体由 `MainLayout` 的认证上下文驱动，未登录时受 `<AuthorizeView>` 顶层包裹不渲染账号区）
