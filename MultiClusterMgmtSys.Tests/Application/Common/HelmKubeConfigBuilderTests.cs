using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class HelmKubeConfigBuilderTests
{
    [Fact]
    public void Build_returns_raw_kubeconfig_for_kubeconfig_connection()
    {
        var cluster = new ClusterInfo
        {
            ConnectionType = ConnectionType.KubeConfig,
            KubeConfig = "apiVersion: v1\nkind: Config\n"
        };

        Assert.Equal("apiVersion: v1\nkind: Config\n", HelmKubeConfigBuilder.Build(cluster));
    }

    [Fact]
    public void Build_synthesizes_kubeconfig_for_token_connection()
    {
        var cluster = new ClusterInfo
        {
            ConnectionType = ConnectionType.Token,
            ApiServer = "https://10.0.0.1:6443",
            Token = "abc.def",
            SkipTlsVerify = true
        };

        var result = HelmKubeConfigBuilder.Build(cluster);

        Assert.Contains("server: \"https://10.0.0.1:6443\"", result);
        Assert.Contains("token: \"abc.def\"", result);
        Assert.Contains("insecure-skip-tls-verify: true", result);
        Assert.Contains("current-context: mcm", result);
    }

    [Fact]
    public void Build_sets_skip_tls_false_when_disabled()
    {
        var cluster = new ClusterInfo
        {
            ConnectionType = ConnectionType.Token,
            ApiServer = "https://10.0.0.1:6443",
            Token = "t",
            SkipTlsVerify = false
        };

        Assert.Contains("insecure-skip-tls-verify: false", HelmKubeConfigBuilder.Build(cluster));
    }

    [Fact]
    public void Build_escapes_special_characters_in_values()
    {
        var cluster = new ClusterInfo
        {
            ConnectionType = ConnectionType.Token,
            ApiServer = "https://10.0.0.1:6443",
            Token = "a\"b\\c"
        };

        var result = HelmKubeConfigBuilder.Build(cluster);

        Assert.Contains("token: \"a\\\"b\\\\c\"", result);
    }
}
