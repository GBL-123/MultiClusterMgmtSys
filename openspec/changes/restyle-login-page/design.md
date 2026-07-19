## Context

The project is a Blazor Server (Interactive Server render mode) app on .NET 10 with MudBlazor 9.7.0 as the primary UI library and EF Core Identity (SQLite) for authentication. All authenticated pages (`AppBar`, `Drawer`, `Clusters`, `Nodes`, `ConfigMaps`, `Accounts`, `Profile`, `Register`) use MudBlazor components and inherit visual tokens from a single `ThemeManager` (`Components/Theme/ThemeManager.cs`) that defines:

- **Light palette**: Primary `#2563EB`, Surface `#FFFFFF`, Background `#F8FAFC`, TextPrimary `#0F172A`, TextSecondary `#475569`, Divider `#E2E8F0`, Error `#DC2626`, default border-radius `6px`.
- **Typography**: system font stack; H4–H6 600 weight with negative letter spacing; Body2 `0.8125rem / 400 / 1.5`; Button `0.8125rem / 500 / 1.5 / 0.02em`; Caption `0.75rem / 400 / 1.4`.

`Components/Pages/Auth/Login.razor` is the lone outlier. It still ships the ASP.NET Identity template markup — `<InputText>` inside `<div class="form-floating">`, `<InputCheckbox class="form-check-input darker-border-checkbox">`, `<ValidationMessage class="text-danger">` — and `wwwroot/app.css` carries the matching Bootstrap residue (`.form-floating` placeholder rules and `.darker-border-checkbox.form-check-input { border-color: #929292; }`).

The login flow binds via `EditForm` with `EditContext` + `OnSubmit` and uses `Input.UserName` / `Input.Password` / `Input.RememberMe` as `SupplyParameterFromForm` properties. `Login.razor` also signs out the external cookie via `HttpContext.SignOutAsync(IdentityConstants.ExternalScheme)`, which depends on the static SSR form-post contract. Substituting MudBlazor's `MudTextField` (which relies on JS interop and interactive render mode) is not viable without rewriting the form-submission flow. The same constraint does **not** apply to `MudButton`, `MudCard`, `MudIcon`, `MudText`, `MudDivider`, and `MudLink`, which render correctly in both static and interactive contexts and are kept in this change.

## Goals / Non-Goals

**Goals:**

- Make `Login.razor` visually indistinguishable in design language from the rest of the app — same colors, same border-radius, same typography scale, same focus affordance.
- Preserve the EF Core Identity form-post contract exactly: `FormName="login"`, `Input.UserName` / `Input.Password` / `Input.RememberMe` property names, `OnSubmit="LoginUser"`, `HttpContext` access, and `AccountService.LoginAsync(LoginRequest(...))` call site.
- Achieve the visual with hand-written CSS only (no preprocessor, no Bootstrap, no MudBlazor form controls), in a Blazor CSS-isolation file scoped to `Login.razor`.
- Reuse the existing color and typography tokens from `ThemeManager.cs` as hard-coded values (no CSS-variable indirection yet, since dark mode is out of scope).

**Non-Goals:**

- Dark-mode adaptation of the login page (deferred).
- Restyling `Register.razor` or any other page.
- Modifying `ThemeManager.cs`, the EF Core Identity wiring, or `Program.cs`.
- Cleaning up the legacy `.form-floating` / `.darker-border-checkbox` rules in `wwwroot/app.css` (deferred; they remain inert because the new markup does not reference them).
- Replacing the working MudBlazor components (`MudButton`, `MudCard`, `MudIcon`, `MudText`, `MudDivider`, `MudLink`) with plain HTML.
- Adding a "forgot password" link, social login, MFA, or any new auth feature.

## Decisions

### D1. Static label above input (Route B), not the floating-label `form-floating` approach

- **Rationale**: Pure-CSS replication of MudBlazor's notched outlined label (where the label appears to cut a gap into the border) is brittle across browsers and conflicts with Bootstrap's own `form-floating` cutout mechanics. A static label above the input gives the same "Material outlined" feel (label is a separate typographic element, input is a single outlined box) without the notched-outline complexity. The trade-off is the loss of the floating-label animation; accepted because the login page is rarely revisited and the simpler structure is easier to maintain.
- **Alternatives considered**:
  - Route A (restyle existing `form-floating`): rejected because the resulting cutout still does not visually match MudBlazor's gapped notch.
  - Route C (CSS-only notched outline with `box-shadow` stack): rejected due to maintenance cost and dark-mode-readiness cost (deferred for now).

### D2. Custom `mcm-*` class names, simple flat naming (not BEM)

- **Rationale**: The project does not have a CSS class-naming convention visible in the codebase — it relies on MudBlazor's `mud-` classes and Tailwind-style utility classes (`d-flex`, `pa-4`, `mb-3`). Introducing BEM (`mcm-field__label`) for ~6 new classes would be over-engineering. Flat names (`mcm-field`, `mcm-label`, `mcm-input`, `mcm-check`, `mcm-validation`) read clearly and are easy to grep.
- **Alternative considered**: BEM. Rejected for the reason above.

