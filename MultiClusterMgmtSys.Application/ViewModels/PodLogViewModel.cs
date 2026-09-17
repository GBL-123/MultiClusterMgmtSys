namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// Pod 日志展示数据:等宽查看器的文本内容与行数标注。
/// </summary>
public class PodLogViewModel
{
    /// <summary>日志文本全文(K8s 已按行数上限截断);无输出时为空串。</summary>
    public string Content { get; set; } = "";

    /// <summary>非空行数(供「共 N 行」标注);空内容为 0。</summary>
    public int LineCount => string.IsNullOrEmpty(Content)
        ? 0
        : Content.Split('\n').Count(line => line.TrimEnd('\r').Length > 0);
}
