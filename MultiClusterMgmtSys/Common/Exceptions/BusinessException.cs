namespace MultiClusterMgmtSys.Common.Exceptions;

/// <summary>
/// 业务异常基类。<see cref="UserMessage"/> 是可直接展示给用户的中文文案。
/// </summary>
public abstract class BusinessException(string userMessage) : Exception(userMessage)
{

    /// <summary>面向用户的中文提示文案。</summary>
    public string UserMessage { get; } = userMessage;
}