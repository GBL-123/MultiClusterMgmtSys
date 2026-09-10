namespace MultiClusterMgmtSys.ViewModels;

/// <summary>工作负载滚动三态:就绪(稳定)/ 滚动中(变更进行中)/ 未就绪(卡住或副本不足)。</summary>
public enum WorkloadRolloutState
{
    /// <summary>就绪:期望副本全部就绪且无进行中的变更。</summary>
    Ready = 0,
    /// <summary>滚动中:升级仍在进行,新旧副本未对齐。</summary>
    Rolling = 1,
    /// <summary>未就绪:就绪副本不足或滚动停滞。</summary>
    NotReady = 2
}
