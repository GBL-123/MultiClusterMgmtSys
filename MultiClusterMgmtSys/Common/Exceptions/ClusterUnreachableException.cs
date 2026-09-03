namespace MultiClusterMgmtSys.Common.Exceptions;

/// <summary>集群不可达(连接失败或超时)。</summary>
public sealed class ClusterUnreachableException(string userMessage) : BusinessException(userMessage);