## Context

The account management feature is dead code: `Accounts.razor`, `AccountEditDialog.razor`, and `ResetPasswordDialog.razor` are wrapped in `@* ... *@` (first-generation UI style), while `Drawer.razor:17` still renders the Admin-gated "账号管理" link to `/accounts` — a live 404. `AccountService` remains registered and live (it seeds the `admin` user on startup via `CreateAdminAsync`) with guards for self-deletion and "must keep at least one Admin", but it carries a dead field (`AccountViewModel.DisplayName` has no backing column) and no login tracking. The cluster feature has established the current visual vocabulary — `MudPaper pa-4` toolbar + filter bar + server-paged `MudTable` (`Clusters.razor`), shared components under `Components/Clusters/Shared/`, admin actions gated by `<AuthorizeView Roles="Admin">`, inline row buttons wrapped in `<span @onclick:stopPropagation="true">`.

User decisions (explore session):
- Scope is `/accounts` only; `Profile.razor` stays commented and will be split out as a separate change.
- No display name (display name == username), no enable/disable — only add 最后登录时间 (`LastLoginAt`).
- Batch operations required: batch delete AND batch role change.
- The built-in `admin` account is fully immutable on this page (no edit, no reset password, no delete), locked by **username** (not role).

## Goals / Non-Goals

**Goals:**
- Bring `/accounts` back online in the current cluster-page vocabulary: `MudPaper` toolbar (刷新 / 批量操作 / 新建账号), `AccountFilterBar` (用户名搜索 + 角色筛选), server-paged `MudTable` (`ServerData` + `Pager` + `NoRecordsContent`).
- Add `LastLoginAt` to `ApplicationUser`, written on the successful login path, shown as `yyyy-MM-dd HH:mm` (or `—` when null).
- Enforce built-in-admin immutability server-side (`AccountService`), not just in the UI; represent it in the table as a lock icon + disabled batch checkbox.
- Add batch delete and batch role change with per-row protection skipping and a processed/skipped summary surfaced via Snackbar.
- Slim `AccountEditDialog` to role-only editing; password changes only via the dedicated reset dialog.
- Preserve existing guards (cannot delete self, must keep ≥1 Admin) and the startup admin seed.

**Non-Goals:**
- No `Profile` page work (`/profile` remains commented; separate change later).
- No display-name column, no enable/disable/lockout UI, no account search by role count or status.
- No account repository — the paged query lives entirely in `AccountService` over `UserManager.Users` (the `cluster-query-layering` spec governs the clusters repository only and is untouched).
- No `dotnet ef migrations` — schema stays `EnsureCreated()`-driven; the added column requires deleting the runtime DB file (see Decision 4).
- No external-login/SSO tracking — the login page is the single authentication entry; `LastLoginAt` is written only there.
- No changes to `Register.razor` / `AuthService.RegisterAsync` (registration already assigns the `Member` role).

## Decisions

### Decision 1: Direct overwrite of the three commented files

`Accounts.razor`, `AccountEditDialog.razor`, `ResetPasswordDialog.razor` are fully commented and written in the retired first-gen style; they are overwritten wholesale (git history preserves the dead versions). `ResetPasswordDialog`'s behavior is preserved (new password + confirm, validation via `ResetPasswordAsync`) — the dialog is rewritten, not resurrected line-by-line.

### Decision 2: Query layering without a repository

Mirror the cluster pattern's *shape* (UI request DTO → service owns translation → paged result) but skip the intermediate repository spec:

```
AccountFilterBar / MudTable.TableState
        │
        ▼
AccountQueryRequest { SearchName, RoleFilter, Page, PageSize }   ← Components/Account/Requests/
        │
        ▼
AccountService.GetPagedAccountsAsync(TableState, AccountQueryRequest)
        │  maps TableState.Page (0-based → 1-based), SortDirection → bool
        ▼
IQueryable over userManager.Users (AsNoTracking)
        │  Where(UserName.Contains) / Where(role membership) → Count → OrderBy(CreatedAt desc,
        │  ThenByDescending(Id)) → Skip/Take
        ▼
PagedResult<AccountViewModel>   ← Common/ViewModels/PagedResult.cs (reused)
```

