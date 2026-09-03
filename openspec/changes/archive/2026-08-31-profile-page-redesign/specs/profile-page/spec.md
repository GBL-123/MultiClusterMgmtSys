## MODIFIED Requirements

### Requirement: Profile page shows read-only account information
The system SHALL display the signed-in user's username, role, registration time, modification time, and last login time on the profile page. All fields SHALL be read-only; the username SHALL be used as the identity directly, and there SHALL be no display-name concept. Presentation SHALL follow the Swiss Industrial Print design system: a 40px amber square avatar (brand-mark language) showing the username's initial, a pale-fill role badge instead of a filled chip, and timestamps rendered in the mono data font.

#### Scenario: Rendering account fields
- **WHEN** the profile page loads for the signed-in user
- **THEN** it shows the username, role, registration time (`CreatedAt`), modification time (`UpdatedAt`), and last login time (`LastLoginAt`)

#### Scenario: Square avatar
- **WHEN** the profile page renders the user identity area
- **THEN** a 40px amber square shows the first character of the username in white, and no circular avatar is used

#### Scenario: Role badge presentation
- **WHEN** the profile page renders the user's role
- **THEN** the role is shown as a pale-fill badge (amber tint for Admin, neutral tint otherwise) with deep text, not a filled chip

#### Scenario: Timestamp font
- **WHEN** registration, modification, or last-login time renders
- **THEN** the value is rendered in the mono data font (`.font-mono`)

#### Scenario: Last login time formatting
- **WHEN** the user's `LastLoginAt` is null
- **THEN** the last-login field shows "—"
- **WHEN** the user's `LastLoginAt` has a value
- **THEN** the last-login field shows the time formatted as `yyyy-MM-dd HH:mm`

#### Scenario: Modification time formatting
- **WHEN** the user's `UpdatedAt` is null
- **THEN** the modification-time field shows "—"
- **WHEN** the user's `UpdatedAt` has a value
- **THEN** the modification-time field shows the time formatted as `yyyy-MM-dd HH:mm`

#### Scenario: No display name is shown
- **WHEN** the profile page renders
- **THEN** no display-name field or display-name editing control is shown, and the username is not editable

### Requirement: User can change their own password
The system SHALL allow the signed-in user to change their own password by providing the current password, a new password, and a confirmation, entered in a password-change dialog opened from the account information card. The change SHALL go through `AccountService.ChangePasswordAsync`; a failed current-password check SHALL be reported with the message 当前密码错误, and a successful change SHALL close the dialog and clear the form fields.

#### Scenario: Opening the dialog
- **WHEN** the user clicks the 修改密码 button on the account information card
- **THEN** a password-change dialog opens with fields for current password, new password, and confirmation, each with a visibility toggle

#### Scenario: Successful password change
- **WHEN** the user enters the correct current password and a valid new password, and the confirmation matches
- **THEN** the password is updated, the dialog closes, and a success message is shown

#### Scenario: Wrong current password
- **WHEN** the user enters an incorrect current password
- **THEN** the change fails and the message 当前密码错误 is shown

#### Scenario: Confirmation mismatch
- **WHEN** the new password and the confirmation do not match
- **THEN** the change is refused and the message 两次输入的密码不一致 is shown

#### Scenario: Password validation rules
- **WHEN** the new password does not satisfy the identity password policy
- **THEN** the change fails and the Chinese validation error from the identity pipeline is shown

## ADDED Requirements

### Requirement: Profile page shows recent operations
The system SHALL display a 最近操作 card below the account information card showing the signed-in user's own most recent audit log entries, at most 5, ordered by creation time descending. Each row SHALL show the time (mono font, `yyyy-MM-dd HH:mm:ss`), the category and action display names, and the target. A 查看全部 link SHALL navigate to `/audit-logs`. When there are no entries, the card SHALL show an empty state.

#### Scenario: Recent operations render
- **WHEN** the user has audit log entries
- **THEN** the card shows at most 5 of the user's own entries, newest first, each with mono timestamp, category · action, and target

#### Scenario: Scope is limited to the current user
- **WHEN** the current user is an administrator with other users' audit entries present
- **THEN** only the current user's own entries are listed, not other users'

#### Scenario: View all link
- **WHEN** the user clicks 查看全部
- **THEN** the browser navigates to `/audit-logs`

#### Scenario: Empty state
- **WHEN** the user has no audit log entries
- **THEN** the card shows an empty state placeholder (e.g. `[ 暂无操作记录 ]`)