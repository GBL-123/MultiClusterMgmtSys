using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Daos;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    public DbSet<ClusterGroup> ClusterGroups => Set<ClusterGroup>();
    public DbSet<ClusterInfo> Clusters => Set<ClusterInfo>();

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

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
