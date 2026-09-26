using MultiClusterMgmtSys.Application.Common.Helm;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class HelmCommandBuilderTests
{
    [Fact]
    public void BuildList_uses_all_namespaces_json_and_kubeconfig()
    {
        Assert.Equal(
            ["list", "-A", "-o", "json", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildList());
    }

    [Fact]
    public void BuildStatus_uses_release_namespace_json()
    {
        Assert.Equal(
            ["status", "nginx", "-n", "web", "-o", "json", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildStatus("nginx", "web"));
    }

    [Fact]
    public void BuildHistory_uses_release_namespace_json()
    {
        Assert.Equal(
            ["history", "nginx", "-n", "web", "-o", "json", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildHistory("nginx", "web"));
    }

    [Fact]
    public void BuildGetValues_uses_yaml_output()
    {
        Assert.Equal(
            ["get", "values", "nginx", "-n", "web", "-o", "yaml", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildGetValues("nginx", "web"));
    }

    [Fact]
    public void BuildGetManifest_uses_default_output()
    {
        Assert.Equal(
            ["get", "manifest", "nginx", "-n", "web", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildGetManifest("nginx", "web"));
    }

    [Fact]
    public void BuildInstall_includes_values_namespace_and_wait_flags()
    {
        Assert.Equal(
            ["install", "nginx", "{chart}", "-n", "web", "-f", "{values}", "--create-namespace", "--wait", "--timeout", "300s", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildInstall("nginx", "web", includeValues: true, createNamespace: true, wait: true, timeoutSeconds: 300));
    }

    [Fact]
    public void BuildInstall_omits_optional_flags()
    {
        Assert.Equal(
            ["install", "nginx", "{chart}", "-n", "web", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildInstall("nginx", "web", includeValues: false, createNamespace: false, wait: false, timeoutSeconds: 300));
    }

    [Fact]
    public void BuildUpgrade_supports_reuse_values_mode()
    {
        Assert.Equal(
            ["upgrade", "nginx", "{chart}", "-n", "web", "--reuse-values", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildUpgrade("nginx", "web", includeValues: false, reuseValues: true, wait: false, timeoutSeconds: 300));
    }

    [Fact]
    public void BuildUpgrade_supports_edited_values_mode()
    {
        Assert.Equal(
            ["upgrade", "nginx", "{chart}", "-n", "web", "-f", "{values}", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildUpgrade("nginx", "web", includeValues: true, reuseValues: false, wait: false, timeoutSeconds: 300));
    }

    [Fact]
    public void BuildRollback_uses_revision()
    {
        Assert.Equal(
            ["rollback", "nginx", "2", "-n", "web", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildRollback("nginx", "web", 2));
    }

    [Fact]
    public void BuildUninstall_supports_keep_history()
    {
        Assert.Equal(
            ["uninstall", "nginx", "-n", "web", "--keep-history", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildUninstall("nginx", "web", keepHistory: true));
        Assert.Equal(
            ["uninstall", "nginx", "-n", "web", "--kubeconfig", "{kubeconfig}"],
            HelmCommandBuilder.BuildUninstall("nginx", "web", keepHistory: false));
    }

    [Theory]
    [InlineData("nginx")]
    [InlineData("my-app-1")]
    [InlineData("a")]
    public void IsValidReleaseName_accepts_dns1123_names(string name)
    {
        Assert.True(HelmCommandBuilder.IsValidReleaseName(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Nginx")]
    [InlineData("-nginx")]
    [InlineData("nginx-")]
    [InlineData("ng inx")]
    [InlineData("nginx_1")]
    public void IsValidReleaseName_rejects_invalid_names(string name)
    {
        Assert.False(HelmCommandBuilder.IsValidReleaseName(name));
    }

    [Fact]
    public void IsValidReleaseName_rejects_too_long_names()
    {
        Assert.False(HelmCommandBuilder.IsValidReleaseName(new string('a', 54)));
    }

    [Fact]
    public void IsValidNamespace_accepts_and_rejects()
    {
        Assert.True(HelmCommandBuilder.IsValidNamespace("web"));
        Assert.False(HelmCommandBuilder.IsValidNamespace("Web"));
        Assert.False(HelmCommandBuilder.IsValidNamespace(new string('a', 64)));
    }
}
