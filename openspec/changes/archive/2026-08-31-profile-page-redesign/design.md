## Context

`Components/Profile/Pages/Profile.razor` 当前为「工具栏 + 左 8 右 4 双卡」结构:左卡账号信息(圆形 MudAvatar + 实心角色 Chip + 3 个非等宽时间),右卡内嵌修改密码表单。全站已落地 Swiss Industrial Print 设计系统(方牌语言、淡彩徽章、mono 数据列、禁阴影),本页未跟随。审计日志数据链路(实体 `AuditLog`、`AuditLogViewModel` 中文显示名、`AuditLogRepository.GetPagedAsync`、`ExceptionPresenter`)均已存在,缺一个"按用户取最近 N 条"的查询。

## Goals / Non-Goals

**Goals:**
- 账号信息卡对齐设计系统(方牌头像/淡彩角色徽章/mono 时间/去阴影)
- 修改密码移入对话框,页面不再直出表单
- 新增"最近操作"卡(本人最近 5 条 + 查看全部跳转)

**Non-Goals:**
- 不改审计日志页(`/audit-logs`)与审计写入逻辑
- 不做分页/筛选(最近操作固定 5 条)
- 不新增头像上传、昵称等账户能力

## Decisions

**D1. 页面布局:单列两卡,账号信息在上、最近操作在下**
改密移出后无必要保留 8/4 分栏,改单列(容器已有 max-width 约束):账号信息卡 → 最近操作卡。取消 `MudGrid`,用 MudStack 顺序堆叠。

**D2. 头像:复用品牌方牌语言**
40px 琥珀方牌(`.brand-mark.large` 同款视觉)+ 白色首字母(m`ono,600),不新建组件——直接 `<span class="brand-mark large">首字母</span>` 置于用户名左侧。替代圆形 MudAvatar。

**D3. 角色徽章:淡彩体系**
新增 `.role-badge` CSS(复用 `.status-badge` 的淡彩底+深字公式):Admin → 琥珀淡彩(`#FBF3DB`/`#956400`,呼应强调色),Member/其他 → 中性淡彩(`#EDEAE3`/`#57534E`)。替代 `MudChip Variant.Filled`。

**D4. 时间字段:等宽 + 三列 grid**
保留 3 列(`sm=4`)label/value 布局,label 保持 secondary 小字,value 加 `.font-mono`。空值显示 `—`。

**D5. 修改密码:对话框组件**
新增 `Components/Profile/Shared/ChangePasswordDialog.razor`:
- `MudDialog` + 三个密码框(当前/新/确认),均带可见性切换(Adornment,与登录页同款)
- 校验逻辑从页面迁移:两次不一致 → 对话框内 Snackbar;`AccountService.ChangePasswordAsync` 失败(`PasswordMismatch` → 「当前密码错误」)→ Snackbar;成功 → `Dialog.Close(Ok)` 
- 页面账号卡右上角「修改密码」文本按钮(带 `LockReset` 图标)打开对话框,成功后无刷新需求(时间字段不涉及)
- 对话框内错误处理沿用 `ExceptionPresenter`(`HandleAsync`)

**D6. 最近操作卡**
- 数据:`AuditService.GetRecentAsync(userName, 5)` 新增:
  - `AuditLogRepository.GetRecentForUserAsync(string userName, int count)`:`WHERE UserName == userName ORDER BY CreatedAt DESC LIMIT count`(`AuditLog.CreatedAt` 已有索引)
  - 服务层映射 `List<AuditLogViewModel>`(复用 `ToAuditLogViewModel`)
- 呈现:卡头「最近操作」+ 右侧「查看全部」(`.link-primary` 链接 → `/audit-logs`,非管理员本就看到自己的日志,语义衔接);行 = mono 时间(`yyyy-MM-dd HH:mm:ss`)+ `CategoryName · ActionName` + `Target`,发丝线分隔的紧凑列表(非表格)
- 空态:`.empty-state` `[ 暂无操作记录 ]`
- 加载失败:走 `ExceptionPresenter`(与页面其余 catch 一致)

**D7. 数据加载**
`OnInitializedAsync`:`AuthState`(用户名/角色)与 `GetUserByNameAsync`(三个时间)、`GetRecentAsync`(最近操作)用 `Task.WhenAll` 并行;全部 catch 走 `ExHandler.HandleAsync(ex, "加载用户资料")`。

## Risks / Trade-offs

- [最近操作查询在用户日志量大时无性能问题(CreatedAt 索引 + LIMIT 5)] → 无风险;若未来加筛选再评估
- [对话框内 Snackbar 与页面 Snackbar 混淆] → 沿用全站对话框模式(EditGroupDialog 等均自行 Snackbar),保持一致
- [`.role-badge` 新增 CSS 类需与 `.status-badge` 视觉协调] → 复用同一套 token 变量,实施时视觉验收

## Migration Plan

- 纯前端 + 两个新增查询方法,无数据迁移;回滚 = git revert
- `ChangePasswordDialog` 是新组件,旧内嵌表单代码直接替换,不留死代码

## Open Questions

- 无阻塞项。执行期可微调:角色徽章中性色深浅、最近操作行内目标过长时的截断方式(ellipsis)。