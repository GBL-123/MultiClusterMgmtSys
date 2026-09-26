using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class ChartPackageReaderTests
{
    private const string ChartYaml = """
        apiVersion: v2
        name: nginx
        version: 1.2.3
        appVersion: "1.25.0"
        description: A basic nginx chart
        """;

    [Fact]
    public void Read_returns_metadata_values_and_no_warnings()
    {
        var package = CreatePackage(
            ("nginx/Chart.yaml", ChartYaml),
            ("nginx/values.yaml", "replicaCount: 2\n"),
            ("nginx/templates/deployment.yaml", "kind: Deployment\n"));

        var info = ChartPackageReader.Read(package);

        Assert.Equal("nginx", info.Name);
        Assert.Equal("1.2.3", info.Version);
        Assert.Equal("1.25.0", info.AppVersion);
        Assert.Equal("A basic nginx chart", info.Description);
        Assert.Empty(info.Dependencies);
        Assert.Equal("replicaCount: 2\n", info.ValuesYaml);
        Assert.Empty(info.Warnings);
    }

    [Fact]
    public void Read_warns_when_dependencies_declared_without_charts_folder()
    {
        var package = CreatePackage(("nginx/Chart.yaml", ChartYaml + "\ndependencies:\n  - name: common\n    version: \"2.x.x\"\n    repository: https://charts.example.com\n"));

        var info = ChartPackageReader.Read(package);

        Assert.Equal(["common"], info.Dependencies);
        Assert.Single(info.Warnings);
        Assert.Contains("charts/", info.Warnings[0]);
    }

    [Fact]
    public void Read_suppresses_warning_when_dependency_packaged()
    {
        var package = CreatePackage(
            ("nginx/Chart.yaml", ChartYaml + "\ndependencies:\n  - name: common\n    version: \"2.x.x\"\n    repository: https://charts.example.com\n"),
            ("nginx/charts/common/Chart.yaml", "apiVersion: v2\nname: common\nversion: 2.0.0\n"));

        var info = ChartPackageReader.Read(package);

        Assert.Equal(["common"], info.Dependencies);
        Assert.Empty(info.Warnings);
    }

    [Fact]
    public void Read_throws_when_chart_yaml_missing()
    {
        var package = CreatePackage(("nginx/values.yaml", "replicaCount: 2\n"));

        var exception = Assert.Throws<ValidationException>(() => ChartPackageReader.Read(package));

        Assert.Contains("Chart.yaml", exception.UserMessage);
    }

    [Fact]
    public void Read_throws_when_name_or_version_missing()
    {
        var package = CreatePackage(("broken/Chart.yaml", "apiVersion: v2\nname: broken\n"));

        var exception = Assert.Throws<ValidationException>(() => ChartPackageReader.Read(package));

        Assert.Contains("名称或版本", exception.UserMessage);
    }

    [Fact]
    public void Read_throws_on_invalid_archive()
    {
        var exception = Assert.Throws<ValidationException>(() => ChartPackageReader.Read([1, 2, 3]));

        Assert.Contains("无法读取", exception.UserMessage);
    }

    [Fact]
    public void Read_throws_on_empty_content()
    {
        Assert.Throws<ValidationException>(() => ChartPackageReader.Read([]));
    }

    private static byte[] CreatePackage(params (string Name, string Content)[] files)
    {
        using var stream = new MemoryStream();
        using (var gzip = new GZipStream(stream, CompressionLevel.SmallestSize, leaveOpen: true))
        using (var tar = new TarWriter(gzip, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content))
                };
                tar.WriteEntry(entry);
            }
        }
        return stream.ToArray();
    }
}