Rationale: `cluster-query-layering` exists because `ClusterRepository` must not import `Components.*`; there is no account repository, so a sentinel-free intermediate query type would be ceremony with no consumer. The service is the single place that knows MudBlazor table state — same "service owns translation" rule, one layer fewer. Roles are attached per row via `GetRolesAsync` on the materialized page (≤ ~20 rows, matching the pre-existing `GetAllAccountsAsync` cost).

### Decision 3: LastLoginAt write site — `AuthService.LoginAsync` success path

`Login.razor` is the only entry (no external providers configured; `RegisterConfirmation.razor` is the other Identity page but does not sign in). After `PasswordSignInAsync` succeeds, set `user.LastLoginAt = DateTime.UtcNow` and `userManager.UpdateAsync(user)`. Consistent with the existing `UtcNow` convention (`CreatedAt`, `UpdatedAt`). Failed logins never touch the field (spec: "Failed login does not write the timestamp").

### Decision 4: Schema change — delete the runtime DB (no migrations)

`ApplicationUser.LastLoginAt` (`DateTime?`) is added to the entity; `ApplicationDbContext` needs no explicit configuration (nullable `DateTime` maps fine). `EnsureCreated()` will **not** alter an existing `MultiClusterMgmtSys.db`, so the file (plus `-shm`/`-wal`) must be deleted before the first run; startup re-creates the schema and re-seeds `admin`/`Changeme_123`. Other registered accounts are lost — accepted by the user, flagged in the proposal as **BREAKING**. `*.db` is gitignored, so this is a local runtime reset, not a tracked-file change.

### Decision 5: Built-in admin immutability — server-side, by username

`AccountService` gains `private const string BuiltInAdminName = "admin";` (matching the `CreateAdminAsync` seed). Guards in `UpdateAccountAsync`, `ResetPasswordAsync`, and `DeleteAccountAsync` return `IdentityResult.Failed` with Chinese errors (内置管理员不可修改 / 内置管理员不可删除) before any other logic. UI mirrors the lock: the table's operations column renders `MudIcon` `Icons.Material.Filled.LockOutline` with tooltip 内置管理员不可修改 instead of the edit/reset/delete buttons, and the batch-mode checkbox is disabled for that row. Locking by username (user's choice) means other `Admin`-role accounts remain fully manageable.

### Decision 6: Batch operations with correct last-admin counting

`BatchDeleteAsync(IReadOnlyList<int> ids, int currentUserId)` and `BatchUpdateRoleAsync(IReadOnlyList<int> ids, int currentUserId, string roleName)` return a small `AccountBatchResult(int Processed, int Skipped)` record.

Skip rules (evaluated once per call, not per row against a stale count):
- built-in admin id (resolved via `FindByNameAsync(BuiltInAdminName)`),
- `currentUserId`,
- last-admin violations: let `adminCandidates` = selected users currently in `Admin` role. For **delete** and for **demotion to `Member`**: if `currentAdminCount - adminCandidates.Count < 1`, skip **all** `adminCandidates` (a naive per-row `if adminCount <= 1` would allow two admins to be deleted when only those two exist — the batch must consider the in-flight removals).

Processed rows are deleted / re-roled in sequence (`RemoveFromRolesAsync` + `AddToRoleAsync` for role change). The page surfaces `已处理 N 个账号，跳过 M 个受保护账号` via Snackbar. Batch UI mirrors `Clusters.razor`: batch-mode toggle in the toolbar (Admin-gated), checkbox column appears in the table, and a bottom `MudPaper` bar shows 已选 N 个 + 批量改角色 `MudMenu` (Admin / Member) + 批量删除 + 清空选择.

### Decision 7: UI decomposition

