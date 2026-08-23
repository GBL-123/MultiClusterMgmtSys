# MultiClusterMgmtSys

基于 .NET 10 + Blazor 的 Kubernetes 多集群管理平台，统一管理多个集群的节点、ConfigMap 与端点信息，内置账号权限体系与操作审计。

界面为中文，使用 MudBlazor 组件库，数据存储采用 SQLite（零依赖，开箱即用）。

## 功能特性

- **集群管理**：接入多个 K8s 集群（kubeconfig / Token 两种连接方式），分组侧栏、分页筛选排序、状态探测与版本识别；集群详情聚合节点、ConfigMap 概览与管理员维护的 VIP / 域名端点
- **节点管理**：跨集群节点列表与详情（资源、条件、标签、注解、污点、地址），支持按 IP 登记备注（如管理口 / 数据口）
- **ConfigMap 管理**：按集群浏览 ConfigMap，YAML 只读查看与在线编辑、新建、删除
- **审计日志**：登录 / 注册及所有增删改操作自动记录（操作人、类别、动作、目标），Admin 可查全量，普通用户仅见本人记录
- **账号与权限**：`Admin` / `Member` 两级角色；管理员可批量删除、批量改角色、重置密码；用户可在个人资料页修改密码
- **主题**：明暗模式切换，偏好本地持久化

## 技术栈

| 组件 | 说明 |
|---|---|
| .NET 10 / ASP.NET Core | Blazor **Interactive Server** 渲染模式 |
| MudBlazor 9 | UI 组件库（含 `Extensions.MudBlazor.StaticInput`） |
| EF Core 10 + SQLite | `EnsureCreated()` 建库，无迁移文件 |
| ASP.NET Identity | Cookie 认证（8 小时滑动过期），角色键 `int` |
| KubernetesClient 19 | 官方 K8s API 客户端 |

## 本地开发

前置要求：.NET 10 SDK

```pwsh
dotnet build MultiClusterMgmtSys.slnx
dotnet run --project MultiClusterMgmtSys          # http://localhost:5021
dotnet run --project MultiClusterMgmtSys --launch-profile https   # https://localhost:7081
```

首次启动自动完成：

1. 在 `MultiClusterMgmtSys/db/` 下创建 SQLite 数据库（模型变更时需手动删除该目录重新生成，项目不使用 EF 迁移）
2. 创建 `Admin` / `Member` 角色，并种子内置管理员

**默认账号：`admin / Changeme_123`（首次登录后请立即修改）**

## Docker Compose 部署

```bash
# 首次部署前执行一次：授权数据目录给容器内非 root 用户(UID 1654)
mkdir -p db && sudo chown 1654:1654 db

# 构建并启动
docker compose -f docker-compose.prod.yml up -d --build
```

- 服务端口：宿主机 `8080` → 容器 `8080`（建议由 Nginx / Caddy 反向代理终止 TLS 后转发）
- 数据持久化：SQLite 位于部署目录 `db/MultiClusterMgmtSys.db`，容器升级重建不丢数据
- 备份：停服后直接复制 `db/MultiClusterMgmtSys.db`
- 注意：不要用裸 `docker compose up`，会合并 VS 调试用的 `docker-compose.override.yml`

## 配置说明

连接串定义在 `MultiClusterMgmtSys/appsettings.json`，可按 ASP.NET Core 配置优先级用环境变量覆盖（Docker 部署即采用此方式）：

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=db/MultiClusterMgmtSys.db"
}
```

```bash
# 环境变量覆盖示例（双下划线分隔层级）
ConnectionStrings__DefaultConnection="Data Source=/data/mcms.db"
```

## 目录结构

```
├── MultiClusterMgmtSys/
│   ├── Program.cs                 # 入口：DI 注册、Identity、EnsureCreated 与管理员种子
│   ├── appsettings.json           # 连接串等配置
│   ├── Components/
│   │   ├── Clusters/              # 集群管理（Pages/Shared/Services/Requests/ViewModels）
│   │   ├── Nodes/                 # 节点管理与节点 IP 备注
│   │   ├── Configmaps/            # ConfigMap 管理
│   │   ├── AuditLogs/             # 审计日志
│   │   ├── Account/               # 账号管理（管理员）
│   │   ├── Profile/               # 个人资料
│   │   ├── Auth/                  # 登录 / 注册
│   │   └── Common/                # 共享组件与服务（侧栏、确认框、主题等）
│   ├── Common/                    # 跨层枚举、查询对象、通用视图模型
│   └── Data/
│       ├── Entities/              # ClusterInfo / ClusterGroup / ClusterEndpoint /
│       │                          # NodeIpRemark / AuditLog 等
│       └── Repositories/          # 数据访问（服务层组合业务与 K8s 调用）
├── docker-compose.prod.yml        # 生产部署编排
└── MultiClusterMgmtSys.slnx       # 解决方案（XML 格式）
```

## 安全提示

- 上线后第一时间修改默认管理员密码
- 集群凭据（kubeconfig / Token）存储于 SQLite 中，请确保部署机与备份文件的访问控制
- 生产环境请启用 HTTPS（反向代理终止即可），并保持数据库目录权限最小化
