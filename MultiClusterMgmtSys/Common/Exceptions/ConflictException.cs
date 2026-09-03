namespace MultiClusterMgmtSys.Common.Exceptions;

/// <summary>资源冲突/并发修改(如 K8s 409)。</summary>
public sealed class ConflictException(string userMessage) : BusinessException(userMessage);