### D3. CSS-isolation file `Login.razor.css`, not additions to `app.css`

- **Rationale**: The new styles are only relevant to the login page. CSS isolation guarantees they cannot leak to `Register.razor` or any future page, and they live next to the component file. `app.css` is reserved for true globals and currently still hosts the legacy Bootstrap residue; mixing the new styles in would muddy that boundary.
- **Alternative considered**: Adding to `app.css` under a `mcm-` namespace. Rejected for leakage risk and weaker locality.

### D4. Focus border thickens 1px → 2px with no padding compensation (Route B border behavior)

- **Rationale**: User-selected for animation feel. The 1 px outward "jump" on focus reinforces the affordance. MudBlazor's actual focus behavior keeps the border at 1 px and only changes color; this is a deliberate deviation in exchange for stronger focus feedback. The visual jump is small (1 px) and acceptable for a one-field-per-page form.
- **Mitigation if it feels too jumpy in practice**: future change can switch to `border-color` change + `box-shadow: 0 0 0 2px rgba(37, 99, 235, .2)` for an outward glow without thickness change.
- **Alternative considered**: 1 px → 1 px color change with focus ring. Rejected by user for this change; kept as a known follow-up if needed.

### D5. Looser vertical density (44 px input height, `mb-4` between fields)

- **Rationale**: User-selected. The login page is an occasional-use page, not a data-entry grid, so slightly more breathing room improves scannability. MudBlazor's `Margin="Dense"` (~40 px) is tuned for dense tables; the login form does not need that density.
- **Concrete values**: input `height: 44px`, `padding: 10px 14px`, label-to-input gap `8px`, field-to-field `margin-bottom: 24px` (`mb-4`).

### D6. Custom checkbox built on a hidden native input + `::after` checkmark

- **Rationale**: Native `<input type="checkbox">` cannot be styled to look like MudBlazor's filled primary-color square with a white check. Hiding the native input (via `position: absolute; opacity: 0;`) preserves accessibility (still focusable, still announced by screen readers) while letting a sibling `<span class="mcm-check__box">` carry the visual. The checkmark is drawn with CSS borders + `transform: rotate(45deg)` to avoid an SVG asset.
- **Focus state**: `input:focus-visible + .mcm-check__box { box-shadow: 0 0 0 2px rgba(37, 99, 235, .2); }` — same focus-ring convention as the text inputs.
- **Alternative considered**: keeping the native checkbox and styling it. Rejected because cross-browser checkbox styling is inconsistent and the resulting visual would still not match MudBlazor.

### D7. Single-line `<MudText Color="Error">` for failed-login error, not a banner

- **Rationale**: User-selected. Kept because it matches the existing minimalism of the form and avoids the visual weight of a banner. The error appears once at the top of the form, just above the username field, immediately after a failed submit.
- **Alternative considered**: `MudAlert` banner. Rejected for this change; can be revisited in a later pass.

### D8. Colors hard-coded from `ThemeManager.cs` palette, not via CSS custom properties

- **Rationale**: Dark mode is out of scope this change, so introducing `var(--mud-palette-divider)` etc. would add indirection without immediate benefit. Hard-coded values are easy to read and the palette is small (8 colors). When dark mode is later required, a follow-up change can refactor these to CSS variables without touching the markup.
- **Alternative considered**: CSS variables referencing MudBlazor's variables. Deferred.

## Risks / Trade-offs

- **[Risk] Focus border "jump" feels like a layout bug to some users** → Mitigation: D4 documents the trade-off; a follow-up can switch to color-only + focus ring if feedback warrants.
- **[Risk] Hard-coded colors diverge from `ThemeManager.cs` palette if palette is later changed** → Mitigation: comment block at the top of `Login.razor.css` lists the theme tokens used; a one-line grep across the project catches any future palette change.
- **[Risk] CSS isolation prevents sharing with a future restyled `Register.razor`** → Mitigation: when (if) `Register.razor` is restyled, the `mcm-*` classes can be promoted to `app.css` or extracted into a shared `auth.css` partial imported by both. Trivial refactor.
- **[Risk] Custom checkbox is harder to maintain than a real component** → Mitigation: scoped to a single page, ~20 lines of CSS, no JS. Acceptable.
- **[Risk] `EditForm` with static SSR + custom-styled inputs may have validation-message DOM ordering subtleties** → Mitigation: keep `ValidationMessage` rendered as the last child of each `mcm-field`, so it always sits below the input regardless of focus state.
- **[Risk] Browser autofill (Chrome yellow background) overrides the white surface** → Mitigation: add `:-webkit-autofill` rule that re-applies the surface color and a slightly tinted `box-shadow` to preserve the input silhouette; will be added in tasks.
- **[Trade-off] Lose the floating-label affordance** → Accepted; static label is the route the user picked.
