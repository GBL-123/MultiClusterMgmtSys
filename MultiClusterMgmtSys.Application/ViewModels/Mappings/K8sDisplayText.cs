using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.ViewModels.Mappings;

/// <summary>
/// Kubernetes 字段/枚举值 → 中文展示文本与状态 CSS 类的纯函数映射(契约见 openspec/specs/display-conventions)。
/// 未登记的取值一律回退为原始文本,不做臆造翻译;应用自有状态(集群在线/离线、滚动三态)在此提供既有中文口径。
/// </summary>
public static class K8sDisplayText
{
    /// <summary>节点状态中文展示名;未知值回退原文。</summary>
    /// <param name="status">节点就绪状态原始值(Ready/NotReady/Unknown)。</param>
    public static string NodeStatusText(string status) => status switch
    {
        "Ready" => "就绪",
        "NotReady" => "未就绪",
        "Unknown" => "未知",
        _ => status
    };

    /// <summary>节点状态对应的状态徽章 CSS 类。</summary>
    /// <param name="status">节点就绪状态原始值。</param>
    public static string NodeStatusCssClass(string status) => status switch
    {
        "Ready" => "online",
        "NotReady" => "offline",
        _ => "unknown"
    };

    /// <summary>集群状态对应的状态徽章 CSS 类。</summary>
    /// <param name="status">集群状态枚举。</param>
    public static string ClusterStatusCssClass(ClusterStatus status) => status switch
    {
        ClusterStatus.Online => "online",
        ClusterStatus.Offline => "offline",
        _ => "unknown"
    };

    /// <summary>节点角色标签(逗号分隔)→ 中文展示名,以顿号连接;未知角色回退原文。</summary>
    /// <param name="roles">由 node-role.kubernetes.io/ 标签派生的逗号分隔角色串。</param>
    public static string NodeRoleText(string roles)
    {
        if (string.IsNullOrEmpty(roles))
        {
            return roles;
        }

        var parts = roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(role => role switch
            {
                "control-plane" => "控制平面",
                "worker" => "工作节点",
                "master" => "主节点",
                _ => role
            });
        return string.Join("、", parts);
    }

    /// <summary>节点生命周期阶段中文展示名;未知值回退原文。</summary>
    /// <param name="phase">节点阶段原始值。</param>
    public static string NodePhaseText(string phase) => phase switch
    {
        "Running" => "运行中",
        "Pending" => "等待中",
        "Terminated" => "已终止",
        _ => phase
    };

    /// <summary>节点地址类型中文展示名;未知值回退原文。</summary>
    /// <param name="type">地址类型原始值(InternalIP/ExternalIP/Hostname)。</param>
    public static string AddressTypeText(string type) => type switch
    {
        "InternalIP" => "内网 IP",
        "ExternalIP" => "外网 IP",
        "Hostname" => "主机名",
        _ => type
    };

    /// <summary>污点效果中文展示名;未知值回退原文。</summary>
    /// <param name="effect">污点效果原始值。</param>
    public static string TaintEffectText(string effect) => effect switch
    {
        "NoSchedule" => "禁止调度",
        "PreferNoSchedule" => "尽量不调度",
        "NoExecute" => "驱逐",
        _ => effect
    };

    /// <summary>节点条件类型中文展示名;未知值回退原文。</summary>
    /// <param name="type">条件类型原始值。</param>
    public static string ConditionTypeText(string type) => type switch
    {
        "Ready" => "就绪",
        "MemoryPressure" => "内存压力",
        "DiskPressure" => "磁盘压力",
        "PIDPressure" => "PID 压力",
        "NetworkUnavailable" => "网络不可用",
        _ => type
    };

    /// <summary>条件状态中文展示名(成立/不成立/未知);未知值回退原文。</summary>
    /// <param name="status">条件状态原始值(True/False/Unknown)。</param>
    public static string ConditionStatusText(string status) => status switch
    {
        "True" => "成立",
        "False" => "不成立",
        "Unknown" => "未知",
        _ => status
    };

    /// <summary>节点条件的健康态 CSS 类:Unknown→unknown;Ready 条件 True 为健康,其余条件 False 为健康。</summary>
    /// <param name="type">条件类型原始值。</param>
    /// <param name="status">条件状态原始值。</param>
    public static string ConditionStatusCssClass(string type, string status)
    {
        if (status == "Unknown")
        {
            return "unknown";
        }

        var isHealthy = type == "Ready" ? status == "True" : status == "False";
        return isHealthy ? "online" : "offline";
    }

    /// <summary>工作负载条件类型中文展示名;未知值回退原文。</summary>
    /// <param name="type">条件类型原始值。</param>
    public static string WorkloadConditionTypeText(string type) => type switch
    {
        "Available" => "可用",
        "Progressing" => "进行中",
        "ReplicaFailure" => "副本失败",
        _ => type
    };

    /// <summary>Service 类型中文展示名;未知值回退原文。</summary>
    /// <param name="type">Service 类型原始值。</param>
    public static string SvcTypeText(string type) => type switch
    {
        "ClusterIP" => "集群内 IP",
        "NodePort" => "节点端口",
        "LoadBalancer" => "负载均衡",
        "ExternalName" => "外部名称",
        _ => type
    };

