# Capability: login-page

Defines the visual and behavioral contract of the `/login` page (`Components/Pages/Auth/Login.razor`) so that the page matches the MudBlazor design language defined in `ThemeManager.cs` while preserving the EF Core Identity form-submission contract.

## ADDED Requirements

### Requirement: Visual alignment with ThemeManager design tokens

The login page MUST render with colors, typography, and geometry sourced from the palette and typography tables in `Components/Theme/ThemeManager.cs`. Concretely, the page MUST use:

- Primary `#2563EB` (focus / checked states).
- Surface `#FFFFFF` (input background, card background).
- TextPrimary `#0F172A` (input text).
- TextSecondary `#475569` (static labels).
- Divider `#E2E8F0` (default input border).
- Error `#DC2626` (validation messages, single-line login-error text).
- Body2 typography: `0.8125rem` / `400` weight / `1.5` line-height for input text and labels.
- Caption typography: `0.75rem` for validation messages.
- `6px` border-radius on all rectangular input controls and the card.
- The same Tailwind-style utility classes already used elsewhere in the project (`d-flex`, `align-center`, `justify-center`, `mb-4`, etc.) for layout.

#### Scenario: Input matches outlined MudBlazor field visual
- **WHEN** the `/login` page is rendered with the default light theme
- **THEN** the username and password inputs render with a `1px` `#E2E8F0` border, `6px` border-radius, `#FFFFFF` background, `0.8125rem` text in `#0F172A`, and the static label above each input renders in `#475569` at `0.8125rem / 500`

#### Scenario: Focused input adopts primary affordance
- **WHEN** a user focuses the username or password input via keyboard or pointer
- **THEN** the input border thickens to `2px` and changes color to `#2563EB`, with no `box-shadow`

#### Scenario: Validation message renders below input in error color
- **WHEN** a field-level `ValidationMessage` is rendered (e.g. empty username on submit)
- **THEN** the message text appears in `#DC2626` at `0.75rem` directly below the corresponding input

### Requirement: EF Core Identity form-post contract preserved

The login page MUST continue to interoperate with the existing EF Core Identity authentication flow without modifying `AccountService`, `IdentityRedirectManager`, `Program.cs`, or the cookie authentication options. The rendered form MUST satisfy all of the following:

- `@page "/login"` and `@layout EmptyLayout`.
- An `<EditForm>` with `EditContext`, `method="post"`, `OnSubmit="LoginUser"`, and `FormName="login"`.
- A `SupplyParameterFromForm InputModel Input` with public properties `UserName` (string), `Password` (string), `RememberMe` (bool).
- A `SupplyParameterFromQuery string? ReturnUrl`.
- A `CascadingParameter HttpContext HttpContext` that the `OnInitializedAsync` method uses to sign out the external cookie on GET.
- A `<DataAnnotationsValidator />` inside the form.
- A `LoginUser` method that calls `editContext.Validate()` first, then `AccountService.LoginAsync(new LoginRequest(Input.UserName, Input.Password, Input.RememberMe))`.

#### Scenario: Successful form post reaches the existing login pipeline
- **WHEN** a user submits valid credentials from `/login`
- **THEN** `AccountService.LoginAsync` is invoked with the same `LoginRequest` shape as before and the response is handled by the existing `LoginUser` method

#### Scenario: Invalid credentials surface a single-line error
- **WHEN** `AccountService.LoginAsync` returns a `SignInResult` that is not `Succeeded`
- **THEN** the value `"用户名或密码错误"` is rendered as a single line of red text (`MudText` with `Color="Color.Error"`) above the username field, with no banner background

#### Scenario: External cookie cleared on GET
- **WHEN** the user navigates to `/login` via HTTP GET
- **THEN** `HttpContext.SignOutAsync(IdentityConstants.ExternalScheme)` is awaited before the form renders, identical to the prior behavior

### Requirement: Custom field structure with static label

The login page MUST use a custom field structure (no `form-floating`, no Bootstrap form classes) for the username and password inputs. Each field MUST consist of:

- A `<div class="mcm-field">` wrapper that arranges label, input, and validation message in a vertical flex column with `8px` gap and `24px` (`mb-4`) bottom margin.
- A `<label class="mcm-label" for="<input-id>">` element above the input.
- An `<InputText>` (or equivalent Identity-compatible `InputBase<string>`) with `class="mcm-input"`, the `id` referenced by the label, and the appropriate `autocomplete` value (`username webauthn` for username, `current-password` for password).
- A `<ValidationMessage>` with `class="mcm-validation"`.

The login page MUST NOT contain any element with class `form-floating`, `form-control`, `form-label`, or `text-danger`.

#### Scenario: Markup no longer references Bootstrap form classes
- **WHEN** `Login.razor` is grepped for the legacy Bootstrap form class names
- **THEN** no matches for `form-floating`, `form-control`, `form-label`, or `text-danger` are returned

#### Scenario: Native label-input association retained
- **WHEN** a user clicks the "用户名" label in the rendered page
- **THEN** the username input receives focus (verified by the `for` / `id` pairing and the standard browser behavior)

