# Design

## 术语与决策记录

- **D1 曲线口径 = 逐快照事件的舰队阶梯序列**(而非「每格固定采样」):窗口内按 `CapturedAt` 升序遍历快照,维护每集群当前值(初始为空,遇该集群首条快照后接入),每读一条写出一个点(时刻 = 快照 `CapturedAt`,值 = 各集群当前值之和)。此口径与「每集群最新一条即最近成功探测结果」的既有语义天然一致:集群离线/停写后其旧值持续在线直到窗口外,需要关注清单承担「离线告警」职能,趋势不重复承担。实现在 Application 服务内纯 LINQ 扫描,读数与一口气取 `GetDashboardAsync` 同量级(24h ≈ 2000 行)。
- **D2 窗口钉死 24 小时**(v1 不做配置):窗口可配置会带来空态/新鲜度与过期告警的双向纠缠(窗口 > 同步间隔 × 物理保留能力时要另造提示),收益低;v1 常量 `TrendWindowHours = 24`。
- **D3 保留清理挂在「全量刷新收尾」而非独立后台服务**:手动「刷新全部」与定时轮次走同一条 `ClusterService.RefreshAllClustersStatusAsync`,该收尾天然覆盖两条写入来源(手动刷新也追加快照);独立 `BackgroundService` 反而需要重复处理互斥与停机语义。进程内限频(实例字段,服务 singleton)保证手动连续点击也不连删。
- **D4 保明天数只在 appsettings**(不开 DB 运行时设置):与 `ClusterSync:Enabled/IntervalMinutes` 的 DB 优先级不同,保留天数是一次部署级策略而非运维高频参数,开卡片反而稀释个人信息页;若将来要运行时可调,再并入「定时同步设置」卡片下的同族设置。
- **D5 不降采样**:24h 全事件点(7 集群 ×288 ≈ 2016 点)对 SVG polyline 无渲染压力,降采样反而破坏「事件时刻即拐点」的可解释性。若将来集群数上百,在聚合方法前加分钟桶合并即可,接口不变。

## 架构落位(分层契约一致)

```
Domain:  无新实体(复用 ClusterHealthSnapshot)
Application/Abstractions:
  IClusterHealthRepository 增两方法(均为窄契约):
    Task<IReadOnlyList<ClusterHealthSnapshot>> GetWindowAsync(DateTime capturedFromUtc)
        —— CapturedAt >= from,OrderBy(CapturedAt)、ThenBy(Id)
    Task<int> DeleteCapturedBeforeAsync(DateTime capturedBeforeUtc)
Application/Services:
  DashboardService.GetNodeReadinessTrendAsync() → List<DashboardTrendPointViewModel>
      (D1 扫描;record CapturedAtUtc/ReadyNodes/NotReadyNodes)
  SnapshotRetentionService(IClusterHealthRepository, IConfiguration, ILogger) [singleton]
      —— CleanupIfDueAsync():解析 ClusterSync:SnapshotRetentionDays(默认 90,<1 或非整数回退,
         语法同 ClusterSyncSettingService.ResolveInterval 的 try/catch 风格),实例内限频 60 分钟,
         实际窗口起点 = now(UTC) - retention;try/catch 全包,失败仅 LogWarning
  ClusterService.RefreshAllClustersStatusAsync 成功收尾(含被互斥阻塞正常进入的轮次)后
      await _snapshotRetention.CleanupIfDueAsync()
      —— ClusterService 主 ctor 增一参;测试内手动 new 的构造点同步补参(与既有 5 参补位点同法)
Infrastructure/Persistence:
  ClusterHealthRepository 实现上述两方法(删除用 EF ExecuteDeleteAsync,单条 SQL)
Web/Components/Dashboard:
  Shared/DashboardNodeTrendCard.razor:参数 Series(List<DashboardTrendPointViewModel>)
      —— 纯 SVG viewBox 0 0 600 160 preserveAspectRatio="none";两条 <path>(就绪 is-online 绿 /
         未就绪 is-offline 红,stroke 1.5,fill 无,阶梯 step-after 折线);<polyline> 由 @code 里
         StringBuilder 拼 points,组件不含 JS;头注「// 节点就绪趋势」+「近 24 小时」
         (font-mono caption)+右侧图例两枚小色块+中文;空 Series → .empty-state is-compact
         「[ 暂无趋势数据 ]」
  Pages/Dashboard.razor:.dashboard-metrics 行与 .dashboard-lists 行之间插入
      <div class="dashboard-trends"><DashboardNodeTrendCard Series="@_trend" /></div>;
      _trend = await DashboardService.GetNodeReadinessTrendAsync()(LoadAsync 内一次调用,
      不新增 K8s 语义——同一次局部刷新重算)
  无集群空态分支(HasClusters=false)不渲染趋势卡(Spec: 无集群空态区块职责)
app.css 第 15 节(看板)追加:.dashboard-trends(单列全宽 grid)、.dashboard-trend-legend、
  .dashboard-trend-note(mono caption)、path 颜色沿用现役 .is-online/.is-offline 色值;
  不登记进表格 flex-fill 规则组(看板本身就非表格页)
```

## 边界与契约一致性核对

- `kubernetes-call-timeout` / `k8s-client-cache`:趋势与清理零 K8s 调用,不适用。
- `display-conventions`:时间轴起点/终点用相对时间 +
  `HH:mm` 等宽注记;图例「就绪/未就绪」中文主标签;无枚举双语问题。
- `exception-handling`:趋势查询失败属于看板既有降级语义(整卡区域随页面 catch 呈错误兜底);
  清理失败是 LogWarning 静默(Spec scenario 已锁)。
- `unit-testing`:仓库用真实 SQLite(区间查询/删除用 `SqliteDbFactory.CreateContext`);
  限频与配置回退用假时钟不可行——限频允许注入 `TimeProvider`(测试用 FakeTimeProvider 推时验两轮只删一次)。
- 审计口径:`audit-log` 的 MUST NOT 清单(状态刷新/播种/只读查询)囊括自动维护;清理由
  `ClusterService` 收尾调用、不在用户请求线程上进行,不写审计(Spec 已有 scenario 声明)。

## 风险与开放问题

- 无。删除窗口、曲线口径、限频已为最小可争议集合;如未来想要 7 天/30 天切换,
  属于 `DashboardService` 的窗口参数化与卡片注记文案的小改动。