    /// <summary>Endpoints 就绪状态中文展示名。</summary>
    /// <param name="ready">是否就绪。</param>
    public static string EndpointStatusText(bool ready) => ready ? "就绪" : "未就绪";

    /// <summary>Endpoints 就绪状态的英文原值。</summary>
    /// <param name="ready">是否就绪。</param>
    public static string EndpointStatusRaw(bool ready) => ready ? "Ready" : "NotReady";

    /// <summary>Endpoints 就绪状态对应的状态徽章 CSS 类。</summary>
    /// <param name="ready">是否就绪。</param>
    public static string EndpointStatusCssClass(bool ready) => ready ? "online" : "offline";

    /// <summary>账号角色中文展示名;未知值回退原文。</summary>
    /// <param name="role">角色名(Admin/Member)。</param>
    public static string AccountRoleText(string role) => role switch
    {
        "Admin" => "管理员",
        "Member" => "成员",
        _ => role
    };

    /// <summary>集群连接方式中文展示名;未知值回退枚举名。</summary>
    /// <param name="type">连接类型枚举。</param>
    public static string ConnectionTypeText(ConnectionType type) => type switch
    {
        ConnectionType.KubeConfig => "配置文件",
        ConnectionType.Token => "访问令牌",
        _ => type.ToString()
    };

    /// <summary>集群连接方式的英文展示原文(Kubeconfig/Token)。</summary>
    /// <param name="type">连接类型枚举。</param>
    public static string ConnectionTypeRaw(ConnectionType type) => type switch
    {
        ConnectionType.KubeConfig => "Kubeconfig",
        ConnectionType.Token => "Token",
        _ => type.ToString()
    };

    /// <summary>命名空间阶段中文展示名:Active→在线,其余(含 Terminating)归一为未知。</summary>
    /// <param name="phase">命名空间阶段原始值。</param>
    public static string NamespacePhaseText(string? phase)
        => phase == "Active" ? "在线" : "未知";

    /// <summary>命名空间阶段对应的状态徽章 CSS 类:Active→online,其余→unknown。</summary>
    /// <param name="phase">命名空间阶段原始值。</param>
    public static string NamespacePhaseCssClass(string? phase)
        => phase == "Active" ? "online" : "unknown";

    /// <summary>工作负载滚动三态中文展示名。</summary>
    /// <param name="state">滚动状态枚举。</param>
    public static string WorkloadRolloutText(WorkloadRolloutState state) => state switch
    {
        WorkloadRolloutState.Ready => "就绪",
        WorkloadRolloutState.Rolling => "滚动中",
        _ => "未就绪"
    };

    /// <summary>工作负载滚动三态对应的状态徽章 CSS 类。</summary>
    /// <param name="state">滚动状态枚举。</param>
    public static string WorkloadRolloutCssClass(WorkloadRolloutState state) => state switch
    {
        WorkloadRolloutState.Ready => "online",
        WorkloadRolloutState.Rolling => "unknown",
        _ => "offline"
    };

    /// <summary>节点数展示文本(带「台」单位)。</summary>
    /// <param name="count">节点数量。</param>
    public static string NodeCountText(int count) => $"{count} 台";

    /// <summary>事件类型中文展示名;未登记值回退原文。</summary>
    /// <param name="type">事件类型原始值(Normal/Warning)。</param>
    public static string EventTypeText(string type) => type switch
    {
        "Normal" => "正常",
        "Warning" => "警告",
        _ => type
    };

    /// <summary>事件类型对应的状态徽章 CSS 类。</summary>
    /// <param name="type">事件类型原始值(Normal/Warning)。</param>
    public static string EventTypeCssClass(string type) => type switch
    {
        "Normal" => "normal",
        "Warning" => "warning",
        _ => "unknown"
    };

    /// <summary>Pod phase 中文展示名;未登记值回退原文。</summary>
    /// <param name="phase">Pod phase 原始值(Running/Pending/Succeeded/Failed/Unknown)。</param>
    public static string PodPhaseText(string? phase) => phase switch
    {
        "Running" => "运行中",
        "Pending" => "等待中",
        "Succeeded" => "已完成",
        "Failed" => "失败",
        "Unknown" => "未知",
        _ => phase ?? ""
    };

    /// <summary>容器 waiting/terminated 原因中文展示名;未登记值回退原文(契约:未登记回退原文)。</summary>
    /// <param name="reason">容器状态原因原始值(CrashLoopBackOff/OOMKilled 等)。</param>
    public static string PodContainerReasonText(string reason) => reason switch
    {
        "CrashLoopBackOff" => "崩溃循环",
        "ImagePullBackOff" => "镜像拉取失败",
        "ErrImagePull" => "镜像拉取异常",
        "CreateContainerConfigError" => "容器配置错误",
        "CreateContainerError" => "容器创建失败",
        "RunContainerError" => "容器启动失败",
        "ContainerCannotRun" => "容器无法运行",
        "OOMKilled" => "内存不足被杀",
        "Evicted" => "已驱逐",
        "Error" => "运行错误",
        "DeadlineExceeded" => "超出时限",
        "Completed" => "已完成",
        "PodInitializing" => "初始化中",
        "ContainerCreating" => "创建中",
        _ => reason
    };

