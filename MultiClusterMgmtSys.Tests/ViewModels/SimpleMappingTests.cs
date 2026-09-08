using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Tests.ViewModels;

public class AccountMappingTests
{
    [Fact]
    public void ToAccountViewModel_maps_all_fields()
    {
        var user = new ApplicationUser
        {
            Id = 7,
            UserName = "tester",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLoginAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var vm = user.ToAccountViewModel("Admin");

        Assert.Equal(7, vm.Id);
        Assert.Equal("tester", vm.UserName);
        Assert.Equal("Admin", vm.RoleName);
        Assert.Equal(user.CreatedAt, vm.CreatedAt);
        Assert.Equal(user.UpdatedAt, vm.UpdatedAt);
        Assert.Equal(user.LastLoginAt, vm.LastLoginAt);
    }

    [Fact]
    public void ToAccountViewModel_null_username_becomes_empty()
    {
        var vm = new ApplicationUser().ToAccountViewModel("");

        Assert.Equal("", vm.UserName);
    }
}

public class AuditLogMappingTests
{
    [Theory]
    [InlineData(AuditCategory.Authentication, "认证")]
    [InlineData(AuditCategory.Account, "账号")]
    [InlineData(AuditCategory.Cluster, "集群")]
    [InlineData(AuditCategory.Group, "分组")]
    [InlineData(AuditCategory.Configmap, "配置")]
    [InlineData(AuditCategory.Node, "节点")]
    [InlineData(AuditCategory.Workload, "工作负载")]
    public void Category_display_names_are_chinese(AuditCategory category, string expected)
    {
        Assert.Equal(expected, category.ToDisplayName());
    }

    [Theory]
    [InlineData(AuditAction.Login, "登录")]
    [InlineData(AuditAction.Logout, "登出")]
    [InlineData(AuditAction.Register, "注册")]
    [InlineData(AuditAction.Create, "创建")]
    [InlineData(AuditAction.Update, "修改")]
    [InlineData(AuditAction.Delete, "删除")]
    [InlineData(AuditAction.Move, "移动")]
    [InlineData(AuditAction.Rename, "重命名")]
    [InlineData(AuditAction.Scale, "扩缩容")]
    [InlineData(AuditAction.Restart, "重启")]
    public void Action_display_names_are_chinese(AuditAction action, string expected)
    {
        Assert.Equal(expected, action.ToDisplayName());
    }

    [Fact]
    public void ToAuditLogViewModel_maps_all_fields()
    {
        var log = new AuditLog
        {
            Id = 3,
            UserName = "admin",
            Category = AuditCategory.Cluster,
            Action = AuditAction.Delete,
            Target = "集群: c1",
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var vm = log.ToAuditLogViewModel();

        Assert.Equal(3, vm.Id);
        Assert.Equal("admin", vm.UserName);
        Assert.Equal("集群", vm.CategoryName);
        Assert.Equal("删除", vm.ActionName);
        Assert.Equal("集群: c1", vm.Target);
        Assert.Equal(log.CreatedAt, vm.CreatedAt);
    }
}

public class GroupMappingTests
{
    [Fact]
    public void ToViewModel_maps_fields_and_count()
    {
        var group = new ClusterGroup
        {
            Id = 2,
            Name = "prod",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Clusters = [new ClusterInfo(), new ClusterInfo()]
        };

        var vm = group.ToViewModel();

        Assert.Equal(2, vm.Id);
        Assert.Equal("prod", vm.Name);
        Assert.Equal(2, vm.ClusterCount);
    }

    [Fact]
    public void ToViewModel_null_clusters_yields_zero_count()
    {
        var vm = new ClusterGroup { Name = "g" }.ToViewModel();

        Assert.Equal(0, vm.ClusterCount);
    }
}

public class ConfigMapMappingTests
{
    [Fact]
    public void ToConfigMapListViewModel_maps_fields()
    {
        var cm = new k8s.Models.V1ConfigMap
        {
            Metadata = new k8s.Models.V1ObjectMeta
            {
                Name = "cm",
                NamespaceProperty = "app",
                Uid = "u-1",
                CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            Data = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" }
        };

        var vm = cm.ToConfigMapListViewModel();

        Assert.Equal("cm", vm.Name);
        Assert.Equal("app", vm.Namespace);
        Assert.Equal(2, vm.DataKeyCount);
        Assert.Equal("a, b", vm.DataKeyPreview);
    }

    [Fact]
    public void ToConfigMapListViewModel_long_data_keys_get_preview_suffix()
    {
        var cm = new k8s.Models.V1ConfigMap
        {
            Data = new Dictionary<string, string>
            {
                ["k1"] = "1", ["k2"] = "2", ["k3"] = "3", ["k4"] = "4"
            }
        };

        var vm = cm.ToConfigMapListViewModel();

        Assert.Equal(4, vm.DataKeyCount);
        Assert.EndsWith("...", vm.DataKeyPreview);
    }

    [Fact]
    public void ToConfigMapDetailViewModel_maps_data_and_yaml()
    {
        var cm = new k8s.Models.V1ConfigMap
        {
            Metadata = new k8s.Models.V1ObjectMeta { Name = "cm", NamespaceProperty = "app", Uid = "u" },
            Data = new Dictionary<string, string> { ["k"] = null! }
        };

        var detail = cm.ToConfigMapDetailViewModel();

        Assert.Equal("cm", detail.Name);
        Assert.Equal("u", detail.Uid);
        Assert.Equal("", detail.Data["k"]);
        Assert.Contains("cm", detail.Yaml);
    }
}
