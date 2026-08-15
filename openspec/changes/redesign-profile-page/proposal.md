## Why

`/profile` 路由对应的页面被整体注释(151 行全部在 `@* *@` 内),Drawer 中的"个人资料"入口点击后无实际内容;旧页面的显示名编辑是半成品(服务层 `UpdateProfileAsync` 只更新时间戳,页面没有对应输入框)。需要重新设计并复活该页面。

## What Changes

- 在 `Components/Account/Pages/Profile.razor` 重新实现个人资料页(`@page "/profile"`),要求登录后可见
- 页面包含只读信息卡片:用户名、角色、注册时间、最后登录时间(时间格式 `yyyy-MM-dd HH:mm`,`LastLoginAt` 为 null 时显示 "—")
- 页面包含修改密码卡片:当前密码 / 新密码 / 确认新密码,复用 `AccountService.ChangePasswordAsync`
- 移除旧代码中的显示名残留(`SaveDisplayName` 方法、`savingDisplayName` 状态、`UpdateProfileAsync` 调用);无显示名概念,身份直接使用用户名
- 清理 `AccountService.UpdateProfileAsync` 空壳方法(若确认无其他调用方)

## Capabilities

### New Capabilities
- `profile-page`: 已登录用户查看自己的只读资料(用户名/角色/注册时间/最后登录)并修改自己密码的个人资料页面

### Modified Capabilities
<!-- 无:现有 spec 不涉及个人资料页 -->

## Impact

- `MultiClusterMgmtSys/Components/Account/Pages/Profile.razor`:重写(当前为注释状态)
- `MultiClusterMgmtSys/Components/Account/Services/AccountService.cs`:删除 `UpdateProfileAsync` 空壳(需先确认无其他调用方)
- 无 schema 变更,不触碰 `ApplicationUser` 实体与数据库
- 复用现有 `ChangePasswordAsync`(中文报错已就绪)、`GetUserByNameAsync`
- 角色信息从认证 claims 读取(与 `Accounts.razor` 读取当前用户方式一致)
