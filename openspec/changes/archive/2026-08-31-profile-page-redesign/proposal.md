## Why

个人资料页 UI 过时且信息单薄:圆形 MudAvatar 头像、实心角色 Chip、`Elevation=1` 阴影均与现行 Swiss Industrial Print 设计系统冲突;修改密码表单直接内嵌页面显得臃肿;页面无任何数据纵深(只有 5 个字段)。

## What Changes

- 账号信息卡重构:琥珀方牌头像(呼应 `.brand-mark`)+ 用户名 + 淡彩角色徽章(替代实心 Chip)+ 三个时间字段等宽化(`.font-mono`),去阴影改发丝线。
- **BREAKING(页面结构)**: 修改密码从内嵌卡片改为 MudDialog 弹窗(带密码可见性切换),页面不再直出表单;账号卡右上角"修改密码"按钮打开对话框。
- 新增"最近操作"卡(位于账号信息卡下方):显示**当前用户本人**最近 **最多 5 条**审计日志(mono 时间 + 类别 · 操作 + 目标),更多通过"查看全部"跳转 `/audit-logs`。
- 服务层新增两个方法:`AuditLogRepository.GetRecentForUserAsync(userName, count)` 与 `AuditService.GetRecentAsync(userName, count)`(按 `CreatedAt` 倒序 LIMIT 5,仅本人记录)。

## Capabilities

### New Capabilities

<!-- 无新能力;最近操作属于 profile-page 能力的增强需求 -->

### Modified Capabilities

- `profile-page`: 账号信息展示方式变化(方牌头像/淡彩徽章/等宽时间)、修改密码改为对话框交互、新增"最近操作"需求。

## Impact

- `Components/Profile/Pages/Profile.razor` — 重构(账号卡 + 最近操作卡 + 改密按钮)
- 新增 `Components/Profile/Shared/ChangePasswordDialog.razor` — 改密对话框
- `Data/Repositories/AuditLogRepository.cs`、`Services/AuditService.cs` — 各新增一个查询方法(约 20 行)
- 复用现有 `AuditLogViewModel` 映射与 `ExceptionPresenter`
- 无数据库/API 变更;无新依赖