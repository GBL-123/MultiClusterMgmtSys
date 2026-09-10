using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Requests;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

/// <summary>
/// 集群实体 → 列表/详情/编辑展示模型的映射,及端点集合的写回。
/// </summary>
public static class ClusterMappingExtensions
{
    /// <summary>集群实体 → 集群列表展示 ViewModel 映射(状态中文文本同步计算)。</summary>
    public static ClusterViewModel ToViewModel(this ClusterInfo e)
    {
        return new ClusterViewModel
        {
            Id = e.Id,
            Name = e.Name,
            Status = e.Status,
            StatusText = e.Status switch
            {
                ClusterStatus.Online => "在线",
                ClusterStatus.Offline => "离线",
                _ => "未知"
            },
            Version = e.Version,
            NodeCount = e.NodeCount,
            GroupId = e.GroupId,
            GroupName = e.Group?.Name,
            ApiServer = e.ApiServer,
            CreatedAt = e.CreatedAt,
            LastCheckedAt = e.LastCheckedAt,
            ConnectionType = e.ConnectionType
        };
    }

    /// <summary>集群实体 → 集群详情展示 ViewModel 映射(端点按类型与序号排序,节点由服务层另行填充)。</summary>
    public static ClusterDetailViewModel ToDetailViewModel(this ClusterInfo e)
    {
        return new ClusterDetailViewModel
        {
            Id = e.Id,
            Name = e.Name,
            Status = e.Status,
            StatusText = e.Status switch
            {
                ClusterStatus.Online => "在线",
                ClusterStatus.Offline => "离线",
                _ => "未知"
            },
            Version = e.Version,
            NodeCount = e.NodeCount,
            GroupId = e.GroupId,
            GroupName = e.Group?.Name,
            ApiServer = e.ApiServer,
            CreatedAt = e.CreatedAt,
            LastCheckedAt = e.LastCheckedAt,
            ConnectionType = e.ConnectionType,
            Nodes = new(),
            IsReachable = false,
            Endpoints = e.Endpoints
                .Select(ep => new ClusterEndpointViewModel
                {
                    Id = ep.Id,
                    Kind = ep.Kind,
                    KindText = ep.Kind == ClusterEndpointKind.Vip ? "VIP" : "域名",
                    Value = ep.Value,
                    Note = ep.Note,
                    SortOrder = ep.SortOrder
                })
                .OrderBy(ep => ep.Kind)
                .ThenBy(ep => ep.SortOrder)
                .ToList()
        };
    }

    /// <summary>集群实体 → 集群编辑表单回填 ViewModel 映射。</summary>
    public static ClusterEditViewModel ToEditViewModel(this ClusterInfo e)
    {
        return new ClusterEditViewModel
        {
            Id = e.Id,
            Name = e.Name,
            GroupId = e.GroupId,
            ApiServer = e.ApiServer,
            ConnectionType = e.ConnectionType,
            SkipTlsVerify = e.SkipTlsVerify,
            KubeConfig = e.KubeConfig,
            Token = e.Token
        };
    }

    /// <summary>
    /// 全量替换集群端点集合。校验不变式：
    /// Value 非空（trim 后）且 ≤ 256 字符；Note ≤ 64 字符——
    /// "service 是端点生存与否的唯一权威"。
    /// 此方法只修改实体内存集合，SaveChanges 由调用方的 UpdateAsync 提交。
    /// </summary>
    public static void ApplyEndpoints(this ClusterInfo entity, IEnumerable<ClusterEndpointEditItem> items)
    {
        var list = items.Where(i => !i.IsDeleted).ToList();

        foreach (var item in list)
        {
            var value = item.Value?.Trim() ?? "";
            if (value.Length == 0 || value.Length > 256)
                throw new ArgumentException("端点地址不能为空且长度不能超过 256 字符");
            if (item.Note is not null && item.Note.Trim().Length > 64)
                throw new ArgumentException("端点备注长度不能超过 64 字符");
        }

        entity.Endpoints.Clear();
        foreach (var item in list)
        {
            entity.Endpoints.Add(new ClusterEndpoint
            {
                Kind = item.Kind,
                Value = item.Value!.Trim(),
                Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim(),
                SortOrder = item.SortOrder
            });
        }
    }
}
