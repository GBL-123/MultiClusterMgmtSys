namespace MultiClusterMgmtSys.Application.Enums;

/// <summary>批量下发在单个集群上实际执行的动作(由 release 是否已存在决定)。</summary>
public enum HelmFleetDeployAction
{
    /// <summary>安装:目标集群中该 release 不存在,执行 helm install 并写归属。</summary>
    Install = 0,

    /// <summary>升级:目标集群中该 release 已存在,执行 helm upgrade,归属保持不变。</summary>
    Upgrade = 1
}
