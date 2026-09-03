## 1. 数据层

- [x] 1.1 `AuditLogRepository` 新增 `GetRecentForUserAsync(string userName, int count)`:`WHERE UserName == userName ORDER BY CreatedAt DESC Take(count)`
- [x] 1.2 `AuditService` 新增 `GetRecentAsync(string userName, int count)`:调用仓库方法并映射 `List<AuditLogViewModel>`(复用 `ToAuditLogViewModel`),含 enter/done 日志

## 2. 修改密码对话框

- [x] 2.1 新建 `Components/Profile/Shared/ChangePasswordDialog.razor`:MudDialog + 当前/新/确认密码三框,均带可见性切换(Adornment,登录页同款)
- [x] 2.2 迁移校验逻辑:两次不一致 →「两次输入的密码不一致」;`ChangePasswordAsync` 失败(`PasswordMismatch` →「当前密码错误」)走 Snackbar;非业务异常走 `ExceptionPresenter`;成功 → `Dialog.Close(Ok)`

## 3. 页面重构

- [x] 3.1 `Profile.razor`:移除内嵌改密表单;账号信息卡重构——琥珀方牌头像(`.brand-mark.large` + 首字母)+ 用户名 + `.role-badge` 角色徽章(新增 CSS:Admin 琥珀淡彩/其他中性淡彩)+ 三个时间 `.font-mono`,去 `Elevation` 阴影
- [x] 3.2 账号卡右上角「修改密码」按钮 → `DialogService.ShowAsync<ChangePasswordDialog>`
- [x] 3.3 新增最近操作卡(账号卡下方):`GetRecentAsync(userName, 5)`;行 = mono 时间 + `CategoryName · ActionName` + `Target`(超长 ellipsis);「查看全部」`.link-primary` → `/audit-logs`;空态 `.empty-state` `[ 暂无操作记录 ]`
- [x] 3.4 数据加载:`Task.WhenAll` 并行(用户信息 + 最近操作),catch 统一走 `ExHandler.HandleAsync(ex, "加载用户资料")`
- [x] 3.5 app.css 新增 `.role-badge`(淡彩底 + 深字,复用 `.status-badge` token 公式)

## 4. 验证

- [x] 4.1 `dotnet build MultiClusterMgmtSys.slnx` 通过,0 错误
- [x] 4.2 冒烟:`dotnet run` 启动,检查 /profile 账号卡(方牌头像/徽章/mono 时间)、最近操作卡(本人 5 条/空态)、改密对话框(可见性切换/成功失败路径)