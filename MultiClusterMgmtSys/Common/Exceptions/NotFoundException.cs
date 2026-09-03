namespace MultiClusterMgmtSys.Common.Exceptions;

/// <summary>资源不存在(如集群、分组、ConfigMap)。</summary>
public sealed class NotFoundException(string userMessage) : BusinessException(userMessage);