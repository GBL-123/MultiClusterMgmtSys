namespace MultiClusterMgmtSys.Common.Exceptions;

/// <summary>无权限执行操作(如 K8s 403/401)。</summary>
public sealed class PermissionException(string userMessage) : BusinessException(userMessage);