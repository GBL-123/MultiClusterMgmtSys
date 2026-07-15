## MODIFIED Requirements

### Requirement: 账号存储与种子

系统 SHALL 通过 ASP.NET Core Identity 在 SQLite 中存储账号（`ApplicationUser : IdentityUser<int>` 写入 `AspNetUsers`，`IdentityRole<int>` 写入 `AspNetRoles`），密码由 Identity 内置 `PasswordHasher`（PBKDF2）哈希；启动时缺失 admin / member 角色与用户时自动种子，重启幂等。

#### Scenario: 启动时种子角色与账号

- **WHEN** 应用启动且 `AspNetRoles` 表为空或缺失 `Admin` / `Member` 角色
- **THEN** `AccountService.SeedAccountsAsync()` 通过 `RoleManager.CreateAsync(new IdentityRole<int> { Name = "Admin" })` / `"Member"` 创建角色；若 admin / member 用户缺失则 `UserManager.CreateAsync(new ApplicationUser { UserName = "admin", DisplayName = "管理员", CreatedAt = UtcNow }, "Changeme_123")` + `AddToRoleAsync(user, "Admin" / "Member")`

#### Scenario: 种子幂等

- **WHEN** 应用启动且 `AspNetRoles` 与 `AspNetUsers` 已有 admin / member 记录
- **THEN** `SeedAccountsAsync()` 不创建重复记录（`RoleManager.RoleExistsAsync` / `UserManager.FindByNameAsync` 检查后跳过）

### Requirement: 角色权限矩阵

系统 SHALL 按 Identity 字符串角色名 `Admin` / `Member` 控制操作可见性：Member 看到完整列表 / 详情但修改类按钮不渲染。

#### Scenario: Member 不可见修改按钮

- **WHEN** Member 用户查看集群列表或详情
- **THEN** "添加集群""编辑""删除""刷新状态""新建分组""显示密文"按钮不渲染（`<AuthorizeView Roles="Admin">` 包裹，Member 角色不匹配；`Roles="Member"` 仅用于控制"只读可改自己"类入口如 `/profile`）

#### Scenario: Admin 可见全部按钮

- **WHEN** Admin 用户查看集群列表或详情
- **THEN** 所有操作按钮正常渲染

#### Scenario: Member 可查看分组管理对话框

- **WHEN** Member 用户点击"分组管理"按钮
- **THEN** 对话框打开并可查看分组列表，但删除按钮不渲染（`AuthorizeView` 包裹）
