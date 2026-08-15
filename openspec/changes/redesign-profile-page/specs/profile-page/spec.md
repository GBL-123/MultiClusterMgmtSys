## ADDED Requirements

### Requirement: Profile page exists at /profile and requires authentication
The system SHALL provide a personal profile page at the `/profile` route, reachable by any authenticated user, and SHALL be the destination of the existing "个人资料" entry in the Drawer navigation.

#### Scenario: Authenticated user visits /profile
- **WHEN** an authenticated user visits `/profile`
- **THEN** the profile page renders with a read-only info card and a change-password card

#### Scenario: Unauthenticated user visits /profile
- **WHEN** an unauthenticated user visits `/profile`
- **THEN** the user is redirected to the login page

#### Scenario: Drawer navigation reaches the page
- **WHEN** the Drawer's "个人资料" link is clicked
- **THEN** it navigates to `/profile` and the page renders without a 404

### Requirement: Profile page shows read-only account information
The system SHALL display the signed-in user's username, role, registration time, and last login time on the profile page. All fields SHALL be read-only; the username SHALL be used as the identity directly, and there SHALL be no display-name concept.

#### Scenario: Rendering account fields
- **WHEN** the profile page loads for the signed-in user
- **THEN** it shows the username, role, registration time (`CreatedAt`), and last login time (`LastLoginAt`)

#### Scenario: Last login time formatting
- **WHEN** the user's `LastLoginAt` is null
- **THEN** the last-login field shows "—"
- **WHEN** the user's `LastLoginAt` has a value
- **THEN** the last-login field shows the time formatted as `yyyy-MM-dd HH:mm`

#### Scenario: No display name is shown
- **WHEN** the profile page renders
- **THEN** no display-name field or display-name editing control is shown, and the username is not editable

### Requirement: User can change their own password
The system SHALL allow the signed-in user to change their own password on the profile page by providing the current password, a new password, and a confirmation. The change SHALL go through `AccountService.ChangePasswordAsync`; a failed current-password check SHALL be reported with the message 当前密码错误, and a successful change SHALL clear the form fields.

#### Scenario: Successful password change
- **WHEN** the user enters the correct current password and a valid new password, and the confirmation matches
- **THEN** the password is updated, the form fields are cleared, and a success message is shown

#### Scenario: Wrong current password
- **WHEN** the user enters an incorrect current password
- **THEN** the change fails and the message 当前密码错误 is shown

#### Scenario: Confirmation mismatch
- **WHEN** the new password and the confirmation do not match
- **THEN** the change is refused and the message 两次输入的密码不一致 is shown

#### Scenario: Password validation rules
- **WHEN** the new password does not satisfy the identity password policy
- **THEN** the change fails and the Chinese validation error from the identity pipeline is shown

### Requirement: No display-name service stub remains
The system SHALL remove the unused `UpdateProfileAsync` method from `AccountService` once confirmed to have no remaining callers, together with the vestigial display-name save logic in the old page code.

#### Scenario: Removing the stub
- **WHEN** a repository-wide search confirms no caller of `UpdateProfileAsync` exists
- **THEN** the method is removed from `AccountService` and the project still builds
