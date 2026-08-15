## 1. Data Model & Login Tracking

- [x] 1.1 Add `DateTime? LastLoginAt` to `Data/Entities/ApplicationUser.cs` (no `ApplicationDbContext` config needed for a nullable DateTime)
- [x] 1.2 In `Components/Auth/Services/AuthService.cs`, on the successful `PasswordSignInAsync` path of `LoginAsync`, set `user.LastLoginAt = DateTime.UtcNow` and persist via `userManager.UpdateAsync(user)`

## 2. ViewModels & Query DTO

- [x] 2.1 Update `Components/Account/ViewModels/AccountViewModel.cs`: remove `DisplayName`, add `DateTime? LastLoginAt` (keep namespace `MultiClusterMgmtSys.Features.Account.ViewModels`)
- [x] 2.2 Update `Components/Account/ViewModels/Mappings/AccountMappingExtensions.cs` to map `LastLoginAt` and drop `DisplayName`
- [x] 2.3 Create `Components/Account/Requests/AccountQueryRequest.cs` (`SearchName` string, `RoleFilter` string?, `Page` int, `PageSize` int; namespace `MultiClusterMgmtSys.Components.Account.Requests`)
- [x] 2.4 Add `AccountBatchResult` record (`Processed`, `Skipped` ints) — declare in `AccountService.cs` (or `Requests/` if cleaner)

## 3. AccountService Hardening & Batch

- [x] 3.1 Add `private const string BuiltInAdminName = "admin";` and reject `UpdateAccountAsync` for it with Chinese error 内置管理员不可修改
- [x] 3.2 Reject `ResetPasswordAsync` for the built-in admin with the same Chinese error
- [x] 3.3 Reject `DeleteAccountAsync` for the built-in admin with Chinese error 内置管理员不可删除 (keep existing self-delete and last-admin guards)
- [x] 3.4 Implement `GetPagedAccountsAsync(TableState, AccountQueryRequest)` → `PagedResult<AccountViewModel>`: map 0-based page and sort direction, filter by `UserName.Contains` and role membership, order `CreatedAt` desc + `Id` desc tie-breaker, clamp page/page size, attach roles per row via `GetRolesAsync`
- [x] 3.5 Implement `BatchDeleteAsync(IReadOnlyList<int> ids, int currentUserId)` → `AccountBatchResult`: skip built-in admin + current user + last-admin set (compute `adminCandidates` once; skip all of them if `adminCount - candidates.Count < 1`)
- [x] 3.6 Implement `BatchUpdateRoleAsync(IReadOnlyList<int> ids, int currentUserId, string roleName)` → `AccountBatchResult`: skip built-in admin + current user; for demotion to `Member` apply the same last-admin set logic; otherwise remove existing roles and add the target role
- [x] 3.7 Add `logger.LogInformation` enter/done and `logger.LogWarning` failure lines to the new service methods (existing convention)

## 4. Shared Components

- [x] 4.1 Create `Components/Account/Shared/AccountFilterBar.razor` (namespace `MultiClusterMgmtSys.Features.Account.*` only if a sibling uses it — otherwise copy sibling `@using`): username `MudTextField` + role `MudSelect<string?>` (全部=null / Admin / Member), binds to `AccountQueryRequest`, emits `OnFilterChanged`
- [x] 4.2 Create `Components/Account/Shared/AccountTable.razor`: `MudTable<AccountViewModel>` with `ServerData`, `Pager`, `NoRecordsContent`; columns 用户名 / 角色 chip / 创建时间 (`yyyy-MM-dd HH:mm`) / 最后登录 (`yyyy-MM-dd HH:mm` or `—`) / 操作; checkbox column when batch mode is on; built-in admin row renders `LockOutline` icon (tooltip 内置管理员不可修改) and disabled checkbox; action buttons inside `<span @onclick:stopPropagation="true">`

## 5. Dialogs

- [x] 5.1 Rewrite `Components/Account/Shared/AccountEditDialog.razor`: create mode = 用户名 + 初始密码 (show/hide toggle) + 角色 select; edit mode = read-only 用户名 + 角色 select only (no display name, no password field); submit path uses `CreateAccountAsync` / `UpdateAccountAsync`
- [x] 5.2 Rewrite `Components/Account/Shared/ResetPasswordDialog.razor` preserving behavior: new password + confirm, `ResetPasswordAsync`, show/hide toggle, 两次输入的密码不一致 check

## 6. Accounts Page

- [x] 6.1 Rewrite `Components/Account/Pages/Accounts.razor` (`@attribute [Authorize(Roles = "Admin")]`, `@page "/accounts"`, PageTitle 账号管理): `MudPaper pa-4` toolbar (刷新 / 批量操作 toggle / 新建账号 — last two Admin-gated), `AccountFilterBar`, `AccountTable` (wired via `@ref` reload like `Clusters.razor`), bottom batch bar (已选 N 个 + 批量改角色 `MudMenu` Admin/Member + 批量删除 + 清空选择), dialogs wiring (create/edit/reset), snackbars for all results including 已处理 N 个账号，跳过 M 个受保护账号
- [x] 6.2 Ensure current-user id detection (`ClaimTypes.NameIdentifier` → `currentUserId`) is passed to delete/batch service calls

## 7. Build & Smoke

- [x] 7.1 `dotnet build MultiClusterMgmtSys.slnx` passes
- [ ] 7.2 Delete `MultiClusterMgmtSys.db` (+ `-shm`/`-wal`), `dotnet run`, and smoke-test: login as `admin` → table renders with 最后登录 populated after re-login; create/edit/delete a Member; batch delete + batch role change with mixed protected/eligible rows (verify skipped count); lock row shows lock icon with disabled checkbox; self-delete and last-admin guards return Chinese errors
