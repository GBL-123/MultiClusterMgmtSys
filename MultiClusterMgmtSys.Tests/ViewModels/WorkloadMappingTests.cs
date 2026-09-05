using k8s.Models;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;
using Xunit;

namespace MultiClusterMgmtSys.Tests.ViewModels;

/// <summary>
/// 三态/就绪度算法(design D7):就绪 = ready==desired 且 updated==desired;
/// 滚动中 = updated&lt;desired 或 generation&gt;observedGeneration(或修订不一致);其余 = 未就绪。
/// </summary>
public class WorkloadMappingTests
{
    [Fact]
    public void Deployment_UpdatedLessThanDesired_IsRolling()
    {
        var dep = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "app", NamespaceProperty = "default", Generation = 2 },
            Spec = new V1DeploymentSpec { Replicas = 4 },
            Status = new V1DeploymentStatus { ReadyReplicas = 4, UpdatedReplicas = 2, ObservedGeneration = 2 }
        };

        var vm = dep.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Rolling, vm.RolloutState);
        Assert.Equal(4, vm.DesiredCount);
        Assert.Equal(4, vm.ReadyCount);
        Assert.Equal("4/4", vm.ReadyText);
        Assert.Equal(MultiClusterMgmtSys.Common.Enums.WorkloadKind.Deployment, vm.Kind);
    }

    [Fact]
    public void Deployment_GenerationAheadOfObserved_IsRolling()
    {
        var dep = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "app", NamespaceProperty = "default", Generation = 3 },
            Spec = new V1DeploymentSpec { Replicas = 2 },
            Status = new V1DeploymentStatus { ReadyReplicas = 2, UpdatedReplicas = 2, ObservedGeneration = 2 }
        };

        var vm = dep.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Rolling, vm.RolloutState);
    }

    [Fact]
    public void Deployment_AllSynced_IsReady()
    {
        var dep = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "app", NamespaceProperty = "default", Generation = 1 },
            Spec = new V1DeploymentSpec { Replicas = 3 },
            Status = new V1DeploymentStatus { ReadyReplicas = 3, UpdatedReplicas = 3, ObservedGeneration = 1 }
        };

        var vm = dep.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Ready, vm.RolloutState);
    }

    [Fact]
    public void Deployment_PartiallyReady_IsNotReady()
    {
        var dep = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "app", NamespaceProperty = "default", Generation = 1 },
            Spec = new V1DeploymentSpec { Replicas = 3 },
            Status = new V1DeploymentStatus { ReadyReplicas = 1, UpdatedReplicas = 3, ObservedGeneration = 1 }
        };

        var vm = dep.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.NotReady, vm.RolloutState);
        Assert.Equal("1/3", vm.ReadyText);
    }

    [Fact]
    public void StatefulSet_RevisionGap_IsRolling()
    {
        var sts = new V1StatefulSet
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "default", Generation = 1 },
            Spec = new V1StatefulSetSpec { Replicas = 3, ServiceName = "web-svc" },
            Status = new V1StatefulSetStatus
            {
                ReadyReplicas = 3,
                UpdatedReplicas = 3,
                CurrentRevision = "web-1",
                UpdateRevision = "web-2",
                ObservedGeneration = 1
            }
        };

        var vm = sts.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Rolling, vm.RolloutState);
    }

    [Fact]
    public void StatefulSet_AllSynced_IsReady()
    {
        var sts = new V1StatefulSet
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "default", Generation = 1 },
            Spec = new V1StatefulSetSpec { Replicas = 3, ServiceName = "web-svc" },
            Status = new V1StatefulSetStatus
            {
                ReadyReplicas = 3,
                UpdatedReplicas = 3,
                CurrentRevision = "web-2",
                UpdateRevision = "web-2",
                ObservedGeneration = 1
            }
        };

        var vm = sts.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Ready, vm.RolloutState);
        Assert.Equal(MultiClusterMgmtSys.Common.Enums.WorkloadKind.StatefulSet, vm.Kind);
    }

    [Fact]
    public void DaemonSet_UpdatedLessThanScheduled_IsRolling()
    {
        var ds = new V1DaemonSet
        {
            Metadata = new V1ObjectMeta { Name = "agent", NamespaceProperty = "kube-system" },
            Status = new V1DaemonSetStatus
            {
                DesiredNumberScheduled = 3,
                NumberReady = 3,
                UpdatedNumberScheduled = 1
            }
        };

        var vm = ds.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Rolling, vm.RolloutState);
        Assert.Equal(3, vm.DesiredCount);
        Assert.Equal(3, vm.ReadyCount);
    }

    [Fact]
    public void DaemonSet_AllReady_IsReady()
    {
        var ds = new V1DaemonSet
        {
            Metadata = new V1ObjectMeta { Name = "agent", NamespaceProperty = "kube-system" },
            Status = new V1DaemonSetStatus
            {
                DesiredNumberScheduled = 3,
                NumberReady = 3,
                UpdatedNumberScheduled = 3
            }
        };

        var vm = ds.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Ready, vm.RolloutState);
        Assert.Equal(3, vm.DesiredCount);
        Assert.Equal(MultiClusterMgmtSys.Common.Enums.WorkloadKind.DaemonSet, vm.Kind);
    }

    [Fact]
    public void ReplicaSet_PartiallyReady_IsNotReady()
    {
        var rs = new V1ReplicaSet
        {
            Metadata = new V1ObjectMeta { Name = "app-rs", NamespaceProperty = "default", Generation = 1 },
            Spec = new V1ReplicaSetSpec { Replicas = 2 },
            Status = new V1ReplicaSetStatus { ReadyReplicas = 1, ObservedGeneration = 1 }
        };

        var vm = rs.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.NotReady, vm.RolloutState);
        Assert.Equal(MultiClusterMgmtSys.Common.Enums.WorkloadKind.ReplicaSet, vm.Kind);
    }

    [Fact]
    public void ReplicaSet_GenerationAhead_IsRolling()
    {
        var rs = new V1ReplicaSet
        {
            Metadata = new V1ObjectMeta { Name = "app-rs", NamespaceProperty = "default", Generation = 5 },
            Spec = new V1ReplicaSetSpec { Replicas = 2 },
            Status = new V1ReplicaSetStatus { ReadyReplicas = 2, ObservedGeneration = 4 }
        };

        var vm = rs.ToWorkloadListViewModel();

        Assert.Equal(WorkloadRolloutState.Rolling, vm.RolloutState);
    }

    [Fact]
    public void DeploymentDetail_MapsSummaryAndConditionsAndYaml()
    {
        var dep = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = "app",
                NamespaceProperty = "default",
                Uid = "uid-1",
                Generation = 1,
                CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = 2,
                Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { ["app"] = "my-app" } }
            },
            Status = new V1DeploymentStatus
            {
                ReadyReplicas = 2,
                UpdatedReplicas = 2,
                ObservedGeneration = 1,
                Conditions =
                [
                    new V1DeploymentCondition
                    {
                        Type = "Available",
                        Status = "True",
                        Reason = "MinimumReplicasAvailable",
                        Message = "Deployment has minimum availability.",
                        LastTransitionTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
                    }
                ]
            }
        };

        var vm = dep.ToWorkloadDetailViewModel();

        Assert.Equal("app", vm.Name);
        Assert.Equal("default", vm.Namespace);
        Assert.Equal("uid-1", vm.Uid);
        Assert.Equal("app=my-app", vm.Selector);
        Assert.Equal(2, vm.UpdatedCount);
        Assert.Single(vm.Conditions);
        Assert.Equal("Available", vm.Conditions[0].Type);
        Assert.Equal("True", vm.Conditions[0].Status);
        Assert.NotEmpty(vm.Yaml);
        Assert.Equal(WorkloadRolloutState.Ready, vm.RolloutState);
    }
}
