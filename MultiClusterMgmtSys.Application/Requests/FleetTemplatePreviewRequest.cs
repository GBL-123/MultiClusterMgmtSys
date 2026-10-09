namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>舰队模板预览请求:模板 YAML、目标集群集合与逐集群变量矩阵(仅 Admin 可调用)。</summary>
public record FleetTemplatePreviewRequest(
    /// <summary>含 <c>{{&#64;var}}</c> 占位符的模板 YAML 原文。</summary>
    string TemplateYaml,

    /// <summary>目标集群 Id 集合(至少一个)。</summary>
    IReadOnlyList<int> ClusterIds,

    /// <summary>逐集群变量矩阵:集群 Id 到「变量名 → 值」映射;键 SHALL 覆盖模板全部变量(值允许空串)。</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> Variables);