New shared components under `Components/Account/Shared/` (mirroring `Components/Clusters/Shared/`):

| File | Mirrors | Purpose |
|---|---|---|
| `AccountFilterBar.razor` | `ClusterFilterBar.razor` | `MudTextField` 用户名搜索 + `MudSelect<string?>` 角色 (全部=null / Admin / Member); binds to `AccountQueryRequest`, emits change events. |
| `AccountTable.razor` | `ClusterTable.razor` | `MudTable<AccountViewModel>` with `ServerData`, `Pager`, `NoRecordsContent`; columns 用户名 / 角色 chip / 创建时间 / 最后登录 / 操作; optional checkbox column in batch mode; row actions in `<span @onclick:stopPropagation="true">`; lock-row handling for built-in admin. |

The page itself keeps the toolbar and the batch bar inline (as `Clusters.razor` does). `AccountEditDialog.razor`: create mode = 用户名 + 初始密码 + 角色; edit mode = read-only 用户名 + 角色 select (no display name, no password field — reset goes through `ResetPasswordDialog`, whose row button is hidden for built-in admin). `AccountViewModel`: drop `DisplayName`, add `DateTime? LastLoginAt`; the mapping extension (`AccountMappingExtensions`) is updated accordingly.

### Decision 8: Namespaces

Per AGENTS.md, `Components/Account/**` splits by folder: ViewModels → `MultiClusterMgmtSys.Features.Account.ViewModels[.Mappings]`; Services → `MultiClusterMgmtSys.Components.Account.Services`; new `Components/Account/Requests/AccountQueryRequest.cs` → `MultiClusterMgmtSys.Components.Account.Requests` (mirrors `Components/Clusters/Requests`). Each new `.razor` copies its `@using` block from a sibling in the same folder; `_Imports.razor` is unchanged.

## Risks / Trade-offs

- **[R1] DB regeneration loses accounts** → Accepted (user decision). Mitigation: delete `MultiClusterMgmtSys.db` before first run; `CreateAdminAsync` re-seeds roles + admin on startup; migration plan calls this out explicitly.
- **[R2] Batch last-admin race** → Naive per-row counting breaks when a batch deletes both of the only two admins. Mitigation: Decision 6 computes `adminCandidates` once against the current admin count and skips the whole set when the remainder would be zero.
- **[R3] N+1 role lookup per page row** → Bounded by page size (~20); identical to the pre-existing `GetAllAccountsAsync` pattern. Noted for a future join-based optimization if the account count grows.
- **[R4] Namespace drift (Features vs Components)** → Invites copy-paste errors; mitigated by Decision 8's sibling-copy rule.
- **[R5] Single edit allows self-demotion** → The approved guard matrix permits a single admin to demote themselves (only *batch* role change skips self). Accepted; the batch path protects against accidental mass self-demotion.
- **[R6] LastLoginAt only tracks password login** → Correct today (single entry); if external providers are added later, the write must move into the sign-in pipeline (noted, not implemented).

## Migration Plan

1. **Pre-run**: delete `MultiClusterMgmtSys.db` (+ `-shm`/`-wal`). `dotnet build MultiClusterMgmtSys.slnx` must pass first.
2. **Implement in order**: `ApplicationUser.LastLoginAt` → `AuthService` login write → `AccountService` (paged query, guards, batch) → `AccountViewModel`/`AccountQueryRequest`/mapping → `Shared/` components → dialogs → `Accounts.razor` → build → smoke.
3. **Smoke**: login as `admin`, verify table renders with 最后登录 populated after re-login; create/edit/delete a Member; batch delete + batch role change with mixed protected/eligible rows (verify skipped count); verify lock row has no actions and its checkbox is disabled; verify self-delete and last-admin guards still return Chinese errors.
4. **Rollback** = `git revert` the change commit; the DB file is a runtime artifact and can be deleted/recreated at any time.

## Open Questions

- (none — guard matrix, lock-by-username, batch scope, and DB-drop acceptance all resolved with the user.)
