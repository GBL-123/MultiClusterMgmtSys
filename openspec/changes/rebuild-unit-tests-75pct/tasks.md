## 1. 基础设施(Infra)

- [x] 1.1 补齐测试 csproj:`FrameworkReference Microsoft.AspNetCore.App` + 主项目 `ProjectReference`(验证 `dotnet build MultiClusterMgmtSys.slnx` 0 错误)
- [x] 1.2 建 `TestInfrastructure/SqliteDbFactory`(每测试独立 SQLite 内存库,生产 provider)
- [x] 1.3 建 `TestInfrastructure/SeedUser`(Identity 用户/角色种子,密码满足策略:长度 8 + 数字)
- [x] 1.4 建 `TestInfrastructure/TestData`(实体/ViewModel 构造器:集群/分组/节点备注/端点/审计/工作负载)
- [x] 1.5 建 `TestInfrastructure/K8sMocks`(Moq `IKubernetes` 的 `*WithHttpMessagesAsync` + 惰性 `ThrowingFactory`,异常以 `KubernetesException(new V1Status { Code = ... })` 构造)
- [x] 1.6 建 `TestInfrastructure/BunitHost`(`BunitContext` 封装:MudServices + `TimeProvider.System` + `JSRuntimeMode.Loose` + `AddAuthorization`,统一 `await using` 释放模式)

## 2. M1 逻辑核心测试

- [x] 2.1 Common 纯函数:`K8sExceptionMapper`(404/409/403/401/400/超时/5xx)、`ExceptionPresenter`(业务/非业务分支、Conflict→Warning)、`ThemeManager` token、`PagedResult`、`VersionFilterSentinel`
- [x] 2.2 `Data/Repositories`:ClusterRepository(GroupFile sentinel 0/正数/空、版本 sentinel、日期过滤、排序分页)、GroupRepository、AuditLogRepository、AppSettingRepository(SQLite 真实库)
- [x] 2.3 `Services/ClusterService`:分页查询契约、CRUD、探活降级分支、K8s 状态翻译、审计副作用
- [x] 2.4 `Services/ClusterNodeService`:节点列表/详情合并 `NodeIpRemark`、IP 备注更新校验、K8s 翻译、审计
- [x] 2.5 `Services/ConfigMapService`:列表/详情/创建/键值操作/YAML 读写、K8s 翻译、审计
- [x] 2.6 `Services/WorkloadService`:四类 `WorkloadKind` 列表/详情/创建/scale 校验/YAML 读写、rollout 状态计算、K8s 翻译、审计
- [x] 2.7 `Services/GroupService` + `Services/AuditService` + `Services/AuthService` + `Services/ClusterSyncSettingService`(设置读写 + 审计)
- [x] 2.8 `Services/ClusterSyncBackgroundService.RunOnceAsync`(scope 调用集群状态刷新)
- [x] 2.9 `Services/AccountService`:登录/注册/改密/重置/角色批量/分页查询(Identity + 真实 SQLite)
- [x] 2.10 `ViewModels/Mappings`:6 个 Mapping 扩展逐字段断言
- [x] 2.11 `Services/Identity/ChineseIdentityErrorDescriber`:中文文案翻译断言
- [x] 2.12 生产代码加 4 处 `[ExcludeFromCodeCoverage]`(Program / IdentityComponentsEndpointRouteBuilderExtensions / App.razor / Routes.razor),build 0 错误
- [x] 2.13 M1 检查点:全量测试绿 + 跑 `--coverage` 记录真实基线(校准 M2/M3 轨迹)

> M1 基线记录:267 tests 全绿,排除口径下行覆盖率 **40.08%**(2026-09-08)。M2 目标混合 ≥55% 保持不变。

## 3. M2 共享 razor 组件测试

- [x] 3.1 Clusters:`ClusterTable`、`ClusterOverviewCard`、`ClusterFilterBar`(DateRangeChanged 回写)、`GroupSidebar`、`ClusterDetailToolbar`
- [x] 3.2 Nodes:`NodeListTable`、`NodeOverviewCard`、`NodeResourcesCard`、`NodeConditionsCard`、`NodeListFilterBar`
- [x] 3.3 Workloads:`WorkloadListTable`、`WorkloadListView`、`WorkloadStatusCard`(rollout 状态分支)、`WorkloadListFilterBar`
- [x] 3.4 Configmaps:`ConfigMapListTable`、`ConfigMapDataViewCard`、`ConfigMapListFilterBar`、`ConfigMapYamlViewCard`
- [x] 3.5 Account/AuditLogs:`AccountTable`、`AccountFilterBar`、`AuditLogFilterBar`
- [x] 3.6 Common:`ConfirmDialog`、`ExceptionPresenter` 组件接线、`ClusterSelectSidebar`
- [x] 3.7 M2 检查点:`dotnet test` 全绿 + 覆盖率复查(目标:混合 ≥55%)

> M2 记录:320 tests 全绿,混合覆盖率 **46.65%**(未达 55% 预估——razor 分母比预估更重,靠 M3 页面壳补齐)。

## 4. M3 页面与分支收尾

- [x] 4.1 列表页壳:Clusters/Nodes/Deployments/StatefulSets/DaemonSets/ReplicaSets/ConfigMaps/Accounts/AuditLogs 页面渲染 + 加载/空态分支
- [x] 4.2 详情页 tab 面板:ClusterDetail/NodeDetail/各 Workload Detail(每个 tab 至少触发一次)+ YAML view/edit 卡片
- [x] 4.3 对话框交互:Create/Edit/Scale/Endpoints/IpNotes/改密/重置密码(经公开事件驱动,断言服务调用与状态)
- [x] 4.4 门控两态补全:全部 Admin 门控块 Admin/Member 渲染变体;badge 三态全扫
- [x] 4.5 Auth 页面:Login/Register/Profile(接线与状态分支)
- [ ] 4.6 M3 检查点:`--coverage` ≥75%(排除口径),不足则按 HTML 报告下钻补测

## 5. 仓库卫生与文档

- [x] 5.1 `.gitignore` 补 `TestResults/`、`coveragereport/`
- [x] 5.2 AGENTS.md 测试章节刷新(项目名 `.Tests`、MTP 命令、覆盖率命令与口径、TestInfrastructure 用法、当前测试数基线)
- [x] 5.3 验证 `dotnet test MultiClusterMgmtSys.Tests` 与覆盖率命令文档可复现;`dotnet build MultiClusterMgmtSys.slnx` 0 错误
