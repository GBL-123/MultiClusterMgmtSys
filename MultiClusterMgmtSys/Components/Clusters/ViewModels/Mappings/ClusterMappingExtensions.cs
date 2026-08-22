using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Components.Clusters.ViewModels.Mappings;

public static class ClusterMappingExtensions
{
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
