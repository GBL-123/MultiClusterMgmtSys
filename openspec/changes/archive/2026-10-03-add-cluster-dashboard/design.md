# Design

## Context

动机与范围见 `proposal.md`;需求契约见 `specs/`。以下是塑造本方案的既有事实:

- **后台同步已在保鲜**:`ClusterSyncBackgroundService` 按生效间隔(默认 5 分钟,可配 1~1440)调用 `ClusterService.RefreshAllClustersStatusAsync`,以有界并发(4)执行 `ProbeAsync`,并把 `Status`/`Version`/`NodeCount`/`LastCheckedAt` 落库到 `ClusterInfo`。看板所需的绝大部分数据**已经在库里**。
- **探测正在丢弃已付费的数据**:`ProbeAsync` 调用 `client.CoreV1.ListNodeAsync(...)` 后只取 `nodeList.Items.Count`。完整的 `V1Node` 数组(含 `Status.Conditions`、`Status.Capacity`、`Status.NodeInfo`)当轮即弃。
- **无 EF migrations**:`Program.cs` 仅 `db.Database.EnsureCreated()`。EF Core 的 `EnsureCreated` 在数据库文件已存在时不补建表/列,因此**任何 schema 变更都需要删库重建**,已登记的集群凭据需重新录入。
- **既有降级契约受 spec 保护**:`cluster-scheduled-sync` 明确断言探测失败时 `NodeCount` 置 0。该行为有 scenario 覆盖,不得随手更改。
- **默认落地页在两处**:`Home.razor` 的 `/` 重定向,以及 `Program.cs:72` 中 `OnRedirectToLogin` 对空/根 `returnUrl` 的回退值 `/clusters`。
- **分层与契约**:服务返回 ViewModel、入参收拢为 Request(见 `service-contracts`);端口在 `Application/Abstractions`、实现在 `Infrastructure/Persistence`(见 `architecture-layering`);Web 组件命名空间不得触碰 EF/仓库端口/k8s 类型。
- **设计系统**:Swiss Industrial Print 全亮色、内容卡片默认 elevation 阴影 + 发丝线、等宽数字(见 `ui-theme`);枚举与应用自有状态的中文口径见 `display-conventions`。

## Goals / Non-Goals

**Goals:**

- 看板在**零 Kubernetes API 调用**下完成渲染,且在全部集群离线时仍然可用。
- 探测扩展**不新增任何 K8s 调用**——只解析当轮已经取回的数据。
- 快照表的数据形态使**趋势能力无需再次 schema 变更**即可在后续获得。
- 既有同步契约(降级、取消、翻转审计、互斥)零变化。

**Non-Goals:**

- 不在看板中做实时巡检、部分失败态或超时预算设计。
- 不在本次做趋势图、时间范围查询或聚合降采样。
- 不采集容量、Pod、事件等需要额外调用或额外列的指标。
- 不引入 metrics-server 依赖。
- 不做快照保留策略。
- 不做页面级定时自动重读。

## Decisions

### D1 看板读本地快照,不连集群

**选择**:页面数据全部来自 SQLite。

**备选**:打开页面时对全部集群实时巡检。

**理由**:决定性的是可用性——**当所有集群都不可达时,实时巡检方案恰好失效,而那正是最需要看板的时刻**。其次,后台同步每 5 分钟已在做同一件事,实时巡检是重复劳动,且会引入部分失败态、每集群超时预算与进度反馈三套额外设计。代价是数据有 0~N 分钟延迟,由 D7 的新鲜度机制显式对冲。

### D2 新建 `ClusterHealthSnapshot` 表并**追加**,而非给 `ClusterInfo` 加列

**选择**:独立表,每次成功探测追加一行。

**备选**:(a) 给 `ClusterInfo` 加 `NotReadyNodeCount` 列;(b) 独立表但每集群覆盖式 upsert;(c) 写入既有 `AppSetting` 的 JSON。

**理由**:

- (a) 让「集群主档」承担健康快照职责,且把未来的路堵死——趋势仍需建表,而那时表里没有历史。
- (c) 把配置表当数据表用,不可接受。
- (b) 与 (a) 同样没有历史。
- 追加式的边际成本仅是「INSERT 而非 UPDATE」+「取最新行的一次分组查询」,而收益是**从第一天起积累时间序列**。本项目「多集群」的差异化正需要趋势这种别人难以复刻的能力,让数据先攒起来几乎不花钱。

**权衡**:表会持续增长。按 7 集群、5 分钟间隔估算约 2000 行/天,SQLite 完全无感;保留策略列为后续。

### D3 只在探测成功时写快照

**选择**:失败与取消均不写。

**备选**:失败时也写一行(节点字段为空)。

**理由**:`ClusterInfo.LastCheckedAt` 已经记录了失败尝试的时刻,失败行不携带额外信息,却会让「每集群最新一行」的语义变复杂(需再判断该行是否成功)。保持「最新一行 = 最近一次成功探测」这一条简单不变量。

