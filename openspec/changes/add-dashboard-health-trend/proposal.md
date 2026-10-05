# Proposal

## Why

节点健康快照表目前只被看板读「每集群最新一条」,采集时间序列本身从未被利用;同时快照是只追加表,没有保留策略,按现有规模(7 集群 / 5 分钟间隔)约 2000 行/天持续积压,长期运行会无限增长。把时间序列图出来(健康趋势)并配套自动清理(保留策略),一次把这张表的产出与成本同时解决。

## What Changes

- 看板新增「节点就绪趋势」区域:读本地快照生成最近 24 小时舰队就绪/未就绪计数的时间序列曲线,零 Kubernetes 调用,不改变既有「节点就绪」点值卡与任何其他看板区块。
- 引入节点健康快照保留策略:默认保留 90 天,经 appsettings `ClusterSync:SnapshotRetentionDays` 可配置;在每轮全量刷新(手动「刷新全部」与定时轮次共用路径)完成后执行过期清理,进程内限频每小时至多一次,失败只记日志。
- 清理属于自动维护操作,不写审计、不新增 K8s 调用。

## Capabilities

### New Capabilities

(无)

### Modified Capabilities

- `cluster-dashboard`:新增「节点就绪趋势」requirement——趋势曲线的数据源(本地快照)、口径(逐快照事件的舰队合计阶梯序列)、空态与零调用约束。
- `cluster-scheduled-sync`:新增「快照保留清理」requirement——保留窗口默认与配置来源、清理时机与限频、失败降级与不写审计。

## Impact

- `Application/Services/DashboardService.cs`:新增趋势聚合方法(逐快照事件推进的舰队合计)。
- `Application/Abstractions/IClusterHealthRepository.cs` + `Infrastructure/Persistence/ClusterHealthRepository.cs`:新增窗口区间查询与过期删除方法。
- `Application/Services/ClusterService.cs`:全量刷新收尾调用快照保留清理(注入轻量清理服务,手动与定时两条轮次路径同时覆盖)。
- `Web/Components/Dashboard/`:新增 `DashboardNodeTrendCard` 组件并在看板页新增全宽区块;`app.css` 第 15 节追加趋势卡样式(SVG 曲线、图例、时间轴注记)。
- `Web/appsettings.json`:新增 `ClusterSync:SnapshotRetentionDays` 配置项(缺省按代码默认 90 生效,不需要必配)。
- 测试:`DashboardServiceTests` 趋势口径、`ClusterHealthRepositoryTests` 区间查询/删除、保留清理服务单测、`DashboardPageTests`/趋势卡 bUnit。
