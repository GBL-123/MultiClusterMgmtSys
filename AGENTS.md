# AGENTS.md

本文件面向在本仓库工作的 OpenCode 会话，仅记录容易踩坑、需读多个文件才能推断的仓库专属信息。通用 .NET / Blazor 知识不再赘述。

## 技术栈

- .NET 10（`net10.0`）Blazor Server 应用。需本机已装 .NET 10 SDK（已验证 `10.0.301`）。
- 解决方案为 `.slnx`（新 XML 格式，非 `.sln`）。
- 单一应用项目 `MultiClusterMgmtSys/MultiClusterMgmtSys.csproj`，**无测试项目**。
- 关键依赖：MudBlazor 9.6（UI）、EF Core Sqlite 10.0.9 + EF Core Design 10.0.9（数据，Design 为设计期工具，`PrivateAssets=all`）、KubernetesClient 19.0.2（连接真实 k8s 集群）。

## 常用命令

- 构建（建议直接构建 csproj，见下方“坑”）：`dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj`
- 运行：`dotnet run --project MultiClusterMgmtSys/MultiClusterMgmtSys.csproj`
- Docker 开发：`docker compose up --build`（会挂载 user secrets 与 HTTPS 开发证书，需本机已配置 dev 证书）
- 重置数据库：删除 `MultiClusterMgmtSys/clusters.db*`（含 `-shm`、`-wal`）后重新运行。
- **无测试、无 lint、无 CI**。验证改动的方式：`dotnet build` 通过 + 运行应用并操作对应页面。

## 坑（务必注意）

- **不要用 `dotnet build MultiClusterMgmtSys.slnx` 做常规构建。** slnx 把 `docker-compose.dcproj` 也纳入了解决方案，构建解决方案会触发 Docker Compose 构建，慢且可能失败。日常构建/运行请直接对 csproj 操作。
- **开发阶段有意不使用 EF 迁移。** `csproj` 已装 `Microsoft.EntityFrameworkCore.Design` 包（设计期工具可用），但 `Program.cs` 启动时仍调用 `db.Database.EnsureCreated()` 依据 `AppDbContext.OnModelCreating` 建库，未创建任何 `Migrations/`。`EnsureCreated` 仅在库不存在时建表，**改了模型不会自动更新已存在的库**。模型变更后让 schema 生效的方式：删除 `clusters.db*`（含 `-shm`、`-wal`）后重跑——这是当前有意采用的方式，不需要建迁移。若日后要切到迁移工作流，需另建首个迁移并把启动逻辑从 `EnsureCreated()` 改为 `Migrate()`。
- `clusters.db`、`clusters.db-shm`、`clusters.db-wal` 是运行时数据，已被 `.gitignore`（`*.db`）忽略，**勿提交**。连接串 `Data Source=clusters.db` 为相对路径，库文件位于运行时工作目录。
- 生产管线启用了 HTTPS 重定向（`UseHttpsRedirection`）；开发环境依赖 user secrets 提供证书（`UserSecretsId` 已在 csproj 中配置）。

## 编码规范

- **Razor 组件（`.razor`）服务注入统一使用 `[Inject]` 特性写在 `@code` 块内**，不要在文件开头使用 `@inject` 指令。即：注入属性以 `[Inject] private <Type> <Name> { get; set; } = default!;` 形式声明在 `@code` 顶部，与其它组件状态字段集中管理。文件开头仅保留 `@page`、`@layout`、`@using` 等非注入指令。
- 例外：纯指令性组件（无 `@code` 块、仅做静态重定向等极简场景）也按此规范，把 `[Inject]` 放入新建的 `@code` 块中。

## 页面与对话框目录约定

- **页面按功能分子目录**：`Pages/Clusters/Clusters.razor`、`Pages/Nodes/Nodes.razor`，不是 `Pages/Clusters.razor`。新增功能 → 新建子目录（如 `Pages/ConfigMaps/ConfigMaps.razor`）。
- **对话框与页面 colocate**：对话框放在对应页面的子目录内（`Pages/Clusters/AddClusterDialog.razor`），**不存在 `Pages/Dialogs/` 目录**。
- **`_Imports.razor` 需为每个新子目录添加 `@using`**：现有 `@using MultiClusterMgmtSys.Components.Pages.Clusters`、`...Pages.Nodes`。新建 `Pages/Foo/` 后必须加 `@using MultiClusterMgmtSys.Components.Pages.Foo`，否则 `DialogService.ShowAsync<FooDialog>()` 无法解析组件。
- **导航菜单在 `Drawer.razor`**（非 `MainLayout.razor`）。`MainLayout.razor` 委托给 `<AppBar>` + `<Drawer>` 组件，自身只做外壳编排。新增侧边栏入口 → 改 `Drawer.razor` 的 `MudNavMenu`。

