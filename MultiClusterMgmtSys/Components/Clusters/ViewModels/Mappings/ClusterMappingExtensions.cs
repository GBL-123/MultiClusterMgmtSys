using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Features.Clusters.ViewModels;

namespace MultiClusterMgmtSys.Features.Clusters.ViewModels.Mappings;

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
            IsReachable = false
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
}
