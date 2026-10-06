# Tasks

## 1. 仓储与请求契约

- [x] 1.1 `IClusterHealthRepository` 增加 `GetWindowAsync(capturedFromUtc)` 与 `DeleteCapturedBeforeAsync(capturedBeforeUtc)`,中文 XML 注释;`ClusterHealthRepository` 实现(查询 OrderBy CapturedAt/ThenBy Id;删除用 ExecuteDeleteAsync)
- [x] 1.2 `Application/ViewModels/` 新增 `DashboardTrendPointViewModel`(CapturedAtUtc/ReadyNodes/NotReadyNodes),类型级中文注释

## 2. 保留清理服务

- [x] 2.1 `SnapshotRetentionService`(Application,singleton,AddApplicationServices 注册):解析 `ClusterSync:SnapshotRetentionDays`(默认 90,非法回退,日志风格与 ResolveInterval 一致)+ 60 分钟进程内限频(TimeProvider 注入)+ `CleanupIfDueAsync()` 全 try/catch
- [x] 2.2 `ClusterService` 注入该服务,`RefreshAllClustersStatusAsync` 成功收尾调用 `CleanupIfDueAsync()`;更新既有手动 new 构造点的测试(补参)

## 3. 服务层单测

- [x] 3.1 `DashboardServiceTests`:趋势口径(事件推进:新快照后曲线值切换;离线集群末快照持续计入;从未探测集群不参与;空窗口→空列表)
- [x] 3.2 `ClusterHealthRepositoryTests`:`GetWindowAsync` 区间/排序;`DeleteCapturedBeforeAsync` 删除计数与余量
- [x] 3.3 `SnapshotRetentionServiceTests`:默认 90 天清理生效;配置 30 天生效;非法配置回退 90(匹配既有配置解析测试风格);限频(推时钟两轮只删一次);删除异常不抛、仅告警
- [x] 3.4 `ClusterServiceTests` 收尾:全量刷新后调用过清理(mock 仓储断言)

## 4. 看板趋势卡 UI

- [x] 4.1 `DashboardService.GetNodeReadinessTrendAsync()`(D1 扫描)+ `Dashboard.razor` 插入 `.dashboard-trends` 区块;`app.css` 第 15 节新增 `.dashboard-trends`/`.dashboard-trend-note`/图例样式
- [x] 4.2 `DashboardNodeTrendCard.razor`(SVG step-after 双线 + 图例 + 「近 24 小时」注记 + 空态)
- [x] 4.3 bUnit:`DashboardPageTests` 补趋势区块接线(有 Series 时渲染曲线;空 Series 渲染 `暂无趋势数据`);新文件 `DashboardTrendCardTests`(路径点/图例/注记/空态分支)
- [x] 4.4 `DashboardServiceTests` 新增:trend 计入零 K8s 调用语义(calls 不触发任何 client mock 调用——用 no-op mock 仓储即可)

## 5. 收尾

- [x] 5.1 `openspec validate "add-dashboard-health-trend" --strict` 通过
- [x] 5.2 `dotnet build` 0 错误 + `dotnet test` 全绿(基线 1141 + 新增)+ `./coverage.ps1` ≥ 75%
