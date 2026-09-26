namespace MultiClusterMgmtSys.Domain.Exceptions;

/// <summary>Helm 操作失败且无法归入更具体业务异常类别(如二进制不可用、模板渲染、存储或未知 CLI 失败)。</summary>
public sealed class HelmOperationException(string userMessage) : BusinessException(userMessage);