### D4 舰队规模取快照的最近成功值,而非 `ClusterInfo.NodeCount` 之和

**选择**:规模 = Σ(每集群最近一条快照的节点总数)。

**备选**:改 `ClusterInfo.NodeCount` 使其在失败时不清零。

**理由**:后者会改动有 scenario 保护的既有降级契约(且「离线 → 不知道」在单集群视图里是合理的)。用快照值可以在**不触碰既有契约**的前提下让舰队数字不说谎:集群离线时规模不缩水,且因为快照带采集时间,看板可以诚实标注这是何时的事。

### D5 探测扩展以 `ADDED` 增量表达,不用 `MODIFIED`

**选择**:`specs/cluster-scheduled-sync/spec.md` 使用 `## ADDED Requirements` 新增一条需求。

**理由**:既有可观测行为(`Status`/`Version`/`NodeCount`/`LastCheckedAt` 语义、降级、取消)全部不变,本次只是**增加**采集与落库。按 OpenSpec 的 MODIFIED 工作流需整块复制既有需求,反而在归档时有丢细节的风险。

### D6 最近操作复用既有审计可见范围

**选择**:Admin 走 `AuditService.GetPagedAsync`(`isAdmin` 为真,取前 N 条),Member 走 `AuditService.GetRecentAsync`。

**备选**:新增「全站最近操作」仓库方法;或为看板放宽可见范围。

**理由**:两个既有方法已经覆盖两种角色,零新增审计代码。放宽可见范围会违反 `audit-log` 的明文契约。服务层强制过滤的既有实现天然满足「不得仅依赖 UI 隐藏」。

### D7 新鲜度基准为「生效同步间隔 × 2」,停用时改显示停用

**选择**:以 `ClusterSyncSettingService` 读出的生效间隔为基准,超过 2 倍即告警。

**理由**:间隔可由管理员配到 1440 分钟,**固定阈值必然错**(5 分钟间隔下 47 分钟是异常,1440 分钟间隔下 47 分钟完全正常)。取 2 倍作为「至少漏了一轮」的宽松判定。同步停用时陈旧是预期状态而非故障,故显示停用文案。

**附带收益**:这是系统里第一个能发现「后台同步已停止」的地方——目前该故障无人知晓。

### D8 查询形状:两个只读端口,计数在服务层内存完成

**选择**:

- 新增 `IClusterHealthRepository`:追加写入 + 「每集群最新一条」查询。
- `IClusterRepository` 新增一个无跟踪、不加载导航集合的「取全部集群(含分组)」只读方法,与既有 `GetAllForSyncAsync()` 对称。
- 状态计数、版本分布、分组健康均在服务层对内存集合聚合。

**备选**:新增 `GetStatusCountsAsync()` 之类的聚合 SQL。

**理由**:集群表规模是数十行,一次读取后内存聚合比多次往返更简单,也少一个端口方法。真正出现性能问题再抽聚合——与本项目「先复用、不预先抽象」的一贯做法一致。

### D9 快照索引与「最新行」查询

**选择**:`ClusterHealthSnapshot` 建 `(ClusterId, CapturedAt)` 复合索引;最新行按 `ClusterId` 分组取 `CapturedAt` 最大。集群外键级联删除。

**理由**:该索引同时服务 v1 的「每集群最新一条」与后续的趋势区间查询,避免以后再动 schema 加索引。

### D10 页面构成与设计系统契合

**选择**:`Web/Components/Dashboard/`(`Pages/Dashboard.razor` + `Shared/` 区块组件)。

- **无** `MudTable`、**无** `ClusterSelectSidebar`、**无**分页与管理按钮——它是聚合快照,不是第二张集群列表。
- 布局为「看板板面卡 + 指标行 + 清单行」:板面卡(默认 elevation)内报头区与 KPI 统计带以 2px 墨线分区;主体第一行三张指标卡(分组健康 / 版本分布 / 节点就绪),第二行两张清单卡(需要关注 / 最近操作),同行卡片由 grid 拉伸为同高;≤1280px 折为单列。节点就绪复用快照中既有的就绪/未就绪数(零 K8s 调用),无快照时显示紧凑空态。
- 字号对比承载层级:板面标题 28–32px `font-grotesk` 紧字距;KPI 数字 `.dashboard-stat-value` 56px 固定值(不用 `vw`,`html ` 前缀压过 Mud 的 body1 字号)对 11px 字距拉开的等宽标签;每格带 `01`–`05` 技术编号;KPI 带用 `auto-fit minmax(200px,1fr)` 且**不设 `overflow: hidden`**(避免窄屏裁切)。数据行与计量条用 `font-mono` + 发丝线分隔,卡头 13px 墨色等宽标题 + 1px 墨线,不引入新色。
- 状态计数仅在非零时着色(在线绿/离线红/未知琥珀),零值保持墨色避免误报;状态用既有 `StatusBadge`;空态用既有 `.empty-state`(卡内紧凑变体)。
- 内容卡片/面板保持 MudBlazor 默认 elevation(默认 1),与全站一致;**不**写 `Elevation="0"`(无阴影例外沿用 `ui-theme` 既有清单)。
- 新增样式进全局 `app.css` 的看板段落。**不**登记进 `{feature}-table` 的 flex-fill 规则组——该规则组是给表格页的,看板不是表格页。
- 最近操作的目标文本截断时经统一 `TextTooltip` 暴露完整内容(不违反 `ui-theme` 的 tooltip 契约)。
- 不引入新的展示性原生元素,不违反 `ui-theme` 的组件优先策略。

