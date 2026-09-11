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

> 本轮续补记录(2026-09-10):新增 Profile 页 / 改密对话框 / 账号编辑对话框 / 重置密码对话框 / Scale 对话框 / Confirm 对话框 / CreateWorkloadDialog / EditClusterDialog / 各页面壳 / ClusterDetail+NodeDetail tab 流 / Clusters 删除确认流 / ConfigMaps+Nodes+Services 页 / SvcService(新功能域)/ 账号重置密码页面流 / Services 域页面与 YAML 保存 / WorkloadService+SvcService 错误翻译分支 / Clusters 删除分组确认流 / ConfigMaps 删除流。当前 **435 tests 全绿,覆盖率 68.89%**(进行中:4.6 检查点,距 75% 差 6 个百分点;剩余:WorkloadListView 交互 ~150、Clusters 移动/批量流 ~140、Accounts 批量流 ~110、Services 页搜索 ~90、ClusterEndpointsDialog 65、CreateWorkloadDialog 52、WorkloadYamlEditView 保存失败 51、Nodes 筛选 47)。


> 注:`add-service-management` change(并行进行中)新增了 Services 功能域(SvcService + 11 个组件)与 `AuditCategory.Service` 枚举,这些代码的测试已并入本 change 的 435 个测试。

## 4. M3 页面与分支收尾

- [x] 4.1 列表页壳:Clusters/Nodes/Deployments/StatefulSets/DaemonSets/ReplicaSets/ConfigMaps/Accounts/AuditLogs 页面渲染 + 加载/空态分支
- [x] 4.2 详情页 tab 面板:ClusterDetail/NodeDetail/各 Workload Detail(每个 tab 至少触发一次)+ YAML view/edit 卡片
- [x] 4.3 对话框交互:Create/Edit/Scale/Endpoints/IpNotes/改密/重置密码(经公开事件驱动,断言服务调用与状态)
- [x] 4.4 门控两态补全:全部 Admin 门控块 Admin/Member 渲染变体;badge 三态全扫
- [x] 4.5 Auth 页面:Login/Register/Profile(接线与状态分支)
- [x] 4.6 M3 检查点:`--coverage` ≥75%(排除口径),不足则按 HTML 报告下钻补测

> **4.6 达成记录(2026-09-11):485 tests 全绿,行覆盖率 75.91% ≥ 75% 目标。** 最终轨迹:40.08%(M1)→ 46.65%(M2)→ 54.72% → … → 68.89% → 70.77% → 72.55% → 74.57% → **75.91%**。收尾批次:AccountService 排序/角色守卫分支、Svcs/ConfigMaps/Workloads 页查询-重置过滤流、Svcs 删除确认流、Workload 行导航、ClusterDetail 刷新/删除/tab 流、Clusters 行刷新/添加对话框/重命名分组、ConfigMap/Svc 详情页、CreateSvcDialog、YAML 编辑非法保存不落审计、NodeIpNotesDialog 提交审计、ClusterSyncBackgroundService 后台循环(含取消停止)。

> 覆盖率完整轨迹:40.08%(M1)→ 46.65%(M2)→ 54.72% → 58.53% → 59.41% → 60.60% → 63.18% → 64.72% → 65.78% → 66.40% → 67.42% → 67.81% → 68.45% → 68.89% → 70.77% → 72.55% → 74.57% → **75.91%(达成)**。期间新增覆盖:Svcs 功能域页面(并行 change)、各页面查询-重置过滤流、详情页 tab/删除/刷新流、YAML 保存流(成功+非法)、对话框提交流(改密/重置/编辑/创建/备注)、服务层错误翻译与排序/守卫分支、ClusterSyncBackgroundService 后台循环。

## 5. 仓库卫生与文档

- [x] 5.1 `.gitignore` 补 `TestResults/`、`coveragereport/`
- [x] 5.2 AGENTS.md 测试章节刷新(项目名 `.Tests`、MTP 命令、覆盖率命令与口径、TestInfrastructure 用法、当前测试数基线)
- [x] 5.3 验证 `dotnet test MultiClusterMgmtSys.Tests` 与覆盖率命令文档可复现;`dotnet build MultiClusterMgmtSys.slnx` 0 错误
