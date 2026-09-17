using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Tests.Application.ViewModels;

public class K8sDisplayTextTests
{
    [Theory]
    [InlineData("Ready", "就绪")]
    [InlineData("NotReady", "未就绪")]
    [InlineData("Unknown", "未知")]
    [InlineData("Odd", "Odd")]
    public void NodeStatusText_maps_known_values_and_falls_back(string status, string expected)
        => Assert.Equal(expected, K8sDisplayText.NodeStatusText(status));

    [Theory]
    [InlineData("Ready", "online")]
    [InlineData("NotReady", "offline")]
    [InlineData("Unknown", "unknown")]
    [InlineData("Odd", "unknown")]
    public void NodeStatusCssClass_maps_badge_classes(string status, string expected)
        => Assert.Equal(expected, K8sDisplayText.NodeStatusCssClass(status));

    [Theory]
    [InlineData(ClusterStatus.Online, "online")]
    [InlineData(ClusterStatus.Offline, "offline")]
    [InlineData(ClusterStatus.Unknown, "unknown")]
    public void ClusterStatusCssClass_maps_badge_classes(ClusterStatus status, string expected)
        => Assert.Equal(expected, K8sDisplayText.ClusterStatusCssClass(status));

    [Fact]
    public void NodeRoleText_joins_multiple_roles_with_enumeration_comma()
        => Assert.Equal("控制平面、工作节点", K8sDisplayText.NodeRoleText("control-plane,worker"));

    [Fact]
    public void NodeRoleText_keeps_unknown_roles_raw()
        => Assert.Equal("控制平面、custom-role", K8sDisplayText.NodeRoleText("control-plane,custom-role"));

    [Fact]
    public void NodeRoleText_empty_stays_empty()
        => Assert.Equal("", K8sDisplayText.NodeRoleText(""));

    [Theory]
    [InlineData("Running", "运行中")]
    [InlineData("Pending", "等待中")]
    [InlineData("Terminated", "已终止")]
    [InlineData("Weird", "Weird")]
    public void NodePhaseText_maps_known_values_and_falls_back(string phase, string expected)
        => Assert.Equal(expected, K8sDisplayText.NodePhaseText(phase));

    [Theory]
    [InlineData("InternalIP", "内网 IP")]
    [InlineData("ExternalIP", "外网 IP")]
    [InlineData("Hostname", "主机名")]
    [InlineData("Other", "Other")]
    public void AddressTypeText_maps_known_values_and_falls_back(string type, string expected)
        => Assert.Equal(expected, K8sDisplayText.AddressTypeText(type));

    [Theory]
    [InlineData("NoSchedule", "禁止调度")]
    [InlineData("PreferNoSchedule", "尽量不调度")]
    [InlineData("NoExecute", "驱逐")]
    [InlineData("Other", "Other")]
    public void TaintEffectText_maps_known_values_and_falls_back(string effect, string expected)
        => Assert.Equal(expected, K8sDisplayText.TaintEffectText(effect));

    [Theory]
    [InlineData("Ready", "就绪")]
    [InlineData("MemoryPressure", "内存压力")]
    [InlineData("DiskPressure", "磁盘压力")]
    [InlineData("PIDPressure", "PID 压力")]
    [InlineData("NetworkUnavailable", "网络不可用")]
    [InlineData("CustomCondition", "CustomCondition")]
    public void ConditionTypeText_maps_known_values_and_falls_back(string type, string expected)
        => Assert.Equal(expected, K8sDisplayText.ConditionTypeText(type));

    [Theory]
    [InlineData("True", "成立")]
    [InlineData("False", "不成立")]
    [InlineData("Unknown", "未知")]
    [InlineData("Other", "Other")]
    public void ConditionStatusText_maps_known_values_and_falls_back(string status, string expected)
        => Assert.Equal(expected, K8sDisplayText.ConditionStatusText(status));

