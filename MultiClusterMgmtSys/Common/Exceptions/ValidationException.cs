namespace MultiClusterMgmtSys.Common.Exceptions;

/// <summary>输入或规则校验失败。</summary>
public sealed class ValidationException(string userMessage) : BusinessException(userMessage);