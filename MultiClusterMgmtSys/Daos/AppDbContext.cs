using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Daos;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ClusterGroup> ClusterGroups => Set<ClusterGroup>();
    public DbSet<ClusterInfo> Clusters => Set<ClusterInfo>();
    public DbSet<Account> Accounts => Set<Account>();

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

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
        });
    }
}
