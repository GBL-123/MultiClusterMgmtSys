# MultiClusterMgmtSys

基于 .NET 10 + Blazor 的 Kubernetes 多集群管理平台，统一管理多个集群的节点、ConfigMap 与端点信息，内置账号权限体系与操作审计。

界面为中文，使用 MudBlazor 组件库（Swiss Industrial Print 工业印刷风格设计系统，浅色主题），数据存储采用 SQLite（零依赖，开箱即用）。

## 功能特性

- **集群管理**：接入多个 K8s 集群（kubeconfig / Token 两种连接方式），分组侧栏、分页筛选排序、状态探测与版本识别；集群详情聚合节点、ConfigMap 概览与管理员维护的 VIP / 域名端点
- **节点管理**：跨集群节点列表与详情（资源、条件、标签、注解、污点、地址），支持按 IP 登记备注（如管理口 / 数据口）
- **ConfigMap 管理**：按集群浏览 ConfigMap，YAML 只读查看与在线编辑、新建、删除
- **审计日志**：登录 / 注册及所有增删改操作自动记录（操作人、类别、动作、目标），Admin 可查全量，普通用户仅见本人记录
- **账号与权限**：`Admin` / `Member` 两级角色；管理员可批量删除、批量改角色、重置密码；用户可在个人资料页修改密码
- **界面设计**：Swiss Industrial Print 工业印刷风格（浅色主题、无暗色模式），自托管 Space Grotesk / IBM Plex Mono 字体

## 技术栈

| 组件 | 说明 |
|---|---|
| .NET 10 / ASP.NET Core | Blazor **Interactive Server** 渲染模式 |
| MudBlazor 9 | UI 组件库（含 `Extensions.MudBlazor.StaticInput`） |
| EF Core 10 + SQLite | `EnsureCreated()` 建库，无迁移文件 |
| ASP.NET Identity | Cookie 认证（8 小时滑动过期），角色键 `int` |
| KubernetesClient 19 | 官方 K8s API 客户端 |
| Serilog | 控制台 + 按天滚动文件日志（`logs/app-.log`，保留 30 天） |

## 本地开发

前置要求：.NET 10 SDK

```pwsh
dotnet build MultiClusterMgmtSys.slnx
dotnet test MultiClusterMgmtSys.Tests           # 66 个单元测试（xUnit + Moq + bUnit）
dotnet run --project MultiClusterMgmtSys          # http://localhost:5021
dotnet run --project MultiClusterMgmtSys --launch-profile https   # https://localhost:7081
```

首次启动自动完成：

1. 在 `MultiClusterMgmtSys/db/` 下创建 SQLite 数据库（模型变更时需手动删除该目录重新生成，项目不使用 EF 迁移）
2. 创建 `Admin` / `Member` 角色，并种子内置管理员

**默认账号：`admin / Changeme_123`（首次登录后请立即修改）**

## Docker Compose 部署

生产编排为 **应用 + nginx**：nginx 对外暴露 80/443 终止 TLS 并反代应用（含 Blazor Server 的 WebSocket 升级），应用 8080 端口仅在 compose 内部网络可访问。

```bash
# 首次部署前执行一次：授权数据目录给容器内非 root 用户(UID 1654)
mkdir -p db logs nginx/certs && sudo chown 1654:1654 db logs

# 构建并启动
docker compose -f docker-compose.prod.yml up -d --build
```

- TLS 证书：`nginx/certs/fullchain.pem` + `privkey.pem`（gitignored；无正式证书时可用 compose 文件注释中的命令生成自签名测试证书）
- 数据持久化：SQLite 位于部署目录 `db/MultiClusterMgmtSys.db`，容器升级重建不丢数据
- 日志：按天滚动写入 `logs/app-日期.log`，保留 30 天，可直接 tail / grep 排查
- 备份：停服后直接复制 `db/MultiClusterMgmtSys.db`
- 注意：不要用裸 `docker compose up`，会合并 VS 调试用的 `docker-compose.override.yml`（Development 环境 + 用户密钥挂载，服务器上会失败）

## 配置说明

连接串与日志路径定义在 `MultiClusterMgmtSys/appsettings.json`，可按 ASP.NET Core 配置优先级用环境变量覆盖（Docker 部署即采用此方式）：

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=db/MultiClusterMgmtSys.db"
},
"Logging": {
  "File": { "Path": "logs/app-.log" }
}
```

```bash
# 环境变量覆盖示例（双下划线分隔层级）
ConnectionStrings__DefaultConnection="Data Source=/data/mcms.db"
Logging__File__Path="/data/logs/app-.log"
```

## 目录结构

```
├── MultiClusterMgmtSys/
│   ├── Program.cs                 # 入口：DI 注册、Identity、EnsureCreated 与管理员种子
│   ├── appsettings.json           # 连接串、Serilog 日志路径等配置
│   ├── Components/                # Razor 组件（Pages/Shared/Layout + 按功能分目录）
│   │   ├── Clusters/  Nodes/  Configmaps/     # 各功能页
│   │   ├── AuditLogs/  Account/  Profile/  Auth/
│   │   └── Common/                # 共享组件与服务（ThemeManager、ExceptionPresenter 等）
│   ├── Services/                  # 业务服务（账号 / 集群 / 分组 / 节点 / ConfigMap / 审计）
│   ├── ViewModels/                # 页面绑定模型与映射扩展方法
│   ├── Requests/  Models/         # 请求 / 查询对象
│   ├── Common/  Data/             # 跨层枚举、异常体系；实体与仓库
│   └── wwwroot/                   # 静态资源与自托管字体
├── MultiClusterMgmtSys.Tests/     # xUnit + Moq + bUnit 单元测试（镜像主项目目录）
├── nginx/                         # nginx 配置与 TLS 证书（gitignored）
├── docker-compose.prod.yml        # 生产部署编排（应用 + nginx）
└── MultiClusterMgmtSys.slnx       # 解决方案（XML 格式）
```

## 安全提示

- 上线后第一时间修改默认管理员密码
- 集群凭据（kubeconfig / Token）存储于 SQLite 中，请确保部署机与备份文件的访问控制
- 生产环境启用 HTTPS（由内置 nginx 终止 TLS），并保持数据库目录权限最小化
