# profile-page

## Purpose

Provide an authenticated `/profile` page where any signed-in user can view their read-only account information (username, role, registration/modification/last-login times) and change their own password. The page lives in its own feature folder (`Components/Profile/`), follows the site-wide page layout conventions, and removes the vestigial display-name concept and its service stub.

## Requirements

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
The system SHALL display the signed-in user's username, role, registration time, modification time, and last login time on the profile page. All fields SHALL be read-only; the username SHALL be used as the identity directly, and there SHALL be no display-name concept.

#### Scenario: Rendering account fields
- **WHEN** the profile page loads for the signed-in user
- **THEN** it shows the username, role, registration time (`CreatedAt`), modification time (`UpdatedAt`), and last login time (`LastLoginAt`)

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

### Requirement: Profile page lives in its own feature folder
The system SHALL implement the profile page in its own feature folder `Components/Profile/Pages/`, decoupled from the account management feature folder `Components/Account/`.

#### Scenario: Folder placement
- **WHEN** the components folder is inspected
- **THEN** the profile page exists at `Components/Profile/Pages/Profile.razor` and no profile page remains under `Components/Account/`

#### Scenario: Account management unaffected
- **WHEN** the `Components/Account/` folder is inspected
- **THEN** it hosts only account-management pages and no profile page

### Requirement: Profile page follows the site-wide page layout
The system SHALL render the profile page with the same layout conventions as the other pages: a header area showing the page title, and content sections presented as cards with section titles and label/value fields.

#### Scenario: Header area
- **WHEN** the profile page renders
- **THEN** a header area shows the page title 个人资料

#### Scenario: Card sections
- **WHEN** the profile page renders
- **THEN** the account information and the password change form each appear in a separate card with a section title

#### Scenario: Label/value field display
- **WHEN** account fields render
- **THEN** each field shows a secondary-styled label and a body-styled value, with "—" for missing values