    [Theory]
    [InlineData("Ready", "True", "online")]
    [InlineData("Ready", "False", "offline")]
    [InlineData("MemoryPressure", "False", "online")]
    [InlineData("MemoryPressure", "True", "offline")]
    [InlineData("Ready", "Unknown", "unknown")]
    public void ConditionStatusCssClass_keeps_healthy_semantics(string type, string status, string expected)
        => Assert.Equal(expected, K8sDisplayText.ConditionStatusCssClass(type, status));

    [Theory]
    [InlineData("Available", "可用")]
    [InlineData("Progressing", "进行中")]
    [InlineData("ReplicaFailure", "副本失败")]
    [InlineData("Other", "Other")]
    public void WorkloadConditionTypeText_maps_known_values_and_falls_back(string type, string expected)
        => Assert.Equal(expected, K8sDisplayText.WorkloadConditionTypeText(type));

    [Theory]
    [InlineData("ClusterIP", "集群内 IP")]
    [InlineData("NodePort", "节点端口")]
    [InlineData("LoadBalancer", "负载均衡")]
    [InlineData("ExternalName", "外部名称")]
    [InlineData("Other", "Other")]
    public void SvcTypeText_maps_known_values_and_falls_back(string type, string expected)
        => Assert.Equal(expected, K8sDisplayText.SvcTypeText(type));

    [Fact]
    public void Endpoint_status_helpers_cover_both_states()
    {
        Assert.Equal("就绪", K8sDisplayText.EndpointStatusText(true));
        Assert.Equal("未就绪", K8sDisplayText.EndpointStatusText(false));
        Assert.Equal("Ready", K8sDisplayText.EndpointStatusRaw(true));
        Assert.Equal("NotReady", K8sDisplayText.EndpointStatusRaw(false));
        Assert.Equal("online", K8sDisplayText.EndpointStatusCssClass(true));
        Assert.Equal("offline", K8sDisplayText.EndpointStatusCssClass(false));
    }

    [Theory]
    [InlineData("Admin", "管理员")]
    [InlineData("Member", "成员")]
    [InlineData("Other", "Other")]
    public void AccountRoleText_maps_known_values_and_falls_back(string role, string expected)
        => Assert.Equal(expected, K8sDisplayText.AccountRoleText(role));

    [Theory]
    [InlineData(ConnectionType.KubeConfig, "配置文件")]
    [InlineData(ConnectionType.Token, "访问令牌")]
    public void ConnectionTypeText_maps_known_values(ConnectionType type, string expected)
        => Assert.Equal(expected, K8sDisplayText.ConnectionTypeText(type));

    [Theory]
    [InlineData(ConnectionType.KubeConfig, "Kubeconfig")]
    [InlineData(ConnectionType.Token, "Token")]
    public void ConnectionTypeRaw_keeps_english_names(ConnectionType type, string expected)
        => Assert.Equal(expected, K8sDisplayText.ConnectionTypeRaw(type));

    [Theory]
    [InlineData("Active", "在线", "online")]
    [InlineData("Terminating", "未知", "unknown")]
    [InlineData(null, "未知", "unknown")]
    public void Namespace_phase_helpers_normalize(string? phase, string expectedText, string expectedClass)
    {
        Assert.Equal(expectedText, K8sDisplayText.NamespacePhaseText(phase));
        Assert.Equal(expectedClass, K8sDisplayText.NamespacePhaseCssClass(phase));
    }

    [Theory]
    [InlineData(WorkloadRolloutState.Ready, "就绪", "online")]
    [InlineData(WorkloadRolloutState.Rolling, "滚动中", "unknown")]
    [InlineData(WorkloadRolloutState.NotReady, "未就绪", "offline")]
    public void Workload_rollout_helpers_map_states(WorkloadRolloutState state, string expectedText, string expectedClass)
    {
        Assert.Equal(expectedText, K8sDisplayText.WorkloadRolloutText(state));
        Assert.Equal(expectedClass, K8sDisplayText.WorkloadRolloutCssClass(state));
    }

    [Fact]
    public void NodeCountText_appends_unit()
        => Assert.Equal("3 台", K8sDisplayText.NodeCountText(3));

