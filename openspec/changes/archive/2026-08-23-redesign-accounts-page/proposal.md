## Why

The account management page (`/accounts`) is written in the project's first-generation UI style and has been **commented out in place** (`@* ... *@` wrapping `Accounts.razor`, `AccountEditDialog.razor`, and `ResetPasswordDialog.razor`), while `Drawer.razor` still renders the Admin-gated "账号管理" nav link pointing at `/accounts` — a live 404. The rest of the app has moved to a newer visual vocabulary (toolbar + filter bar + server-paged `MudTable`), and the `AccountService` layer is live but carries dead fields (`AccountViewModel.DisplayName` has no backing column) and no last-login tracking. The redesign brings the account surface back online in the current style, adds last-login time and batch operations, and hardens the built-in `admin` account against modification.

## What Changes

- **Rewrite `Components/Account/Pages/Accounts.razor`** (`/accounts`, Admin-only): replace the flat first-gen table with the current list-page vocabulary — `MudPaper pa-4` toolbar (刷新 / 批量操作 toggle / 新建账号, the latter two Admin-gated), a new `AccountFilterBar` (用户名搜索 + 角色筛选 全部/Admin/Member), and a server-paged `MudTable` (`ServerData` + `Pager` + `NoRecordsContent`) with columns 用户名 / 角色 chip / 创建时间 / 最后登录 / 操作.
- **Add `LastLoginAt` (`DateTime?`) to `ApplicationUser`** — **BREAKING: DB schema change**. The schema is created by `db.Database.EnsureCreated()` with no EF migrations, so the existing `MultiClusterMgmtSys.db` must be deleted and regenerated (admin re-seeded with default password; all other registered accounts are lost).
- **Record last login**: `AuthService.LoginAsync` sets `user.LastLoginAt = DateTime.UtcNow` on the successful `PasswordSignInAsync` path (login page is the single authentication entry).
- **`AccountViewModel`**: remove the dead `DisplayName` field (display name is the username); add `LastLoginAt`. New `AccountQueryRequest` DTO (`SearchName`, `RoleFilter`, `Page`, `PageSize`) for the server-paged query, translated inside `AccountService` (no repository — accounts query lives in the service over `UserManager.Users`).
- **`AccountService` hardening**:
  - Built-in admin (username `"admin"`) is **immutable**: `UpdateAccountAsync`, `ResetPasswordAsync`, and `DeleteAccountAsync` reject it with a Chinese error.
  - Existing guards preserved: cannot delete self; the system must keep at least one `Admin`.
  - New `GetPagedAccountsAsync(TableState, AccountQueryRequest)` → `PagedResult<AccountViewModel>` (default sort 创建时间倒序, stable tie-breaker).
  - New `BatchDeleteAsync` / `BatchUpdateRoleAsync`: skip protected rows (built-in admin, current user, last-admin violation) and return a per-row skip summary for the UI to surface.
- **`AccountEditDialog` slimming**: role-only editing — username displayed read-only, display-name and password fields removed (password changes go through the dedicated reset dialog only).
- **`ResetPasswordDialog`** reused as-is; its row button is hidden for the built-in admin.
- **Built-in admin row presentation**: lock icon in the operations column instead of the edit/reset/delete icon buttons; batch-mode checkbox disabled.
- **New shared components** under `Components/Account/Shared/`: `AccountFilterBar.razor` and `AccountTable.razor`, mirroring the `Components/Clusters/Shared/` decomposition pattern.
- **Profile page out of scope**: `Profile.razor` remains commented out; it will be split out as a separate change.

## Capabilities

### New Capabilities

- `accounts-page`: Admin-only account management surface at `/accounts` — server-paged list with username/role filters, last-login time, role-only editing, password reset, single/batch delete, batch role change, and the immutable built-in `admin` account.

### Modified Capabilities

- (none — the existing `cluster-query-layering` spec governs the clusters repository contract; the account query lives entirely in `AccountService` over `UserManager.Users` and does not touch the clusters repository or the spec's `ClusterPageQuery`.)

## Impact

- **Code**:
  - `Components/Account/Pages/Accounts.razor` — rewrite (currently fully commented).
  - `Components/Account/Shared/AccountEditDialog.razor` — rewrite (currently fully commented); `ResetPasswordDialog.razor` — un-comment and reuse.
  - `Components/Account/Shared/AccountFilterBar.razor`, `AccountTable.razor` — new.
  - `Components/Account/Services/AccountService.cs` — paged query, batch operations, built-in-admin guards.
  - `Components/Auth/Services/AuthService.cs` — one-line `LastLoginAt` write on successful login.
  - `Components/Account/ViewModels/AccountViewModel.cs` — drop `DisplayName`, add `LastLoginAt`; new `Components/Account/Requests/AccountQueryRequest.cs`.
  - `Data/Entities/ApplicationUser.cs` — add `LastLoginAt`.
  - `Components/Layout/Drawer.razor` — no code change (its `/accounts` link becomes live again).
- **Namespaces**: `Components/Account/**` splits — ViewModels/Mappings use `MultiClusterMgmtSys.Features.Account.ViewModels[.Mappings]`, Services use `MultiClusterMgmtSys.Components.Account.Services` (per AGENTS.md); new files copy the namespace from a sibling in the same folder.
- **Services/APIs**: `AccountService` grows the methods above; `AuthService.LoginAsync` gains the last-login write. No service signature removed.
- **Database**: **BREAKING** — `ApplicationUser.LastLoginAt` added; `EnsureCreated()` will not alter the existing SQLite file, so `MultiClusterMgmtSys.db` must be deleted and regenerated at startup (admin re-seeded; other accounts lost).
- **Dependencies**: no new NuGet packages; MudBlazor 9 + existing Identity only.
- **Routes**: `/accounts` becomes a live, working route (currently 404).
- **Tests**: no test project exists in the repo; manual smoke verified via `dotnet build` + `dotnet run`.
