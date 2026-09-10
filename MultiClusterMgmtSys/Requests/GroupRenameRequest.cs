namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 分组重命名的入参,由 <see cref="MultiClusterMgmtSys.Services.GroupService"/> 的重命名方法(RenameGroupAsync)消费;分组不存在时抛业务异常。
/// </summary>
/// <param name="Id">目标分组 Id(数据库主键)。</param>
/// <param name="NewName">新分组名。</param>
public record GroupRenameRequest(int Id, string NewName);