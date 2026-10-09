# Proposal

## Why

目前问题的发现完全依赖人主动打开看板:集群离线、节点大面积未就绪、定时同步断流,都只能「碰巧看到」。而数据管道其实已经存在——定时同步每次成功探测都追加 `ClusterHealthSnapshot`,状态翻转已有审计——缺的只是一个独立评估器把异常变成持久的告警记录。告警让系统从「仪表盘」变成「值守系统」。

## What Changes

- 新增 `AlertService`(Application 层)+ 独立计时的告警评估后台服务(Infrastructure/Sync 旁路):分钟级轮询,**纯读 SQLite,零 K8s 调用**(与看板「零 K8s 聚合视图」哲学一致)。
- v1 三条评估规则:
  1. **集群离线**:集群 `Status == Offline` 持续超过 N 分钟;
  2. **节点未就绪**:最近快照 `NotReadyNodes > 0`(或较上份快照环比新增);
  3. **快照断流**:`LastCheckedAt` 超过过期阈值(阈值 = 生效同步间隔 × 2,复用看板新鲜度判定口径)。
- 评估器**独立计时**而非挂在同步轮末——否则「同步本身停了」这种最该告警的场景永远触发不了;同步停用时断流规则仍须可触发。
- 新增 `AlertRecord` 表:告警状态机 `open → resolved`,恢复(如集群回到 Online)也解析并保留记录——历史记录是价值;**建表变更,升级需删库重建**(参照 `add-cluster-dashboard` 先例)。
- 新增告警页面(路由 Admin-only,`[Authorize(Roles="Admin")]` 服务端强制)+ AppBar 铃铛入口(Admin `AuthorizeView`):**角标 = 当前 open 告警数**,无未读/已读概念。
- Admin-only 可见:v1 三条规则全是 Admin 域问题(修法是集群凭据/节点维护/同步设置,全是 Admin-only 操作),与账号、节点维护等仓库先例一致;Member 不因此变盲——看板「需要关注」本来就全员可见。

## Capabilities

### New Capabilities

- `alert-center`: 告警中心契约——三条 v1 评估规则的判定口径(离线持续/未就绪/断流阈值)、评估器独立计时与零 K8s 调用边界、`AlertRecord` 状态机与恢复记录、Admin-only 可见性(路由 + 铃铛双重强制)、铃铛角标 = open 数、无未读态。

### Modified Capabilities

<!-- 无:cluster-dashboard 的既有需求不变(看板页面零改动);cluster-scheduled-sync 的同步行为不变(评估器是旁路读取,不挂同步轮末);两者仅在阈值口径上复用,不构成需求变更。 -->

## Impact

- **新增代码**:`Domain/Entities/AlertRecord.cs`、`Application/Services/AlertService.cs`、告警评估后台服务(`Infrastructure/Sync`,与 `ClusterSyncBackgroundService` 并列)、`Infrastructure/Persistence/ApplicationDbContext`(新增 `DbSet<AlertRecord>`)、告警页面(`Web/Components/Alerts/`)+ AppBar 铃铛组件;测试 `AlertServiceTests`(三规则判定矩阵 + 状态机)+ bUnit 页面/铃铛测试。
- **数据库/Schema 变更**:新增 `AlertRecord` 表——升级必须删除 `MultiClusterMgmtSys.Web/db/` 下库文件后重启重建(`EnsureCreated` 不为既有库补建新表)。
- **K8s 调用**:零新增。评估器纯读 SQLite(集群主档 + 快照表),与看板同口径。
- **不动**:看板页面、同步链路(`RefreshAllClustersStatusAsync` / `ClusterSyncBackgroundService` / `SnapshotRetentionService`)一行不改。
- **进化口**:资源级规则(CrashLoop 等)按规则类别分流可见性(基础设施 → Admin,资源 → owner);v2 北极星 = 模板库漂移检测与之串联成 GitOps 骨架。
