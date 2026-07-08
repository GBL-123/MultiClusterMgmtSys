## MODIFIED Requirements

### Requirement: 登录流程

系统 SHALL 提供独立登录页 `/login`，使用原生 HTML form POST 提交到 `/api/login` 端点，支持用户名 + 密码 + 记住我。登录失败时 SHALL 保留用户已输入的用户名，仅清空密码。

#### Scenario: 登录成功

- **WHEN** 用户在 `/login` 填写正确凭据并提交（form POST 到 `/api/login`）
- **THEN** 端点调 `AccountService.ValidateCredentialsAsync` 校验通过，`SignInAsync` 写 Cookie，返回 `Results.LocalRedirect(returnUrl)` 整页跳转，新电路读 Cookie 已认证

#### Scenario: 登录失败

- **WHEN** 用户填写错误凭据并提交
- **THEN** 端点返回 `Results.Redirect("/login?error=1&returnUrl={encoded}&username={encodedUsername}")`，登录页显示"用户名或密码错误"（`showError = true`，由 query string `error=1` 触发），用户名输入框预填 query string 中的 `username` 值，密码框为空

#### Scenario: 记住我

- **WHEN** 用户勾选"记住我"并登录成功
- **THEN** `SignInAsync` 的 `AuthenticationProperties.IsPersistent = true`，Cookie 持久化（浏览器关闭后仍保持）；不勾选则会话级 Cookie