**理由**:避免看板退化为「换皮的第二张集群列表」。两者的分工是:集群列表页 = 管理视角(分页/筛选/增删改/分组编排),看板 = 健康视角(不分页/只看整体与异常)。

### D11 落地页与导航

**选择**:`Home.razor` 的 `/` 重定向目标与 `Program.cs:72` 的 `returnUrl` 回退值均由 `/clusters` 改为 `/dashboard`;Drawer 在「集群管理」之前插入看板入口。

**理由**:登录第一眼应是舰队全貌。`fallback-pages` 的「返回集群列表」入口**保持不变**——那是 404/错误页的显式动作,指向集群列表仍然正确。

### D12 不做页面级定时自动重读

**选择**:仅提供「刷新全部」手动入口 + 常显新鲜度。

**备选**:每 30 秒轮询重读 SQLite。

**理由**:新鲜度指示 + 手动刷新已覆盖需求,且后台同步本身在保鲜(下次打开页面自然是最新)。轮询会为每个在线用户增加一个常驻循环,收益不足。若后续确需,可独立添加而不影响本设计的其他部分。

### D13 分层落位与依赖影响

**选择**:

- 实体 `ClusterHealthSnapshot` → `Domain/Entities`。
- 端口 `IClusterHealthRepository` → `Application/Abstractions`;实现 → `Infrastructure/Persistence`。
- `DashboardService` + `DashboardViewModel` → `Application`。
- `ClusterService` 构造函数新增 `IClusterHealthRepository` 依赖。

**影响**:`ClusterService` 的构造签名变更会波及 `ClusterServiceTests`、`ClusterRefreshConcurrencyTests` 与测试脚手架(`ServiceHarness` 等)——这是本次改动中最需要小心的一处,但范围可控。

## Risks / Trade-offs

- **[新增表需要删库重建,已登记凭据丢失]** → 项目文档已把「模型变更 = 删库重建」列为既定流程;影响面仅限开发态与自建部署;在 AGENTS.md 补记本次变更的重建要求。回滚同样只需还原代码并重建库。
- **[舰队规模可能包含陈旧值]** → 快照携带采集时间,看板标注数据时间;贡献陈旧值的集群本身已在「需要关注」清单中单列。
- **[后台同步停滞时看板会安静地展示旧数据]** → 新鲜度常显 + 超过 2 倍间隔告警是本设计的显式防线,也是 D7 存在的根本原因。
- **[快照表无保留策略会持续增长]** → 量级无感(约 2000 行/天/7 集群);复合索引保证查询不随行数退化;保留策略列为后续 change。
- **[看板与集群列表页存在冗余风险]** → 通过 D10 的功能边界(无分页/无筛选/无管理操作)固化分工,避免退化为重复页面。
- **[若后续要容量等指标需第二次删库]** → v1 不预埋未使用列(避免投机性设计);开发态下第二次重建的代价可接受,且 D2 的表形态使「加列」是纯增量操作而非重构。
- **[`ClusterService` 构造签名变更波及多个测试]** → 在 tasks 中把测试脚手架的同步更新列为独立条目,避免遗漏导致编译失败。

## Migration Plan

1. 停止应用。
2. 删除 `MultiClusterMgmtSys.Web/db/` 下的库文件(或整个目录;SQLite 文件与其 `-shm`/`-wal` 一并删除)。
3. 部署新版本并启动:`EnsureCreated` 重建含 `ClusterHealthSnapshot` 的完整 schema,并按既有逻辑播种管理员账号。
4. 重新录入集群与凭据(或从备份的旧库手工比对重建)。
5. 等待一轮后台同步(默认 5 分钟内)或手动点「刷新全部」,快照开始积累。

**回滚**:还原代码 → 再次删除库文件 → 启动重建。由于无迁移脚本,回滚不涉及数据转换,只损失已积累的快照历史。

## Open Questions

- 看板在侧边导航与页面标题中的中文命名(「集群看板」/「总览」/「运行总览」)。纯文案选择,不影响规格、方案或任务拆分,实现时确定即可。
