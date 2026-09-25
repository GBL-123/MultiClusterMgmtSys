# Tasks

## 1. 数据模型与持久化

- [x] 1.1 新增 `ClusterHealthSnapshot` 实体(`Domain/Entities`):集群标识、采集时间、节点总数、就绪数、未就绪数,含中文 XML 注释。验证:`dotnet build` 0 错误且 CS1591 零命中
- [x] 1.2 在 `ApplicationDbContext` 注册 `ClusterHealthSnapshot` 的 DbSet、`(ClusterId, CapturedAt)` 复合索引,并配置随 `ClusterInfo` 级联删除。验证:`dotnet build` 通过
- [x] 1.3 新增 `IClusterHealthRepository` 端口(`Application/Abstractions`):追加一条快照、查询每集群最新一条,含中文 XML 注释。验证:接口编译通过
- [x] 1.4 实现 `ClusterHealthRepository`(`Infrastructure/Persistence`)。验证:新增仓库测试(经 `SqliteDbFactory`)覆盖追加、每集群最新行选取、集群删除后快照级联删除
- [x] 1.5 `IClusterRepository` 新增「取全部集群(含分组、无跟踪)」只读方法并实现,与既有 `GetAllForSyncAsync` 对称。验证:仓库测试断言返回全部集群且携带分组名

## 2. 探测扩展(写路径)

- [x] 2.1 `ClusterService.ProbeAsync` 在既有 `ListNodeAsync` 结果上解析节点就绪统计:`Ready` 条件为 `True` 计就绪,`False`/`Unknown`/缺失 `Ready` 条件计未就绪。验证:单元测试覆盖三类条件,并断言「总数 = 就绪 + 未就绪」
- [x] 2.2 探测成功时追加一条快照;探测失败与停机取消时均不追加。验证:测试分别断言成功追加、失败不追加、取消不追加
- [x] 2.3 确认探测未新增 Kubernetes API 调用(节点列表调用次数与改动前一致)。验证:mock 校验 `ListNodeAsync` 调用次数为一次
- [x] 2.4 `ClusterService` 构造函数注入 `IClusterHealthRepository`,同步更新测试脚手架(`ServiceHarness` 等)与 `ClusterServiceTests`、`ClusterRefreshConcurrencyTests`。验证:`dotnet build` 0 错误且这两个测试文件全绿

## 3. 看板服务(读路径)

- [x] 3.1 新增 `DashboardViewModel`(`Application/ViewModels`),承载状态计数、需要关注项、分组健康、版本分布、舰队规模及其数据时间、新鲜度与过期判定、最近操作,含中文 XML 注释。验证:`dotnet build` 0 错误
- [x] 3.2 新增 `DashboardService.GetDashboardAsync()`,读取集群、快照、审计与同步设置后在内存聚合。验证:服务测试覆盖状态计数、分组健康(含未分组独立成项)、版本分布(含未探测独立成项)
- [x] 3.3 实现需要关注清单:状态为离线的集群与从未被探测(`LastCheckedAt` 为空)的集群,两者以可区分的标记呈现。验证:服务测试断言两类集群均入列且标记不同
- [x] 3.4 实现舰队规模口径:每集群最近一条快照的节点总数之和;离线集群贡献其最近成功值,从未探测的集群不贡献;包含陈旧值时可标注数据时间。验证:服务测试断言「某集群 `NodeCount` 已置 0 但规模仍计入其快照值」
- [x] 3.5 实现新鲜度:最近同步时间取 `LastCheckedAt` 最大值,超过生效间隔 2 倍时告警,同步停用时显示停用,无任何同步记录时显示「尚未同步」。验证:服务测试覆盖上述全部分支
- [x] 3.6 实现最近操作按角色可见:Admin 经 `AuditService.GetPagedAsync`(取前 N 条),非 Admin 经 `AuditService.GetRecentAsync`。验证:服务测试断言 Member 的清单不含他人记录
- [x] 3.7 将 `DashboardService` 与 `ClusterHealthRepository` 分别注册进 `AddApplicationServices()` 与 `AddInfrastructure()`。验证:应用启动无 DI 解析错误

## 4. 页面与导航(表现层)

- [x] 4.1 新增 `Web/Components/Dashboard/Pages/Dashboard.razor`(路由 `/dashboard`)与 `Shared/` 区块组件骨架。验证:`dotnet build` 0 错误且 bUnit 可渲染页面
- [x] 4.2 实现数字条、需要关注清单(每项可跳转集群详情)、分组健康、版本分布、最近操作、新鲜度区;状态用既有 `StatusBadge`,空态用既有 `.empty-state` 词汇。验证:bUnit 断言各区块渲染分支与空态
- [x] 4.3 实现「刷新全部」入口,复用 `ClusterService.RefreshAllClustersStatusAsync` 及其进度回调,刷新中展示进度并禁用重复触发。验证:bUnit 断言进度文本与禁用态
- [x] 4.4 实现页面级空态:无任何集群时展示空态与前往集群管理的入口,且不展示零值统计与过期告警。验证:bUnit 断言空态分支
- [x] 4.5 在 `wwwroot/css/app.css` 新增看板样式段落,使用既有设计 token;**不**登记进 `{feature}-table` 的 flex-fill 规则组。验证:`dotnet run` 下看板布局正常且其它表格页高度行为无回归
- [x] 4.6 在 `Drawer.razor` 的「集群管理」之前新增看板入口,经 `/dashboard` 前缀匹配保持激活态(中文名默认「集群看板」,可调整)。验证:bUnit 断言导航项存在、顺序与激活态
- [x] 4.7 落地页改造:`Home.razor` 的 `/` 重定向目标与 `Program.cs` 中 `OnRedirectToLogin` 的 `returnUrl` 回退值均由 `/clusters` 改为 `/dashboard`。验证:登录成功后进入 `/dashboard`,未登录访问 `/` 登录后回到 `/dashboard`

## 5. 测试与门禁

- [x] 5.1 全量测试通过。验证:`dotnet test MultiClusterMgmtSys.Tests` 全绿且测试数量不低于改动前基线
- [x] 5.2 覆盖率门禁不降。验证:`./coverage.ps1` 四程序集合并行覆盖率 ≥75%
- [x] 5.3 架构约束未被破坏:Web 组件命名空间不引用 Infrastructure/EF/仓库端口/k8s 类型,Application 不引用 EF Core。验证:`dotnet test` 中 `ArchitectureTests` 通过

## 6. 文档与交付

- [x] 6.1 在 AGENTS.md 补记:看板页面与路由、`ClusterHealthSnapshot` 表与其追加语义、默认落地页变更、以及本次模型变更要求的删库重建步骤。验证:文档更新可读且与实现一致
- [ ] 6.2 端到端手工验收:删除库文件 → 启动重建 → 登录后落到看板 → 登记集群 → 等待一轮同步或点「刷新全部」→ 看板显示计数、需要关注、分组、版本、规模与最近操作。验证:按上述步骤走查通过
