# accounts-page

## Purpose

Admin-only account management over ASP.NET Identity accounts: a server-paged account list with username search and role filter, last-login tracking, and guarded single/batch operations that protect the built-in admin account, the current session, and last-admin availability.

## Requirements

### Requirement: Admin-only account management page

The system SHALL provide an account management page at the `/accounts` route, reachable only by users with the `Admin` role. The page SHALL be the destination of the existing "账号管理" entry in the Drawer navigation.

#### Scenario: Admin navigates to the page
- **WHEN** an authenticated user with the `Admin` role visits `/accounts`
- **THEN** the account management page renders with a toolbar, a filter bar, and an account table

#### Scenario: Non-admin access is blocked
- **WHEN** a user without the `Admin` role visits `/accounts`
- **THEN** the page is not rendered and access is denied

#### Scenario: Page was previously a dead link
- **WHEN** the Drawer's "账号管理" link is clicked
- **THEN** it navigates to `/accounts` and the page renders without a 404

### Requirement: Server-paged account list with filters

The system SHALL render the account list as a server-paged `MudTable` driven by `AccountService.GetPagedAccountsAsync(TableState, AccountQueryRequest)`, which returns `PagedResult<AccountViewModel>`. `AccountQueryRequest` SHALL carry a username search string, an optional role filter, page, and page size. The query SHALL be translated entirely inside `AccountService` over `UserManager.Users` (no repository, no import of the clusters repository or its query types).

#### Scenario: Search by username
- **WHEN** `AccountQueryRequest.SearchName` is a non-empty string
- **THEN** the query filters accounts whose username contains the string (case-insensitive)

#### Scenario: Role filter
- **WHEN** `AccountQueryRequest.RoleFilter` is set to a role name
- **THEN** the query filters accounts having that role
- **WHEN** `AccountQueryRequest.RoleFilter` is null
- **THEN** no role filter is applied

#### Scenario: Default sort and paging
- **WHEN** no sort label is provided
- **THEN** accounts are ordered by `CreatedAt` descending with a stable tie-breaker
- **WHEN** `Page` or `PageSize` is less than 1
- **THEN** the value is clamped to 1 before `Skip`/`Take`

#### Scenario: Table state translated in the service
- **WHEN** a Razor page calls `GetPagedAccountsAsync(TableState, query)`
- **THEN** the service maps `TableState.Page` (0-based) to the 1-based page and `TableState.SortDirection` to descending/ascending before delegating to the query

#### Scenario: Last login time displayed
- **WHEN** the table renders an account row whose `LastLoginAt` is null
- **THEN** the 最后登录 column shows "—"
- **WHEN** the table renders an account row whose `LastLoginAt` has a value
- **THEN** the 最后登录 column shows the time formatted as `yyyy-MM-dd HH:mm`

### Requirement: Last login time recording

The system SHALL record the last successful login time on the `ApplicationUser` entity (`LastLoginAt`, nullable `DateTime`). The field SHALL be written only on the successful password-sign-in path, and SHALL NOT affect existing accounts that have never logged in (their value stays null).

#### Scenario: Successful login writes the timestamp
- **WHEN** `AuthService.LoginAsync` completes with a successful `PasswordSignInAsync` result
- **THEN** the signed-in user's `LastLoginAt` is set to the current UTC time and persisted

#### Scenario: Failed login does not write the timestamp
- **WHEN** `AuthService.LoginAsync` completes with a failed result
- **THEN** the user's `LastLoginAt` is not modified

#### Scenario: Existing user with no record
- **WHEN** an account was created before this change and has never logged in since
- **THEN** its `LastLoginAt` is null and the list displays "—"

### Requirement: Immutable built-in admin account

The system SHALL treat the account with username `admin` as immutable: role editing, password reset, and deletion SHALL be rejected by `AccountService` with a Chinese error message, and SHALL NOT be offered in the UI. The account SHALL be excluded from batch selection and batch operations.

#### Scenario: Edit role is rejected
- **WHEN** `UpdateAccountAsync` is called for the built-in admin account
- **THEN** it returns a failed `IdentityResult` with a Chinese error indicating the built-in admin cannot be modified

#### Scenario: Password reset is rejected
- **WHEN** `ResetPasswordAsync` is called for the built-in admin account
- **THEN** it returns a failed `IdentityResult` with a Chinese error indicating the built-in admin cannot be modified

#### Scenario: Delete is rejected
- **WHEN** `DeleteAccountAsync` is called for the built-in admin account
- **THEN** it returns a failed `IdentityResult` with a Chinese error indicating the built-in admin cannot be deleted

#### Scenario: Row shows lock instead of actions
- **WHEN** the table renders the built-in admin row
- **THEN** the operations column shows a lock icon instead of the edit / reset-password / delete buttons, and the batch checkbox is disabled

### Requirement: Single account operations with guards

The system SHALL support creating, editing, and deleting single accounts, and resetting a single account's password. Creation SHALL require a username, a password, and an existing role; editing SHALL change the role only (username read-only, no display name, no password field). Deletion SHALL be refused for the current signed-in user and SHALL never leave the system without at least one `Admin`.

#### Scenario: Create account with role
- **WHEN** a new account is created with username, password, and a role that exists
- **THEN** the account is created and assigned that role
- **WHEN** the role does not exist
- **THEN** creation fails with a Chinese error naming the missing role

#### Scenario: Edit changes role only
- **WHEN** an account is edited with a different role
- **THEN** the account's role is replaced by the new role and the username is unchanged
- **WHEN** the edit dialog is open in edit mode
- **THEN** the username field is read-only and no display-name or password fields are shown

#### Scenario: Cannot delete the current user
- **WHEN** `DeleteAccountAsync` targets the currently signed-in user's id
- **THEN** it returns a failed `IdentityResult` with the error 不能删除当前登录账号

#### Scenario: Must keep at least one admin
- **WHEN** `DeleteAccountAsync` targets an admin and the system has only one admin left
- **THEN** it returns a failed `IdentityResult` with the error 系统中必须至少保留一个 Admin 账号

### Requirement: Batch operations with protected-row skipping

The system SHALL support batch delete and batch role change from the list page's batch mode. Both operations SHALL process eligible rows and skip protected rows — the built-in admin, the currently signed-in user, and, for demotion or deletion, any admin whose removal would leave the system without an `Admin`. The UI SHALL report how many accounts were processed and how many were skipped.

#### Scenario: Batch delete skips protected rows
- **WHEN** a batch delete targets several accounts including the built-in admin, the current user, and the last remaining admin
- **THEN** the protected rows are skipped, the eligible rows are deleted, and the result reports the skipped count

#### Scenario: Batch role change skips protected rows
- **WHEN** a batch role change to `Member` targets the built-in admin, the current user, or an admin whose demotion would leave zero admins
- **THEN** those rows are skipped and the result reports the skipped count
- **WHEN** a batch role change to `Admin` targets eligible non-admin accounts
- **THEN** those accounts are promoted and the result reports the processed count

#### Scenario: Batch UI
- **WHEN** batch mode is active and at least one account is selected
- **THEN** a batch bar shows the selected count with actions 批量改角色 (Admin/Member), 批量删除, and 清空选择
- **WHEN** a batch operation skips rows
- **THEN** the page shows a message such as 跳过 N 个受保护账号
