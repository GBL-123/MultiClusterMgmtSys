## Why

登录失败时，当前实现通过整页重定向回到 `/login?error=1`，导致用户已输入的用户名和密码都被清空。用户需要重新输入用户名，体验不佳——尤其是用户名较长或记不清是密码错还是用户名错时。应保留用户名、仅清空密码，让用户直接重试密码。

## What Changes

- `/api/login` 端点登录失败时的重定向 URL 增加 `username` 参数：`/login?error=1&returnUrl=...&username=...`。
- `Login.razor` 的 `OnInitializedAsync` 解析 query string 中的 `username` 参数，预填到用户名输入框。
- 密码框始终为空（不回传、不预填），符合安全惯例。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `authentication`: 「登录流程」需求的「登录失败」场景变更——失败时保留用户名，仅清空密码。

## Impact

- **修改文件**：`Program.cs`（`/api/login` 端点失败重定向 URL 增加 `username` 参数）、`Components/Pages/Login.razor`（`OnInitializedAsync` 解析 `username` query 参数预填用户名框）。
- **不改文件**：`AccountService.cs`、`AccountRepository.cs`、`Routes.razor`、`EmptyLayout.razor`、`AppBar.razor`。
- **数据库**：无变更。
- **依赖**：无新增。
