## 1. Service Cleanup

- [x] 1.1 Remove the `UpdateProfileAsync` stub method from `AccountService` (only caller is the commented-out old page, confirmed by search)
- [x] 1.2 Verify the project builds after removal

## 2. Profile Page Implementation

- [x] 2.1 Rewrite `Components/Profile/Pages/Profile.razor` uncommented (new feature folder, old `Components/Account/Pages/Profile.razor` removed), with `@page "/profile"` and `@attribute [Authorize]`
- [x] 2.2 Load the signed-in user on init via `AuthStateProvider` + `AccountService.GetUserByNameAsync`, with loading indicator and error snackbar on failure
- [x] 2.3 Render a read-only info card (MudPaper) with username, role (from claims), `CreatedAt`, and `LastLoginAt` formatted `yyyy-MM-dd HH:mm` (null → "—")
- [x] 2.4 Render the change-password card (MudCard) with current/new/confirm password fields (required, immediate validation, password input type)
- [x] 2.5 Wire the 修改密码 button to `AccountService.ChangePasswordAsync` with: confirmation mismatch check (两次输入的密码不一致), `PasswordMismatch` → 当前密码错误 mapping, success clears fields + success snackbar, disabled state + progress spinner while saving
- [x] 2.6 Drop all vestigial display-name code (SaveDisplayName, savingDisplayName, UpdateProfileAsync call) from the new page
- [ ] 2.7 Build the solution and manually verify: login → Drawer 个人资料 → page renders with info card + password card, wrong current password shows 当前密码错误, successful change clears fields
