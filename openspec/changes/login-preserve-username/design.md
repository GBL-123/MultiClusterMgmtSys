## Context

登录页 `Login.razor` 使用原生 HTML `<form method="post" action="/api/login">` 提交。`/api/login` 端点校验失败时返回 `Results.Redirect("/login?error=1&returnUrl={encoded}")`，整页跳转回登录页。由于是整页重定向，表单中已输入的用户名和密码均丢失，用户需重新输入用户名。

`Login.razor` 的 `OnInitializedAsync` 已从 query string 解析 `returnUrl` 和 `error` 参数。扩展此模式，增加解析 `username` 参数即可预填用户名框。

## Goals / Non-Goals

**Goals:**

- 登录失败时保留用户已输入的用户名，仅清空密码。

**Non-Goals:**

- 不回传密码（密码框始终为空，安全惯例）。
- 不改为 SPA 式 AJAX 提交（保持原生 form POST + 整页跳转模式）。
- 不增加登录失败次数限制或账号锁定。

## Decisions

### D1: 通过 query string 传递用户名（非 TempData / Cookie）

**选择：** `/api/login` 端点失败重定向 URL 增加 `username` 参数：`/login?error=1&returnUrl={encoded}&username={encodedUsername}`。`Login.razor` 解析该参数预填用户名框。

**理由：** 与现有 `error` 和 `returnUrl` 参数传递方式一致，无需引入 TempData 或 Cookie 等额外机制。query string 经 URL 编码，安全传递用户名。

**替代方案（已拒绝）：** TempData/Cookie 传递——增加状态管理复杂度，且最小 API 端点用 TempData 需额外注册服务。

### D2: 用户名经 `Uri.EscapeDataString` 编码

**选择：** 重定向 URL 中 `username` 值用 `Uri.EscapeDataString(username)` 编码，`Login.razor` 中 `System.Web.HttpUtility.ParseQueryString` 自动解码。

**理由：** 用户名可能含特殊字符（虽然种子账号仅 admin/guest，但未来可能有其他用户名），URL 编码避免参数注入或解析错误。

## Risks / Trade-offs

- **[用户名出现在 URL 中]** → 用户名经 URL 编码后出现在 query string，浏览器历史记录可见。影响极小——这是登录页，用户名不是敏感信息（密码不回传）。与现有 `returnUrl` 参数同级别。
