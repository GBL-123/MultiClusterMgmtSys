## Why

The login page (`Components/Pages/Auth/Login.razor`) is the only page in the project that does not visually match the rest of the MudBlazor-based design system. It still uses ASP.NET Identity template scaffolding — `<InputText>` with Bootstrap `form-floating` / `form-control` / `form-label` classes, `<InputCheckbox>` with `form-check-input`, and a `darker-border-checkbox` helper in `app.css`. Switching between the polished Material-style Register page and the Bootstrap-style Login page breaks the visual continuity users expect.

MudBlazor's `MudTextField` cannot be used here because the login flow must interoperate with EF Core Identity's form submission pattern (`EditForm` + `OnSubmit` + `InputText` bound via `EditContext` + `HttpContext`-based external cookie sign-out). The constraint is therefore to **restyle the existing Identity-compatible components with custom CSS** that visually aligns to MudBlazor's outlined text field design language defined in `ThemeManager.cs`.

## What Changes

- Restyle `Login.razor` form controls (username, password, remember-me) with a custom CSS layer that mirrors MudBlazor's `MudTextField Variant="Outlined"` visual: 6 px border-radius, 1 px `#E2E8F0` border that thickens to 2 px `#2563EB` on focus, Body2 typography (0.8125 rem / 500), `#94A3B8` placeholder.
- Restyle the remember-me checkbox into a custom 18×18 square with primary-color fill and a CSS-drawn checkmark, replacing the native Bootstrap checkbox.
- Replace the `form-floating` markup with a static-label-above-input structure (Route B from the design discussion) for visual simplicity and easier CSS scoping.
- Introduce a CSS-isolation file `Login.razor.css` so the new styles are scoped to the login page only.
- Preserve all currently-working MudBlazor components: `MudCard`, `MudIcon`, `MudText` (h5 title), `MudButton`, `MudDivider`, `MudLink`, and the single-line `<MudText Color="Error">` error message.
- Out of scope: dark-mode adaptation, `Register.razor` changes, cleanup of the legacy Bootstrap residue in `wwwroot/app.css`.

## Capabilities

### New Capabilities

- `login-page`: Defines the visual and behavioral contract of the login page — the form structure, the styled control set, the error display convention, and the requirement that the page visually align to the MudBlazor design tokens defined in `ThemeManager.cs`.

### Modified Capabilities

None. No existing `openspec/specs/` capabilities are affected.

## Impact

- **Modified**: `MultiClusterMgmtSys/Components/Pages/Auth/Login.razor` — markup change (form-floating → custom field structure; native checkbox → custom checkbox).
- **New**: `MultiClusterMgmtSys/Components/Pages/Auth/Login.razor.css` — CSS isolation file with `mcm-*` class definitions.
- **Unchanged**:
  - `MultiClusterMgmtSys/Components/Pages/Auth/Register.razor` (out of scope this change).
  - `MultiClusterMgmtSys/Components/Theme/ThemeManager.cs` (color tokens reused, not modified).
  - `MultiClusterMgmtSys/Program.cs` (auth pipeline untouched).
  - `MultiClusterMgmtSys/wwwroot/app.css` (Bootstrap-form residue kept; deferred cleanup).
  - EF Core Identity wiring (`AccountService`, `IdentityRedirectManager`, `LoginRequest`).
- **No new dependencies**: relies only on standard CSS (no preprocessor, no CSS framework, no MudBlazor features beyond the already-imported `MudButton` etc.).
- **No breaking changes**: HTTP form contract (`FormName="login"`, `Input.UserName` / `Input.Password` / `Input.RememberMe`, `OnSubmit="LoginUser"`) preserved; server-side `AccountService.LoginAsync` flow unchanged.
- **Accessibility**: native `<label for="…">` / `<input id="…">` association preserved; checkbox uses visually-hidden input + `focus-visible` ring for keyboard users.
