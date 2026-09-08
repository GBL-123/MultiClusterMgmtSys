using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Tests.ViewModels;

public class WorkloadMappingTests
{
    private static V1Deployment NewDeployment(int? specReplicas, int ready, int updated, int generation = 1, int observed = 1)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app", Generation = generation },
            Spec = new V1DeploymentSpec { Replicas = specReplicas },
            Status = new V1DeploymentStatus
            {
                ReadyReplicas = ready,
                UpdatedReplicas = updated,
                ObservedGeneration = generation
            }
        };

    [Fact]
    public void Deployment_complete_rollout_is_ready()
    {
        var vm = NewDeployment(3, ready: 3, updated: 3).ToWorkloadListViewModel();

        Assert.Equal(WorkloadKind.Deployment, vm.Kind);
        Assert.Equal(3, vm.DesiredCount);
        Assert.Equal(3, vm.ReadyCount);
        Assert.Equal(WorkloadRolloutState.Ready, vm.RolloutState);
    }

    [Fact]
    public void Deployment_updated_less_than_desired_is_rolling()
    {
        var vm = NewDeployment(3, ready: 3, updated: 1).ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Rolling, vm.RolloutState);
    }

    [Fact]
    public void Deployment_generation_ahead_of_observed_is_rolling()
    {
        var dep = NewDeployment(3, ready: 3, updated: 3, generation: 5);
        dep.Status.ObservedGeneration = 1;

        var vm = dep.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Rolling, vm.RolloutState);
    }

    [Fact]
    public void Deployment_not_ready_yields_not_ready_state()
    {
        var vm = NewDeployment(3, ready: 1, updated: 3).ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.NotReady, vm.RolloutState);
    }

    [Fact]
    public void Deployment_null_spec_replicas_counts_as_zero()
    {
        var dep = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "web" },
            Status = new V1DeploymentStatus { ReadyReplicas = 0, UpdatedReplicas = 0, ObservedGeneration = 1 }
        };

        var vm = dep.ToWorkloadListViewModel();

        Assert.Equal(0, vm.DesiredCount);
        Assert.Equal(WorkloadRolloutState.Ready, vm.RolloutState);
    }

    [Fact]
    public void StatefulSet_revision_gap_is_rolling()
    {
        var sts = new V1StatefulSet
        {
            Metadata = new V1ObjectMeta { Name = "sts", NamespaceProperty = "app" },
            Spec = new V1StatefulSetSpec { Replicas = 2 },
            Status = new V1StatefulSetStatus
            {
                ReadyReplicas = 2,
                UpdatedReplicas = 2,
                CurrentRevision = "rev-1",
                UpdateRevision = "rev-2"
            }
        };

        var vm = sts.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Rolling, vm.RolloutState);
    }

    [Fact]
    public void StatefulSet_matched_revisions_is_ready()
    {
        var sts = new V1StatefulSet
        {
            Metadata = new V1ObjectMeta { Name = "sts", NamespaceProperty = "app" },
            Spec = new V1StatefulSetSpec { Replicas = 2 },
            Status = new V1StatefulSetStatus
            {
                ReadyReplicas = 2,
                UpdatedReplicas = 2,
                CurrentRevision = "rev-1",
                UpdateRevision = "rev-1"
            }
        };

        var vm = sts.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Ready, vm.RolloutState);
    }

    [Fact]
    public void DaemonSet_uses_scheduled_counts()
    {
        var ds = new V1DaemonSet
        {
            Metadata = new V1ObjectMeta { Name = "ds", NamespaceProperty = "app" },
            Status = new V1DaemonSetStatus
            {
                DesiredNumberScheduled = 4,
                NumberReady = 4,
                UpdatedNumberScheduled = 4
            }
        };

        var vm = ds.ToWorkloadListViewModel();

        Assert.Equal(WorkloadKind.DaemonSet, vm.Kind);
        Assert.Equal(4, vm.DesiredCount);
        Assert.Equal(4, vm.ReadyCount);
        Assert.Equal(WorkloadRolloutState.Ready, vm.RolloutState);
    }

    [Fact]
    public void DaemonSet_partial_update_is_rolling()
    {
        var ds = new V1DaemonSet
        {
            Status = new V1DaemonSetStatus
            {
                DesiredNumberScheduled = 4,
                NumberReady = 4,
                UpdatedNumberScheduled = 2
            }
        };

        var vm = ds.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Rolling, vm.RolloutState);
    }

    [Fact]
    public void ReplicaSet_uses_generation_only()
    {
        var rs = new V1ReplicaSet
        {
            Metadata = new V1ObjectMeta { Name = "rs", Generation = 3 },
            Spec = new V1ReplicaSetSpec { Replicas = 2 },
            Status = new V1ReplicaSetStatus { ReadyReplicas = 2, ObservedGeneration = 3 }
        };

        var vm = rs.ToWorkloadListViewModel();

        Assert.Equal(WorkloadKind.ReplicaSet, vm.Kind);
        Assert.Equal(WorkloadRolloutState.Ready, vm.RolloutState);
    }

    [Fact]
    public void ReplicaSet_stale_generation_is_rolling()
    {
        var rs = new V1ReplicaSet
        {
            Metadata = new V1ObjectMeta { Name = "rs", Generation = 9 },
            Spec = new V1ReplicaSetSpec { Replicas = 2 },
            Status = new V1ReplicaSetStatus { ReadyReplicas = 2, ObservedGeneration = 3 }
        };

        Assert.Equal(WorkloadRolloutState.Rolling, rs.ToWorkloadListViewModel().RolloutState);
    }

    [Fact]
    public void Deployment_detail_maps_selector_conditions_and_yaml()
    {
        var dep = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app", Uid = "uid-9" },
            Spec = new V1DeploymentSpec
            {
                Replicas = 2,
                Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { ["app"] = "web" } }
            },
            Status = new V1DeploymentStatus
            {
                ReadyReplicas = 2,
                UpdatedReplicas = 2,
                ObservedGeneration = 1,
                Conditions = [new V1DeploymentCondition { Type = "Available", Status = "True", Reason = "ok" }]
            }
        };

        var detail = dep.ToWorkloadDetailViewModel();

        Assert.Equal("uid-9", detail.Uid);
        Assert.Equal("app=web", detail.Selector);
        Assert.Equal(WorkloadRolloutState.Ready, detail.RolloutState);
        Assert.Equal(2, detail.UpdatedCount);
        Assert.Equal("Available", detail.Conditions.Single().Type);
        Assert.Contains("web", detail.Yaml);
    }

    [Fact]
    public void Detail_yaml_is_valid_roundtrip()
    {
        var dep = NewDeployment(3, 3, 3);
        dep.Metadata.Uid = "uid-1";

        var detail = dep.ToWorkloadDetailViewModel();
        var reparsed = KubernetesYaml.Deserialize<V1Deployment>(detail.Yaml);

        Assert.Equal("web", reparsed!.Metadata!.Name);
    }

    [Theory]
    [InlineData(WorkloadKind.Deployment, true, true)]
    [InlineData(WorkloadKind.StatefulSet, true, true)]
    [InlineData(WorkloadKind.DaemonSet, false, true)]
    [InlineData(WorkloadKind.ReplicaSet, true, false)]
    public void Kind_capabilities_match_matrix(WorkloadKind kind, bool supportsScale, bool supportsRestart)
    {
        Assert.Equal(supportsScale, kind.SupportsScale());
        Assert.Equal(supportsRestart, kind.SupportsRestart());
    }

    [Theory]
    [InlineData(WorkloadKind.Deployment, "部署")]
    [InlineData(WorkloadKind.StatefulSet, "有状态应用")]
    [InlineData(WorkloadKind.DaemonSet, "守护进程")]
    [InlineData(WorkloadKind.ReplicaSet, "副本集")]
    public void Kind_display_names_are_chinese(WorkloadKind kind, string expected)
    {
        Assert.Equal(expected, kind.ToDisplayText());
    }

    [Theory]
    [InlineData(WorkloadKind.Deployment, "deployments")]
    [InlineData(WorkloadKind.StatefulSet, "statefulsets")]
    [InlineData(WorkloadKind.DaemonSet, "daemonsets")]
    [InlineData(WorkloadKind.ReplicaSet, "replicasets")]
    public void Kind_route_segments(WorkloadKind kind, string expected)
    {
        Assert.Equal(expected, kind.ToRouteSegment());
    }
}
