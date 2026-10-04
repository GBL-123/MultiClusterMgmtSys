namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>持久卷声明查询请求:Namespace 为 null 时查全部命名空间。</summary>
/// <param name="ClusterId">集群 Id。</param>
/// <param name="Namespace">命名空间过滤,null 表示全部命名空间。</param>
public record StorageClaimQueryRequest(int ClusterId, string? Namespace);

/// <summary>以 YAML 创建持久卷声明的请求。</summary>
/// <param name="ClusterId">集群 Id。</param>
/// <param name="Yaml">持久卷声明 YAML 内容。</param>
public record StorageClaimCreateRequest(int ClusterId, string Yaml);

/// <summary>按名称定位单条持久卷声明的请求(详情/删除共用)。</summary>
/// <param name="ClusterId">集群 Id。</param>
/// <param name="Name">持久卷声明名称。</param>
/// <param name="Namespace">命名空间。</param>
public record StorageClaimKeyRequest(int ClusterId, string Name, string Namespace);

/// <summary>持久卷查询请求(集群级资源,无命名空间维度)。</summary>
/// <param name="ClusterId">集群 Id。</param>
public record StorageVolumeQueryRequest(int ClusterId);

/// <summary>按名称定位单条持久卷的请求(只读详情)。</summary>
/// <param name="ClusterId">集群 Id。</param>
/// <param name="Name">持久卷名称。</param>
public record StorageVolumeKeyRequest(int ClusterId, string Name);

/// <summary>存储类查询请求(集群级只读浏览)。</summary>
/// <param name="ClusterId">集群 Id。</param>
public record StorageClassQueryRequest(int ClusterId);
