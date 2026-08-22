## Context

`Components/Account/Pages/Profile.razor` 整页被 `@* *@` 注释(151 行),Drawer 已有 `/profile` 导航入口。旧页含改密码表单与残留的显示名保存逻辑(`SaveDisplayName` 从未有对应输入框)。`AccountService` 提供 `ChangePasswordAsync`(完整、中文报错)与 `GetUserByNameAsync`;`UpdateProfileAsync` 是空壳(仅更新 `UpdatedAt`)。`ApplicationUser` 有 `CreatedAt`、`UpdatedAt`、`LastLoginAt`(登录成功时写入,见 `AuthService`),无显示名概念。schema 由 `EnsureCreated()` 创建,无迁移 —— 本次设计刻意不触碰实体。

## Goals / Non-Goals

**Goals:**
- 复活 `/profile` 页面,登录可见,展示只读信息卡片 + 修改密码
- 身份直接使用用户名,无显示名
- 零 schema 变更

**Non-Goals:**
- 头像、主题偏好持久化、显示名等任何新字段
- 修改用户名、角色自管理等管理员功能

## Decisions

**D1: 信息卡片数据来源 —— `GetUserByNameAsync` + 认证 claims。**
用 `AuthStateProvider.GetAuthenticationStateAsync()` 取用户名与角色 claim,再经 `AccountService.GetUserByNameAsync` 取实体展示 `CreatedAt` / `LastLoginAt`。与 `Accounts.razor` 取当前用户方式一致。备选:直接读取 `AuthenticationState` 全部信息 —— 但创建时间不在 claim 中,仍需查库;查询服务层,保持既有分层。

**D2: 不新建 ProfileViewModel,直接展示实体字段。**
信息卡片仅 4 个只读字段,无表单绑定、无映射逻辑,引入 ViewModel + Mapping 属过度设计。备选:新建 `ProfileViewModel` + 映射 —— 被否决,无编辑语义。

**D3: 时间显示格式 `yyyy-MM-dd HH:mm`,`LastLoginAt` 为 null 显示 "—"。**
与 `AccountTable.razor` 的最后登录列保持一致,避免同应用内两种格式。

**D4: 密码修改复用 `AccountService.ChangePasswordAsync`,错误码 `PasswordMismatch` 映射为"当前密码错误"。**
沿用旧页已验证的报错映射逻辑;Identity 中文错误描述器已就绪。

**D5: 删除 `UpdateProfileAsync` 空壳。**
实现前 grep 确认无调用方后删除。备选:保留 —— 无调用方的死代码,删除更干净。

**D6: 页面结构对齐全站风格。**
页面放入独立功能文件夹 `Components/Profile/Pages/`(遵循 `Components/<Feature>/{Pages,...}` 惯例,与账号管理解耦)。视觉上对齐现有页面:顶部 `MudPaper Class="pa-4"` 头部(标题 `Typo.h5` + 操作按钮行),内容区用 `MudCard Elevation="1"` + `MudCardHeader`(`Typo.h6` 标题)+ `MudCardContent`;只读字段用 `MudGrid Spacing="2"` 栅格,标签 `Typo.body2 mud-text-secondary`、值 `Typo.body1`,null 显示 "—"(参考 `NodeOverviewCard.razor`)。加载期用 `MudProgressLinear Class="my-4"`(对齐详情页),按钮提交时禁用并显示 `MudProgressCircular`。

## Risks / Trade-offs

- [角色 claim 可能滞后于管理员改角色] → 仅展示用途,接受一次性陈旧;刷新页面即更新
- [`UpdateProfileAsync` 删除前若存在隐藏调用方] → 先 grep 全仓库确认,无调用方再删
- [页面被注释期间 `/profile` 实际 404/空白] → 实现后手动访问验证路由与 Drawer 入口连通
- [密码修改成功后会话保持] → `ChangePasswordAsync` 不调用 `SignOutAsync`,登录态不受影响,符合现状预期

## Migration Plan

无数据库迁移。实现后运行 `dotnet build MultiClusterMgmtSys.slnx` 验证编译,手动走查:登录 → Drawer 点击个人资料 → 信息展示与改密码流程。
