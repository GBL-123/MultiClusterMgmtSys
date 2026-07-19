## 1. Create CSS isolation file

- [x] 1.1 Create `MultiClusterMgmtSys/Components/Pages/Auth/Login.razor.css` with a header comment listing the `ThemeManager.cs` tokens used (Primary `#2563EB`, Surface `#FFFFFF`, TextPrimary `#0F172A`, TextSecondary `#475569`, Divider `#E2E8F0`, Error `#DC2626`, 6px radius, Body2 0.8125rem)
- [x] 1.2 Add `.mcm-field` (flex column, `gap: 8px`, `margin-bottom: 24px`)
- [x] 1.3 Add `.mcm-label` (`font-size: 0.8125rem`, `font-weight: 500`, `color: #475569`, `line-height: 1.5`)
- [x] 1.4 Add `.mcm-input` (height `44px`, padding `10px 14px`, `1px solid #E2E8F0`, `border-radius: 6px`, surface background, Body2 typography, `transition: border-color 200ms cubic-bezier(0.4, 0, 0.2, 1)`, `font-family: inherit`)
- [x] 1.5 Add `.mcm-input:hover` (`border-color: #CBD5E1`)
- [x] 1.6 Add `.mcm-input:focus` (`border-color: #2563EB`, `border-width: 2px`, no box-shadow — do not compensate padding, accept 1px outward shift)
- [x] 1.7 Add `.mcm-input::placeholder` (`color: #94A3B8`, `opacity: 1`)
- [x] 1.8 Add `.mcm-validation` (`font-size: 0.75rem`, `color: #DC2626`, `line-height: 1.4`, `min-height: 0`, `margin-top: 2px`)
- [x] 1.9 Add `.mcm-input:-webkit-autofill` rule that re-applies `#FFFFFF` background and a tinted `box-shadow: 0 0 0 1000px #FFFFFF inset` so the browser's yellow autofill is suppressed
- [x] 1.10 Add `.mcm-check` (inline-flex, `align-items: center`, `gap: 8px`, `cursor: pointer`, `user-select: none`, Body2 typography in `#475569`)
- [x] 1.11 Add `.mcm-check input[type="checkbox"]` (visually hidden: `position: absolute; opacity: 0; pointer-events: none;` — keep in tab order and screen-reader accessible)
- [x] 1.12 Add `.mcm-check__box` (`width/height: 18px`, `2px solid #94A3B8`, `border-radius: 2px`, transition `all 150ms ease`, inline-flex centered, relative positioning for the `::after` checkmark)
- [x] 1.13 Add `.mcm-check__box::after` (empty content, `5px × 9px` white borders, `transform: rotate(45deg) scale(0)`, `transition: transform 100ms ease 30ms`)
- [x] 1.14 Add `.mcm-check input:checked + .mcm-check__box` (background and border `#2563EB`)
- [x] 1.15 Add `.mcm-check input:checked + .mcm-check__box::after` (`transform: rotate(45deg) scale(1)` — animates checkmark in)
- [x] 1.16 Add `.mcm-check input:focus-visible + .mcm-check__box` (`box-shadow: 0 0 0 2px rgba(37, 99, 235, .2)`)

## 2. Restructure Login.razor markup

- [x] 2.1 In the username block, replace the `<div class="form-floating mb-3">` wrapper with `<div class="mcm-field">`, swap the `<label class="form-label">` to `class="mcm-label"`, and swap the `<InputText class="form-control">` to `class="mcm-input"`. Keep `id`, `@bind-Value`, `autocomplete`, `aria-required`, and `placeholder` attributes unchanged
- [x] 2.2 In the password block, apply the same swaps as 2.1; keep `type="password"` on the `InputText`
- [x] 2.3 Swap `class="text-danger"` on each `ValidationMessage` to `class="mcm-validation"`
- [x] 2.4 Replace the `<div class="checkbox mb-3"><label class="form-label"><InputCheckbox class="darker-border-checkbox form-check-input" /> 记住我</label></div>` block with `<label class="mcm-check mb-3"><InputCheckbox @bind-Value="Input.RememberMe" /><span class="mcm-check__box"></span><span>记住我</span></label>` — keep `@bind-Value` unchanged
- [x] 2.5 Confirm that `MudCard`, `MudIcon`, `MudText Typo="Typo.h5"`, `MudButton` (with `ButtonType="ButtonType.Submit"`), `MudDivider`, and `MudLink Href="/register"` are all still present and unchanged
- [x] 2.6 Confirm the failed-login error rendering `<MudText Color="Color.Error" Align="Align.Center" Class="mb-3">@errorMessage</MudText>` is unchanged (single-line red text, no banner)

## 3. Verify

- [x] 3.1 `rg -n "form-floating|form-control|form-label|form-check-input|darker-border-checkbox|text-danger" MultiClusterMgmtSys/Components/Pages/Auth/Login.razor` returns no matches
- [x] 3.2 `dotnet build MultiClusterMgmtSys.slnx` succeeds with no new warnings or errors
- [x] 3.3 `dotnet run --project MultiClusterMgmtSys` starts the app and `/login` renders without console errors
- [ ] 3.4 Manual visual check: focus the username field — border thickens to 2px and turns primary blue; tab to password — same affordance; tab to remember-me — focus ring appears on the custom box; press Space — checkmark appears
- [x] 3.5 Manual visual check: navigate to `/register` and confirm the register page renders unchanged (no `mcm-*` styles leaking)
- [ ] 3.6 Manual visual check: submit the login form with an empty username — the validation message renders in `#DC2626` directly below the input
- [ ] 3.7 Manual visual check: submit the login form with wrong credentials — the single-line red "用户名或密码错误" text appears above the username field, no banner background
