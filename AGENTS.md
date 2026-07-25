# AGENTS.md

本文件为 OpenCode / 代码智能体在该仓库工作时的指引。`默认使用中文` 沟通与撰写面向用户的文案。

## 项目概述

多集群 Kubernetes 管理系统。单项目 Blazor Server 应用（.NET 10），通过 `KubernetesClient` 连接并管理多个 K8s 集群。UI 使用 MudBlazor 9.x。中文界面。

- 解决方案：`MultiClusterMgmtSys.slnx`（注意是 `.slnx` 新格式）
- 唯一项目：`MultiClusterMgmtSys/MultiClusterMgmtSys.csproj`
- 入口：`MultiClusterMgmtSys/Program.cs`

## 常用命令

仓库未配置 lint / format / test 验证步骤（无测试项目、无 analyzer 配置、无 CI 工作流）。验证改动主要靠构建：

```bash
# 还原 + 构建
dotnet build MultiClusterMgmtSys.slnx

# 发布
dotnet publish MultiClusterMgmtSys/MultiClusterMgmtSys.csproj -c Release -o ./publish

# 本地运行（开发）
dotnet run --project MultiClusterMgmtSys/MultiClusterMgmtSys.csproj
#   http  : http://localhost:5021
#   https : https://localhost:7081 ; http://localhost:5021
```

Docker：见 `MultiClusterMgmtSys/Dockerfile`（多阶段，基镜像 `dotnet/aspnet:10.0` 与 `sdk:10.0`），`docker-compose.yml` 通过 `docker-compose.dcproj` 编排（VS 容器工具生成）。

## 架构关键点

- **Blazor Server 交互式渲染**：`Program.cs` 使用 `AddRazorComponents().AddInteractiveServerComponents()` + `MapRazorComponents<App>()`。所有页面在 `Components/Pages/` 下，按领域分子目录组织：`Auth/`、`Accounts/`、`Clusters/`、`Nodes/`、`ConfigMaps/`、`Profile/`。
- **分层约定**（不是严格 MVVM，注意命名陷阱）：
  - `Models/`：EF Core 实体（`ClusterInfo`、`ClusterGroup`、`ApplicationUser`）
  - `Daos/`：`AppDbContext` + Repository（`ClusterRepository`、`GroupRepository`）—— 直接持有 `DbContext`，**不是** DI 中注入的仓储接口，按具体类注册为 `Scoped`。
  - `Services/`：业务逻辑（`ClusterService`、`ClusterNodeService`、`ConfigMapService`、`GroupService`、`AccountService`、`AuthService`）。`Services/Identity/` 为 Identity 适配（自定义 `AuthenticationStateProvider`、`IdentityComponentsEndpointRouteBuilderExtensions`、`ChineseIdentityErrorDescriber`）。
  - `Requests/`：登录注册 DTO；`ViewModels/`：页面模型；`ViewModels/Mappings/`：实体 ↔ ViewModel 的映射扩展方法。
  - `Constants/`：枚举（`ClusterStatus`、`ConnectionType`）。
  - `Components/`：Razor 组件；`Components/Layout/`、`Components/Redirection/`、`Components/Theme/` 为基础设施。
- **K8s 连接**：`ClusterService.BuildConfig` 支持两种连接方式（`ConnectionType.KubeConfig` 走 `BuildConfigFromConfigFile`，`Token` 走 ApiServer + AccessToken + `SkipTlsVerify`）。`KubeConfig` 与 `Token` 以 `TEXT` 列存储于 SQLite。`SkipTlsVerify` 默认 `true`。
- **状态探测**：新增 / 修改集群或调用 `RefreshClusterStatusAsync` 时同步访问 K8s API 探活并回写 `Status`、`Version`、`NodeCount`、`LastCheckedAt`；不可达时置 `Offline`。代理须在线表现出这一副作用，避免在离线环境误判代码损坏。

## 数据库与 Identity 要点

- **SQLite，无迁移**：`AppDbContext` 使用 EF Core + `Microsoft.EntityFrameworkCore.Sqlite`。`Program.cs` 启动时调用 `db.Database.EnsureCreated()`，**不使用 EF 迁移**。改实体模型后无需 `dotnet ef migrations add`，但需要删除 `MultiClusterMgmtSys/MultiClusterMgmtSys.db` 让其重建（该 db 文件不入库）。仅开发环境启用 `UseMigrationsEndPoint`。
- **Identity 配置**（见 `Program.cs`）：
  - `IdentityDbContext<ApplicationUser, IdentityRole<int>, int>` —— **主键为 `int`**，不是默认 Guid。
  - 密码策略：长度 ≥ 8、必须含数字；不要求大小写 / 非字母数字。
  - `Stores.SchemaVersion = IdentitySchemaVersions.Version3`。
  - 角色固定为 `Admin` / `Member`（见 `AccountService`）。**至少保留一个 Admin**，删除最后一个 Admin 或删除当前登录账号会被业务层拒绝并返回中文错误。
  - 启动自动创建 `admin` / `Changeme_123`（`AccountService.CreateAdminAsync`，`Program.cs` 中无条件执行）。新环境首次运行即存在该账号——撰写测试 / 文档时勿假定空库。
  - 错误描述已汉化（`ChineseIdentityErrorDescriber`），用户面向文案统一中文。
- Cookie 名 `MultiClusterMgmtSys.Auth`，8 小时滑动过期，登录路径 `/login`，默认 `returnUrl` 为 `/clusters`。

## 风格与约定

- C# 12+ primary constructor 风格广泛使用（Service / Repository / DbContext 均为 primary constructor）。
- 隐式 usings 开启；`_Imports.razor` 已 `@using` 主要命名空间，新组件无需重复 import `MudBlazor` / `MultiClusterMgmtSys.*`（除 `Models`、`Daos` 需要时显式引入）。
- Razor 组件按领域分组到子目录，避免在 `Components/Pages` 根堆放。
- 用户可见文案（按钮、提示、错误）使用中文；代码标识符与注释保持英文为主，除非周边文件已用中文（如 Dockerfile 顶部注释）。

## 常见陷阱

- 改动 Identity 模型 / 新增 DbSet 后：必须配合 `EnsureCreated` 的建表时机，不要去运行不存在的 `dotnet-ef migrations`；本地开发时删除 `*.db` 重建。
- `Daos` 命名误导：它包含 `AppDbContext` 本身，不只是 DAO。新增实体需在 `AppDbContext` 注册 `DbSet` 并在 `OnModelCreating` 配置映射。
- `UserSecretsId` 已配置（`1569137f-...`），但当前无 secrets 实际使用；连接串写死在 `appsettings.json`（`Data Source=MultiClusterMgmtSys.db`）。
- 仓库 `bin/`、`obj/`、`.vs/`、`*.db`、`*.user` 均被 `.gitignore` 忽略，不要提交构建产物或本地 SQLite 文件。

## OpenSpec

仓库使用 OpenSpec 做规范驱动开发（`openspec/` 目录，配置见 `openspec/config.yaml`，`schema: spec-driven`）。规划新变更时优先走 OpenSpec 流程（propose / apply / archive 相关 skill 已在本工作区可用），而非直接开工。