    /// <summary>Pod 列表状态徽章中文主行:容器异常信号优先于 phase(契约 pod-management:容器信号优先)。</summary>
    /// <param name="phase">Pod phase 原始值,可空。</param>
    /// <param name="containerReason">已过滤的容器异常信号(waiting/terminated 原因,正常等待原因不应传入);无信号传 null。</param>
    public static string PodStatusText(string? phase, string? containerReason)
        => string.IsNullOrWhiteSpace(containerReason) ? PodPhaseText(phase) : PodContainerReasonText(containerReason);

    /// <summary>Pod 列表状态徽章英文次行(raw):容器异常信号优先,否则 phase 原文。</summary>
    /// <param name="phase">Pod phase 原始值,可空。</param>
    /// <param name="containerReason">已过滤的容器异常信号;无信号传 null。</param>
    public static string PodStatusRaw(string? phase, string? containerReason)
        => string.IsNullOrWhiteSpace(containerReason) ? (phase ?? "") : containerReason;

    /// <summary>Pod 列表状态徽章 CSS 类:容器异常信号 offline;否则 Running online、Failed offline、其余 unknown。</summary>
    /// <param name="phase">Pod phase 原始值,可空。</param>
    /// <param name="containerReason">已过滤的容器异常信号;无信号传 null。</param>
    public static string PodStatusCssClass(string? phase, string? containerReason)
        => string.IsNullOrWhiteSpace(containerReason)
            ? phase switch
            {
                "Running" => "online",
                "Failed" => "offline",
                _ => "unknown"
            }
            : "offline";

    /// <summary>Pod QoS 类中文展示名;未登记值回退原文。</summary>
    /// <param name="qos">QoS 类原始值(Guaranteed/Burstable/BestEffort)。</param>
    public static string PodQosClassText(string qos) => qos switch
    {
        "Guaranteed" => "保证级",
        "Burstable" => "突发级",
        "BestEffort" => "尽力级",
        _ => qos
    };

    /// <summary>Pod 条件类型中文展示名;未登记值回退原文。</summary>
    /// <param name="type">条件类型原始值(Ready/Initialized/PodsScheduled/ContainersReady)。</param>
    public static string PodConditionTypeText(string type) => type switch
    {
        "Ready" => "就绪",
        "Initialized" => "已初始化",
        "PodScheduled" => "已调度",
        "ContainersReady" => "容器就绪",
        "DisruptionTarget" => "中断目标",
        _ => type
    };

    /// <summary>容器状态徽章英文次行(raw):异常原因优先,否则容器状态原文。</summary>
    /// <param name="state">容器状态原始值(Running/Waiting/Terminated)。</param>
    /// <param name="waitingReason">waiting 原始原因,可空。</param>
    /// <param name="terminatedReason">terminated 原始原因,可空。</param>
    public static string PodContainerStateRaw(string state, string? waitingReason, string? terminatedReason)
        => state switch
        {
            "Waiting" => string.IsNullOrWhiteSpace(waitingReason) ? "Waiting" : waitingReason,
            "Terminated" => string.IsNullOrWhiteSpace(terminatedReason) ? "Terminated" : terminatedReason,
            _ => state
        };

    /// <summary>容器状态徽章中文主行:waiting/terminated 时以原因中文为主文案;未登记值回退原文。</summary>
    /// <param name="state">容器状态原始值(Running/Waiting/Terminated)。</param>
    /// <param name="waitingReason">waiting 原始原因,可空。</param>
    /// <param name="terminatedReason">terminated 原始原因,可空。</param>
    public static string PodContainerStateText(string state, string? waitingReason, string? terminatedReason)
        => state switch
        {
            "Running" => "运行中",
            "Waiting" => string.IsNullOrWhiteSpace(waitingReason) ? "等待中" : PodContainerReasonText(waitingReason),
            "Terminated" => string.IsNullOrWhiteSpace(terminatedReason) ? "已终止" : PodContainerReasonText(terminatedReason),
            _ => state
        };

    /// <summary>容器状态徽章 CSS 类:Running online;中性原因(初始化/创建/已完成)unknown;其余异常原因 offline;无原因 unknown。</summary>
    /// <param name="state">容器状态原始值(Running/Waiting/Terminated)。</param>
    /// <param name="waitingReason">waiting 原始原因,可空。</param>
    /// <param name="terminatedReason">terminated 原始原因,可空。</param>
    public static string PodContainerStateCssClass(string state, string? waitingReason, string? terminatedReason)
    {
        if (state == "Running")
        {
            return "online";
        }

        var reason = state switch
        {
            "Waiting" => waitingReason,
            "Terminated" => terminatedReason,
            _ => null
        };

        return reason switch
        {
            null or "" => "unknown",
            "Completed" or "PodInitializing" or "ContainerCreating" => "unknown",
            _ => "offline"
        };
    }
}