    [Theory]
    [InlineData("Running", "运行中", "online")]
    [InlineData("Pending", "等待中", "unknown")]
    [InlineData("Succeeded", "已完成", "unknown")]
    [InlineData("Failed", "失败", "offline")]
    [InlineData("Unknown", "未知", "unknown")]
    [InlineData("Weird", "Weird", "unknown")]
    [InlineData(null, "", "unknown")]
    public void Pod_phase_helpers_map_known_values_and_fallback(string? phase, string expectedText, string expectedClass)
    {
        Assert.Equal(expectedText, K8sDisplayText.PodStatusText(phase, null));
        Assert.Equal(phase ?? "", K8sDisplayText.PodStatusRaw(phase, null));
        Assert.Equal(expectedClass, K8sDisplayText.PodStatusCssClass(phase, null));
    }

    [Fact]
    public void Pod_running_with_crash_loop_backoff_shows_reason_and_offline()
    {
        Assert.Equal("崩溃循环", K8sDisplayText.PodStatusText("Running", "CrashLoopBackOff"));
        Assert.Equal("CrashLoopBackOff", K8sDisplayText.PodStatusRaw("Running", "CrashLoopBackOff"));
        Assert.Equal("offline", K8sDisplayText.PodStatusCssClass("Running", "CrashLoopBackOff"));
    }

    [Theory]
    [InlineData("OOMKilled", "内存不足被杀")]
    [InlineData("ImagePullBackOff", "镜像拉取失败")]
    [InlineData("Evicted", "已驱逐")]
    public void Pod_container_reason_maps_known_values(string reason, string expected)
        => Assert.Equal(expected, K8sDisplayText.PodContainerReasonText(reason));

    [Fact]
    public void Pod_container_reason_unregistered_falls_back_to_raw()
        => Assert.Equal("SomeNewReason", K8sDisplayText.PodStatusText("Running", "SomeNewReason"));

    [Theory]
    [InlineData("Running", null, null, "Running", "运行中", "online")]
    [InlineData("Waiting", "CrashLoopBackOff", null, "CrashLoopBackOff", "崩溃循环", "offline")]
    [InlineData("Waiting", "PodInitializing", null, "PodInitializing", "初始化中", "unknown")]
    [InlineData("Terminated", null, "Completed", "Completed", "已完成", "unknown")]
    [InlineData("Terminated", null, "OOMKilled", "OOMKilled", "内存不足被杀", "offline")]
    [InlineData("Mystery", null, null, "Mystery", "Mystery", "unknown")]
    public void Pod_container_state_helpers_resolve_reason_priority(
        string state,
        string? waitingReason,
        string? terminatedReason,
        string expectedRaw,
        string expectedText,
        string expectedClass)
    {
        Assert.Equal(expectedRaw, K8sDisplayText.PodContainerStateRaw(state, waitingReason, terminatedReason));
        Assert.Equal(expectedText, K8sDisplayText.PodContainerStateText(state, waitingReason, terminatedReason));
        Assert.Equal(expectedClass, K8sDisplayText.PodContainerStateCssClass(state, waitingReason, terminatedReason));
    }

    [Theory]
    [InlineData("Guaranteed", "保证级")]
    [InlineData("Burstable", "突发级")]
    [InlineData("BestEffort", "尽力级")]
    [InlineData("Custom", "Custom")]
    public void Pod_qos_class_maps_known_values(string qos, string expected)
        => Assert.Equal(expected, K8sDisplayText.PodQosClassText(qos));

    [Theory]
    [InlineData("Ready", "就绪")]
    [InlineData("Initialized", "已初始化")]
    [InlineData("PodScheduled", "已调度")]
    [InlineData("ContainersReady", "容器就绪")]
    [InlineData("New", "New")]
    public void Pod_condition_type_maps_known_values(string type, string expected)
        => Assert.Equal(expected, K8sDisplayText.PodConditionTypeText(type));
}
