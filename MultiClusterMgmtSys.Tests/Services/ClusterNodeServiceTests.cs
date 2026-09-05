using k8s;
using k8s.Models;
using Moq;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class ClusterNodeServiceTests
{
    [Fact]
    public async Task UpdateNodeIpNotes_NoteOver64Chars_ThrowsValidation()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();
        var svc = TestServices.NodeService(db, TestServices.ThrowingFactory());

        var items = new List<NodeIpNoteEditItem>
        {
            new() { Address = "10.0.0.1", Note = new string('x', 65) }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.UpdateNodeIpNotesAsync(new NodeIpNotesUpdateRequest(1, "node-a", items)));

        Assert.Contains("64", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateNodeIpNotes_MissingCluster_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.NodeService(db, TestServices.ThrowingFactory());

        await Assert.ThrowsAsync<NotFoundException>(
            () => svc.UpdateNodeIpNotesAsync(new NodeIpNotesUpdateRequest(999, "node-a", [])));
    }

    [Fact]
    public async Task UpdateNodeIpNotes_MergesAddUpdateRemove()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        db.NodeIpRemarks.Add(new NodeIpRemark { ClusterId = 1, NodeName = "node-a", Address = "10.0.0.2", Note = "旧备注" });
        await db.SaveChangesAsync();
        var svc = TestServices.NodeService(db, TestServices.ThrowingFactory());

        var items = new List<NodeIpNoteEditItem>
        {
            new() { Address = "10.0.0.1", Note = "新增" },
            new() { Address = "10.0.0.2", Note = "更新" }
        };

        await svc.UpdateNodeIpNotesAsync(new NodeIpNotesUpdateRequest(1, "node-a", items));

        var remarks = db.NodeIpRemarks.Where(r => r.NodeName == "node-a").OrderBy(r => r.Address).ToList();
        Assert.Equal(2, remarks.Count);
        Assert.Equal("新增", remarks[0].Note);
        Assert.Equal("更新", remarks[1].Note);
        Assert.DoesNotContain(remarks, r => r.Address == "10.0.0.3");
    }

    [Fact]
    public async Task GetClusterNodes_K8s404_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.CoreV1.ListNodeWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KubernetesException(new V1Status { Code = 404, Message = "not found" }));

        var svc = TestServices.NodeService(db, _ => fake.Object);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => svc.GetClusterNodesAsync(1));

        Assert.Contains("加载节点列表", ex.UserMessage);
    }

    [Fact]
    public async Task GetClusterNodes_Timeout_ThrowsClusterUnreachable()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.CoreV1.ListNodeWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        var svc = TestServices.NodeService(db, _ => fake.Object);

        await Assert.ThrowsAsync<ClusterUnreachableException>(() => svc.GetClusterNodesAsync(1));
    }

    [Fact]
    public async Task GetNodeDetail_OfflineCluster_ReturnsUnreachableWithoutK8s()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c", status: ClusterStatus.Offline));
        await db.SaveChangesAsync();
        var svc = TestServices.NodeService(db, TestServices.ThrowingFactory());

        var result = await svc.GetNodeDetailAsync(new NodeDetailQueryRequest(1, "node-a"));

        Assert.NotNull(result);
        Assert.False(result.IsReachable);
    }
}