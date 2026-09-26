using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Infrastructure.Persistence;

/// <summary>
/// Helm release 归属仓储实现:经 SQLite 存取归属记录;
/// (ClusterId, Namespace, ReleaseName) 唯一索引保证同键至多一条,写入走 upsert 语义。
/// </summary>
public class HelmReleaseOwnershipRepository(ApplicationDbContext db) : IHelmReleaseOwnershipRepository
{
    private readonly ApplicationDbContext _db = db;

    /// <inheritdoc />
    public async Task<HelmReleaseOwnership?> GetAsync(int clusterId, string namespaceName, string releaseName)
        => await _db.HelmReleaseOwnerships
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.ClusterId == clusterId
                && o.Namespace == namespaceName
                && o.ReleaseName == releaseName);

    /// <inheritdoc />
    public async Task<List<HelmReleaseOwnership>> GetByClusterAsync(int clusterId)
        => await _db.HelmReleaseOwnerships
            .AsNoTracking()
            .Where(o => o.ClusterId == clusterId)
            .ToListAsync();

    /// <inheritdoc />
    public async Task UpsertAsync(HelmReleaseOwnership ownership)
    {
        var existing = await _db.HelmReleaseOwnerships
            .FirstOrDefaultAsync(o => o.ClusterId == ownership.ClusterId
                && o.Namespace == ownership.Namespace
                && o.ReleaseName == ownership.ReleaseName);
        if (existing is null)
        {
            _db.HelmReleaseOwnerships.Add(ownership);
        }
        else
        {
            existing.OwnerUserId = ownership.OwnerUserId;
            existing.OwnerUserName = ownership.OwnerUserName;
            existing.InstalledAt = ownership.InstalledAt;
            existing.InstalledRevision = ownership.InstalledRevision;
        }
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int clusterId, string namespaceName, string releaseName)
    {
        var existing = await _db.HelmReleaseOwnerships
            .FirstOrDefaultAsync(o => o.ClusterId == clusterId
                && o.Namespace == namespaceName
                && o.ReleaseName == releaseName);
        if (existing is null)
        {
            return;
        }
        _db.HelmReleaseOwnerships.Remove(existing);
        await _db.SaveChangesAsync();
    }
}
