## 1. 后端：`/api/login` 端点

- [x] 1.1 `Program.cs` 中 `/api/login` 端点校验失败时的重定向 URL 增加 `username` 参数：`Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}&username={Uri.EscapeDataString(username)}")`

## 2. 前端：`Login.razor`

- [x] 2.1 `OnInitializedAsync` 中解析 query string 的 `username` 参数，赋值给 `username` 字段（预填用户名输入框）

## 3. 验证

- [x] 3.1 `dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 通过
- [x] 3.2 运行应用，输入错误密码登录，验证用户名保留、密码清空、错误提示显示
- [x] 3.3 输入正确密码登录，验证正常登录不受影响
