using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    public DbSet<ClusterGroup> ClusterGroups => Set<ClusterGroup>();

    public DbSet<ClusterInfo> Clusters => Set<ClusterInfo>();

    public DbSet<ClusterEndpoint> ClusterEndpoints => Set<ClusterEndpoint>();

    public DbSet<NodeIpRemark> NodeIpRemarks => Set<NodeIpRemark>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClusterGroup>(entity =>
        {
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<ClusterInfo>(entity =>
        {
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.KubeConfig).HasColumnType("TEXT");
            entity.Property(e => e.Token).HasColumnType("TEXT");
            entity.Property(e => e.SkipTlsVerify).HasDefaultValue(true);

            entity.HasOne(e => e.Group)
                  .WithMany(g => g.Clusters)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ClusterEndpoint>(entity =>
        {
            entity.Property(e => e.Value).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Note).HasMaxLength(64);

            entity.HasOne(e => e.Cluster)
                  .WithMany(c => c.Endpoints)
                  .HasForeignKey(e => e.ClusterId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NodeIpRemark>(entity =>
        {
            entity.Property(e => e.Note).HasMaxLength(64);
            entity.HasIndex(e => new { e.ClusterId, e.NodeName, e.Address }).IsUnique();

            entity.HasOne(e => e.Cluster)
                  .WithMany(c => c.NodeIpRemarks)
                  .HasForeignKey(e => e.ClusterId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.Target).IsRequired();
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
