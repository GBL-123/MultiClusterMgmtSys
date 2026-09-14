namespace MultiClusterMgmtSys.Models;

/// <summary>
/// 事件对象类型分类条的单个分类选项:关联对象 kind 与其在其余筛选结果中的计数。
/// </summary>
/// <param name="Kind">关联对象 kind 原始值(如 Pod/Node/Deployment)。</param>
/// <param name="Count">该 kind 在其余筛选条件(命名空间/级别/关键词)作用后的事件计数。</param>
public record EventKindOption(string Kind, int Count);