### Requirement: Custom remember-me checkbox

The remember-me checkbox MUST be implemented as a visually hidden native `<InputCheckbox>` paired with a sibling `<span class="mcm-check__box">` that draws the visual. The wrapping `<label class="mcm-check">` MUST contain the hidden input, the visual box, and the text "记住我" in that order.

The visual box MUST:

- Render as an `18px × 18px` square with `2px` border in `#94A3B8` and `2px` border-radius when unchecked.
- Render with `#2563EB` background and `#2563EB` border when checked, with a white checkmark drawn via CSS borders + `transform: rotate(45deg)`.
- Display a `0 0 0 2px rgba(37, 99, 235, .2)` focus ring when the native input has `:focus-visible`.

The login page MUST NOT use `class="form-check-input"` or `class="darker-border-checkbox form-check-input"`.

#### Scenario: Checked state visually matches MudBlazor primary checkbox
- **WHEN** the remember-me checkbox is checked (the bound `Input.RememberMe` is `true`)
- **THEN** the `mcm-check__box` element renders with `#2563EB` background and a visible white checkmark

#### Scenario: Checkbox remains keyboard-accessible
- **WHEN** a user tabs to the remember-me control
- **THEN** the hidden native `<InputCheckbox>` receives focus and the visual box shows the `0 0 0 2px rgba(37, 99, 235, .2)` ring

#### Scenario: Spacebar toggles the checkbox
- **WHEN** the remember-me control has keyboard focus and the user presses Space
- **THEN** the underlying `Input.RememberMe` value flips and the visual check state updates accordingly

### Requirement: MudBlazor page chrome preserved

The login page MUST continue to use the following MudBlazor components (all of which render correctly under the current static SSR form-post flow):

- `MudCard` with `Elevation="8"` and `pa-8` for the centered form container.
- `MudIcon Icon="@Icons.Material.Filled.TravelExplore"` with `Size="Size.Large"` and `Color="Color.Primary"` for the page glyph.
- `MudText Typo="Typo.h5" Align="Align.Center"` for the "多集群管理系统" title.
- `MudButton Variant="Variant.Filled" Color="Color.Primary" FullWidth="true" Size="Size.Large" ButtonType="ButtonType.Submit"` for the "登 录" submit button.
- `MudDivider` between the submit button and the register link.
- `MudLink Href="/register" Underline="Underline.Always"` for the "立即注册" link.

The `@using MudBlazor` import MUST remain active in the file.

#### Scenario: All chrome components still render
- **WHEN** the `/login` page is loaded
- **THEN** the page contains a `MudCard`, a `MudIcon`, a `MudText` title, a `MudButton` submit, a `MudDivider`, and a `MudLink` to `/register`, in that order

### Requirement: Styles scoped via Blazor CSS isolation

The new styles MUST live in `MultiClusterMgmtSys/Components/Pages/Auth/Login.razor.css` (Blazor CSS isolation) and MUST apply only to the `Login.razor` component. The styles MUST NOT be added to `MultiClusterMgmtSys/wwwroot/app.css`.

The styles MUST define, at minimum: `.mcm-field`, `.mcm-label`, `.mcm-input` (with `:hover`, `:focus`, `::placeholder` variants), `.mcm-validation`, `.mcm-check` (with the wrapping `label`), `.mcm-check__box` (with `::after` checkmark and `input:checked + .mcm-check__box` / `input:focus-visible + .mcm-check__box` variants).

A `-webkit-autofill` rule MUST be present to suppress the browser's default yellow autofill background and re-apply the surface color while keeping text legible.

#### Scenario: Styles do not leak to Register.razor
- **WHEN** the user navigates to `/register` after the change is deployed
- **THEN** the register page renders unchanged (no `mcm-*` class names appear in its DOM, and the `mcm-*` rules have no observable effect on its inputs)

#### Scenario: Autofill does not break the input visual
- **WHEN** the browser autofills the username or password field
- **THEN** the field background remains the surface color (not the browser's default yellow) and the input border is unaffected

### Requirement: Out of scope for this change

This change MUST NOT modify any of the following (each is deferred to a separate change):

- `MultiClusterMgmtSys/Components/Pages/Auth/Register.razor`.
- `MultiClusterMgmtSys/Components/Theme/ThemeManager.cs`.
- `MultiClusterMgmtSys/Program.cs` and the EF Core Identity wiring.
- `MultiClusterMgmtSys/wwwroot/app.css` (the legacy `.form-floating` and `.darker-border-checkbox` rules remain in place but are inert because the new markup does not reference them).
- Any dark-mode adaptation of the login page (the new CSS uses hard-coded light-palette values, not `var(--mud-palette-*)`).

#### Scenario: Dark mode behavior is unchanged from current state
- **WHEN** the user toggles the app into dark mode and visits `/login`
- **THEN** the page renders with the same hard-coded light-palette values as in light mode (i.e. the login page is not yet dark-mode-aware; this is the accepted trade-off for this change)
