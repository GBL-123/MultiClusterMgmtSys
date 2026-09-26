using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;

namespace MultiClusterMgmtSys.Application.Common.Helm;

/// <summary>
/// Helm 凭据物化:把集群连接凭据转换为 Helm 可消费的 kubeconfig 文本。
/// KubeConfig 型连接直写既有文本;Token 型连接合成最小 kubeconfig(server/token/insecure-skip-tls-verify)。
/// </summary>
public static class HelmKubeConfigBuilder
{
    /// <summary>按集群连接方式生成 kubeconfig 文本。</summary>
    /// <param name="cluster">集群实体(凭据字段须为当前值)。</param>
    /// <returns>kubeconfig YAML 文本;KubeConfig 型为原文本,Token 型为合成文本。</returns>
    public static string Build(ClusterInfo cluster)
    {
        return cluster.ConnectionType == ConnectionType.KubeConfig
            ? cluster.KubeConfig ?? ""
            : BuildTokenKubeConfig(cluster);
    }

    private static string BuildTokenKubeConfig(ClusterInfo cluster)
    {
        var server = Quote(cluster.ApiServer ?? "");
        var token = Quote(cluster.Token ?? "");
        var insecureSkipTlsVerify = cluster.SkipTlsVerify ? "true" : "false";
        return $"""
            apiVersion: v1
            kind: Config
            clusters:
            - name: cluster
              cluster:
                server: {server}
                insecure-skip-tls-verify: {insecureSkipTlsVerify}
            users:
            - name: user
              user:
                token: {token}
            contexts:
            - name: mcm
              context:
                cluster: cluster
                user: user
            current-context: mcm

            """;
    }

    private static string Quote(string value) =>
        "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n") + "\"";
}
