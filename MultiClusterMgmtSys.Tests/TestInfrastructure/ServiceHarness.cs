using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Services;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public sealed class ServiceHarness : IDisposable
{
    public ApplicationDbContext Db { get; } = SqliteDbFactory.CreateContext();

    public ClusterRepository ClusterRepo { get; }

    public AuditService Audit { get; }

    public ServiceHarness(string actor = "admin", params string[] roles)
    {
        var auditLogRepo = new AuditLogRepository(Db);
        Audit = new AuditService(
            auditLogRepo,
            TestHttpContext.For(actor, roles).Object,
            NullLogger<AuditService>.Instance);
        ClusterRepo = new ClusterRepository(Db);
    }

    public void Dispose() => Db.Dispose();
}
