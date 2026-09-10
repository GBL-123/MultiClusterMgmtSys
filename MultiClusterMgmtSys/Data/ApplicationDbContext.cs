using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Data;

/// <summary>
/// 应用数据库上下文:同时承载 ASP.NET Identity 表(角色 Admin/Member,主键 int)与业务实体。
/// 库结构由启动时的 EnsureCreated 直接生成,本仓库不含 EF 迁移;
/// 修改模型后需删除 SQLite 数据库文件,由下次启动重建。
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    /// <summary>集群分组表;删除分组时其下集群的 GroupId 按 SetNull 置空,集群本身保留。</summary>
    public DbSet<ClusterGroup> ClusterGroups => Set<ClusterGroup>();

    /// <summary>集群主表:名称、API 地址、凭据、可达状态与所属分组等。</summary>
    public DbSet<ClusterInfo> Clusters => Set<ClusterInfo>();

    /// <summary>集群端点表:管理员登记的 VIP/域名元数据,随所属集群级联删除。</summary>
    public DbSet<ClusterEndpoint> ClusterEndpoints => Set<ClusterEndpoint>();

    /// <summary>节点 IP 备注表:随所属集群级联删除,(ClusterId, NodeName, Address) 三元组唯一。</summary>
    public DbSet<NodeIpRemark> NodeIpRemarks => Set<NodeIpRemark>();

    /// <summary>审计日志表:记录用户关键操作,CreatedAt 建有索引以支撑按时间排序分页。</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>应用设置表:持久化键值对配置,Key 唯一。</summary>
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    /// <summary>
    /// 配置实体映射与库级约束:凭据列用 TEXT 且 SkipTlsVerify 默认 true;
    /// 端点与节点 IP 备注随集群级联删除,分组删除时集群 GroupId 置空(SetNull);
    /// 节点 IP 备注按 (ClusterId, NodeName, Address) 唯一索引,审计日志按 CreatedAt 建索引,
    /// 应用设置按 Key 唯一索引,用户 CreatedAt 使用数据库默认值 CURRENT_TIMESTAMP。
    /// </summary>
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

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.Property(e => e.Key).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(256);
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}
