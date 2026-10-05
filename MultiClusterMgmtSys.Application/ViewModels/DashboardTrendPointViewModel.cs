namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 看板节点就绪趋势曲线上的一个数据点:一次快照事件推进后的舰队就绪/未就绪合计。
/// 序列整体按采集时间升序排列,构成阶梯式时间线(契约 cluster-dashboard「节点就绪趋势」)。
/// </summary>
/// <param name="CapturedAtUtc">数据点时刻(UTC),即触发推进的那条快照的采集时间。</param>
/// <param name="ReadyNodes">该时刻的舰队就绪节点合计。</param>
/// <param name="NotReadyNodes">该时刻的舰队未就绪节点合计。</param>
public record DashboardTrendPointViewModel(
    DateTime CapturedAtUtc,
    int ReadyNodes,
    int NotReadyNodes);
