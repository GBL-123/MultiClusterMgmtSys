## MODIFIED Requirements

### Requirement: Profile page shows read-only account information
The system SHALL display the signed-in user's username, role, registration time, modification time, and last login time on the profile page. All fields SHALL be read-only; the username SHALL be used as the identity directly, and there SHALL be no display-name concept. Presentation SHALL follow the Swiss Industrial Print design system: a 40px amber square avatar (brand-mark language) showing the username's initial, a pale-fill role badge instead of a filled chip, and timestamps rendered in the mono data font. Per `display-conventions`, the role badge SHALL display the Chinese role as its primary text (Admin → 管理员, Member → 成员) with the raw role name (`Admin` / `Member`) as an adjacent secondary mono line.

#### Scenario: Rendering account fields
- **WHEN** the profile page loads for the signed-in user
- **THEN** it shows the username, role, registration time (`CreatedAt`), modification time (`UpdatedAt`), and last login time (`LastLoginAt`)

#### Scenario: Square avatar
- **WHEN** the profile page renders the user identity area
- **THEN** a 40px amber square shows the first character of the username in white, and no circular avatar is used

#### Scenario: Role badge presentation
- **WHEN** the profile page renders the user's role
- **THEN** the role is shown as a pale-fill badge (amber tint for Admin, neutral tint otherwise) with deep text, not a filled chip
- **AND** the badge primary text is 管理员 or 成员, with the raw `Admin` / `Member` as a secondary mono line

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
