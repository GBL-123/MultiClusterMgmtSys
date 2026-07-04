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

## 架构 / 接线

- 入口 `Program.cs`：注册 Razor Components（交互式 Server）、`MudServices`、`AppDbContext`（`AddDbContext`）、`ClusterService`（`AddScoped`）。启动时在 scope 内调用 `EnsureCreated()` 建库。
- 分层：
  - `Components/`：Blazor（`App.razor`、`Routes.razor`、`Layout/`、`Pages/`，主页面 `Pages/Clusters.razor`）。
  - `Daos/AppDbContext.cs`：EF DbContext，DbSets 为 `ClusterGroups`、`Clusters`。
  - `Services/ClusterService.cs`：scoped，承载业务逻辑与 k8s 调用。
  - `Models/`：`ClusterGroup`、`ClusterInfo`、`ClusterStatus`。
- 数据模型：`ClusterGroup` 1—N `ClusterInfo`（外键 `GroupId`，`OnDelete SetNull`）。`ClusterInfo.KubeConfig` 以 **完整 kubeconfig YAML 文本**形式存于 `TEXT` 列。
- `ClusterService` 连接真实集群：用存储的 kubeconfig 文本构建 `KubernetesClientConfiguration.BuildConfigFromConfigFile(stream)`，再调 `Version.GetCodeAsync()` 与 `CoreV1.ListNodeAsync()`。集群不可达时会捕获异常、记录日志并把状态置为 `Offline`（**不致命**，不影响应用启动）。新增/刷新集群状态依赖该集群真实可连。
