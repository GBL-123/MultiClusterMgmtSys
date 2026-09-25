# 全局集群看板

## Why

系统叫「多集群管理」,但所有业务页面都是单集群视角:每个页面都绑定一个 `ClusterId`,配左侧集群选择栏,一次只能看一个集群。没有任何一处能一眼回答「我的集群舰队现在整体健康吗」——而这恰恰是集群出问题时最想先看到的东西。

同时,后台定时同步每 5 分钟已经在探测每个集群的 `Status`/`Version`/`NodeCount`/`LastCheckedAt` 并落库,但这些数据至今只被单集群视图消费,从未被聚合。补齐这个缺口不需要新增任何 Kubernetes API 调用。

## What Changes

- **看板页面**:新增 `/dashboard`(全局、跨集群、无 `ClusterId` 参数、无分页表格、无管理按钮);展示集群状态计数、需要关注清单、分组健康、版本分布、最近操作。
- **快照读取语义**:看板数据全部读自 SQLite,打开即渲染,**不发起任何 K8s 调用**;集群离线时看板照样可用——而那正是最需要它的时刻。
- **数据新鲜度**:以生效同步间隔为基准判定过期(超过 2 倍间隔即告警),顶部常显「数据截至 …」;同步停滞时显式提示。这顺带补上「后台同步是否还活着」这个目前系统里无人知晓的洞。
- **刷新入口**:看板提供「刷新全部」按钮,复用既有 `RefreshAllClustersStatusAsync` 及其进度回调,不新增探测路径。
- **节点健康快照**:新增 `ClusterHealthSnapshot` 表,后台同步每次**成功**探测一个集群即追加一行(节点总数/就绪/未就绪);只追加,不覆盖、不修改历史。
- **探测内容扩展**:节点就绪统计从既有 `ListNodeAsync` 的返回结果解析(`V1Node.Status.Conditions`),**不新增 K8s API 调用**——这次调用早已付出,只是数据被丢掉了。
- **失败与取消语义不变**:探测失败仍按既有契约置 `Offline`、清空 `Version`/`NodeCount`;失败与停机取消均不写快照。因此「最后一次成功探测的节点数」自然保留在快照表中,看板据此诚实展示舰队规模,而不是让总数在集群离线时凭空缩水。
- **落地页变更**:登录后默认落地页由 `/clusters` 改为 `/dashboard`;侧边导航新增看板入口。
- **设计系统口径修正**:内容卡片/面板沿用 MudBlazor 默认 elevation 阴影(默认 1;列表页空态卡 2;卡片内表格 0),无阴影例外(登录/注册面板、兜底页、AppBar/Drawer、tooltip、重连弹窗)保持不变——看板实现初版误按「全站无阴影」执行而与其余页面不一致,`ui-theme` 契约随之修正。
- **BREAKING(数据)**:新增表需删库重建(项目无 EF migrations,`EnsureCreated` 不补建表),已登记的集群与凭据需重新录入。仅影响开发态与自建部署。

## Capabilities

### New Capabilities
- `cluster-dashboard`: 全局舰队看板契约——路由与登录后落地页、快照读取语义(零 K8s 调用)、数据新鲜度判定与过期告警、节点健康快照的读取口径、按角色的最近操作可见范围、空态与部分数据缺失时的表现。

### Modified Capabilities
- `cluster-scheduled-sync`: 探测在既有 K8s 调用结果上额外采集节点就绪统计;探测成功时追加一条节点健康快照,探测失败与停机取消时不写。既有降级语义(`Offline`/清空 `Version`/清空 `NodeCount`/更新 `LastCheckedAt`)与状态翻转审计规则保持不变。
- `ui-theme`: 修正层级表达口径为「内容卡片保持 MudBlazor 默认 elevation 阴影 + 发丝线」,并固化无阴影例外清单;调色板、字体、徽章、表格等其余约定不变。

## Impact

- **代码**:
  - 新增:`Domain/Entities/ClusterHealthSnapshot.cs`、`Application/Abstractions/IClusterHealthRepository.cs`、`Infrastructure/Persistence/ClusterHealthRepository.cs`、`Application/ViewModels/DashboardViewModel.cs`、`Application/Services/DashboardService.cs`、`Web/Components/Dashboard/**`(页面与区块组件)。
  - 修改:`Infrastructure/Persistence/ApplicationDbContext.cs`(DbSet + 索引 + 级联)、`Application/Services/ClusterService.cs`(`ProbeAsync` 采集并追加快照,构造函数新增依赖)、`Web/Program.cs`(DI 注册 + 默认重定向)、`Web/Components/Pages/Home.razor`、`Web/Components/Layout/Drawer.razor`、`Web/wwwroot/css/app.css`(看板布局规则)。
- **测试**:波及 `ClusterServiceTests`、`ClusterRefreshConcurrencyTests`(探测行为与构造函数签名变更)及测试脚手架;新增 `DashboardService` 服务测试、快照仓库测试与看板 bUnit 接线测试。`dotnet build` 0 错误、全量测试通过、`./coverage.ps1` 四程序集合并行覆盖率门禁 75% 不降。
- **文档**:AGENTS.md 补记看板页面、快照表与默认落地页;`cluster-dashboard` 新 spec 与 `cluster-scheduled-sync` delta 随归档合并进主 spec。
- **范围外**:不做任何 K8s 实时巡检(看板永不连集群);不做趋势/曲线图(快照表从本次起积累数据,趋势留给后续 change);不做异常 Pod 数、警告事件数等需要额外 API 调用的指标;不引入 metrics-server 依赖;不做快照保留策略(数据量无感,后续再加);不改 `fallback-pages` 的「返回集群列表」入口(仍指向 `/clusters`)。
