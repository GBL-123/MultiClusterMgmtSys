using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

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
}
