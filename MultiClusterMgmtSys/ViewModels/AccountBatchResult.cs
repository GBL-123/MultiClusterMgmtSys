namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 账号批量操作结果,供前端统计成功与跳过条数。
/// </summary>
/// <param name="Processed">实际处理的账号数量。</param>
/// <param name="Skipped">因校验不通过等原因跳过的账号数量。</param>
public record AccountBatchResult(int Processed, int Skipped);