## 架构 / 接线

- 入口 `Program.cs`：注册 Razor Components（交互式 Server）、`MudServices`、`AppDbContext`（`AddDbContext`）、各 Repository/Service（均 `AddScoped`）、`ThemeManager`（Scoped）、Cookie 认证（`AddAuthentication` + `AddCookie` + `AddAuthorization` + `AddCascadingAuthenticationState`）。启动时在 scope 内调用 `EnsureCreated()` 建库 + `AccountService.SeedAccountsAsync()` 种子账号。
- 分层（单向依赖：Razor → Service 返回 ViewModel → Repository 返回实体 → DbContext）：
  - `Components/`：Blazor UI。`Layout/`（`MainLayout` 委托 `AppBar`+`Drawer`、`EmptyLayout` 供登录页）、`Pages/`（按功能分子目录）、`Theme/ThemeManager.cs`（Scoped，MudTheme 定义 + localStorage 持久化）。
  - `Daos/`：`AppDbContext`（DbSets：`ClusterGroups`、`Clusters`、`Accounts`）+ `ClusterRepository`、`GroupRepository`、`AccountRepository`（返回实体）。
  - `Services/`：`ClusterService`（集群 CRUD + 探测）、`ClusterNodeService`（节点查询）、`GroupService`、`AccountService`（均返回 ViewModel）。
  - `Models/`：`ClusterGroup`、`ClusterInfo`、`ClusterStatus`、`ConnectionType`、`Account`、`AppRole`。
  - `ViewModels/`：纯 POCO 契约 + `Mappings/` 扩展方法（`entity.ToViewModel()`）。
- 数据模型：`ClusterGroup` 1—N `ClusterInfo`（外键 `GroupId`，`OnDelete SetNull`）。`ClusterInfo.KubeConfig`/`Token` 以明文存于 `TEXT` 列。`Account` 密码用 `PasswordHasher<string>`（PBKDF2）哈希。

## k8s 服务错误处理模式

- **`ClusterService`**：`ProbeAsync` 内部 `try/catch`，集群不可达时记日志并把状态置为 `Offline`（**不致命**）。`GetClusterDetailAsync` 内部 catch k8s 异常并设 `IsReachable = false`。注入 `ILogger`。
- **`ClusterNodeService`**：**不做 try/catch**，异常直接上抛由页面处理。**无 `ILogger`**。主构造函数仅注入 `ClusterRepository`。
- **可达性判断链路**：页面先调 `ClusterService.GetClusterDetailAsync(id)` 取 `IsReachable`，仅在可达时才调资源服务（如 `ClusterNodeService.GetClusterNodesAsync`），页面层 `try/catch` + `Snackbar`。新增 k8s 资源服务（如 `ConfigMapService`）应跟随 `ClusterNodeService` 模式。
- **`BuildConfig` 重复**：`ClusterService.BuildConfig` 与 `ClusterNodeService.BuildConfig` 是相同的私有方法（KubeConfig 走 `BuildConfigFromConfigFile(stream)`，Token 走手动 `KubernetesClientConfiguration`）。已知技术债，后续可抽取 `KubernetesClientFactory`。

## 认证 quirks

- Blazor Server 交互式渲染时 `HttpContext` 为 null，`SignInAsync`/`SignOutAsync` **必须在最小 API 端点调用**（`/api/login` POST、`/api/logout` GET），不能在 Blazor `@onclick` 里调。
- 登录页 `Login.razor` 用**原生 HTML `<form method="post" action="/api/login">`** 提交（非 `HttpClient.PostAsJsonAsync`），端点返回 `Results.Redirect`/`LocalRedirect` 整页跳转。
- 登录页 `@layout EmptyLayout`（非 `@layout null`）——`EmptyLayout` 含 `MudThemeProvider` + providers，`@layout null` 会丢失 MudBlazor 组件服务。
- 种子账号：admin / guest，默认密码 `Changeme_123`。
- `AddCascadingAuthenticationState()` 必须显式注册，不被 `AddInteractiveServerComponents()` 自动调用。

## OpenSpec 变更管理

- 仓库使用 OpenSpec 管理功能变更。主 spec 在 `openspec/specs/`（`authentication`、`cluster-management`、`node-management`），已实现功能的权威规范。
- 活跃变更在 `openspec/changes/`，归档变更在 `openspec/changes/archive/YYYY-MM-DD-<name>/`。
- 常用命令：`openspec list`、`openspec status --change "<name>"`、`openspec validate --changes "<name>"`、`openspec new change "<name>"`。
- 无 store 注册，命令作用于最近的本地 `openspec/` 目录。
