namespace MultiClusterMgmtSys.ViewModels;

/// <summary>工作负载滚动三态:就绪(稳定)/ 滚动中(变更进行中)/ 未就绪(卡住或副本不足)。</summary>
public enum WorkloadRolloutState
{
    Ready = 0,
    Rolling = 1,
    NotReady = 2
}
