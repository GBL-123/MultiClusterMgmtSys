namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 读取单个 Pod 容器日志的入参,由 <see cref="MultiClusterMgmtSys.Services.PodService"/> 的日志方法(GetPodLogAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Namespace">Pod 所在命名空间。</param>
/// <param name="Name">Pod 名称。</param>
/// <param name="Container">容器名;null/空 = 按 K8s 默认容器语义。</param>
/// <param name="TailLines">最多返回的日志行数(服务端截断)。</param>
/// <param name="Previous">是否读取上次运行(重启前)的日志。</param>
public record PodLogRequest(int ClusterId, string Namespace, string Name, string? Container, int TailLines, bool Previous);
