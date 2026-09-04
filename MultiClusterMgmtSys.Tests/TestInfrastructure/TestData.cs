using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>测试造数工具。</summary>
public static class TestData
{
    public static ClusterInfo NewCluster(string name, ClusterStatus status = ClusterStatus.Online, string? version = "v1.30.3", int? groupId = null, DateTime? createdAt = null)
    {
        return new ClusterInfo
        {
            Name = name,
            ApiServer = "https://127.0.0.1:6443",
            ConnectionType = ConnectionType.Token,
            Token = "token",
            SkipTlsVerify = true,
            Status = status,
            Version = version,
            NodeCount = status == ClusterStatus.Online ? 3 : 0,
            GroupId = groupId,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            LastCheckedAt = createdAt ?? DateTime.UtcNow
        };
    }

    public static ClusterGroup NewGroup(string name)
    {
        return new ClusterGroup
        {
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static AuditLog NewAuditLog(string userName, AuditCategory category, AuditAction action, string target, DateTime? createdAt = null)
    {
        return new AuditLog
        {
            UserName = userName,
            Category = category,
            Action = action,
            Target = target,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }
}