using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public static class TestData
{
    public static ClusterInfo NewCluster(
        string name = "cluster-1",
        int? groupId = null,
        ClusterStatus status = ClusterStatus.Online,
        string? version = "1.29.0",
        int nodeCount = 3,
        DateTime? createdAt = null)
    {
        var now = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return new ClusterInfo
        {
            Name = name,
            ApiServer = $"https://{name}:6443",
            ConnectionType = ConnectionType.Token,
            Token = "token-" + name,
            SkipTlsVerify = true,
            Status = status,
            Version = version,
            NodeCount = nodeCount,
            GroupId = groupId,
            CreatedAt = now,
            LastCheckedAt = now
        };
    }

    public static ClusterGroup NewGroup(string name = "group-1")
        => new() { Name = name, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

    public static ClusterEndpoint NewEndpoint(int clusterId, ClusterEndpointKind kind = ClusterEndpointKind.Vip, string value = "10.0.0.1", string? note = null)
        => new() { ClusterId = clusterId, Kind = kind, Value = value, Note = note, SortOrder = 0 };

    public static NodeIpRemark NewIpRemark(int clusterId, string nodeName = "node-1", string address = "192.168.1.10", string? note = null)
        => new() { ClusterId = clusterId, NodeName = nodeName, Address = address, Note = note };

    public static AuditLog NewAudit(
        string userName = "admin",
        AuditCategory category = AuditCategory.Cluster,
        AuditAction action = AuditAction.Create,
        string target = "cluster-1",
        DateTime? createdAt = null)
        => new() { UserName = userName, Category = category, Action = action, Target = target, CreatedAt = createdAt ?? DateTime.UtcNow };

    public static AppSetting NewSetting(string key, string value)
        => new() { Key = key, Value = value, UpdatedAt = DateTime.UtcNow };
}
