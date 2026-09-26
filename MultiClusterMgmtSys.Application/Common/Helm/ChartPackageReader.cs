using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using MultiClusterMgmtSys.Domain.Exceptions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MultiClusterMgmtSys.Application.Common.Helm;

/// <summary>chart 包(.tgz)的解析结果。</summary>
/// <param name="Name">chart 名称(Chart.yaml 的 name)。</param>
/// <param name="Version">chart 版本(Chart.yaml 的 version)。</param>
/// <param name="AppVersion">chart 的 appVersion,可为空字符串。</param>
/// <param name="Description">chart 描述,可为空字符串。</param>
/// <param name="Dependencies">Chart.yaml 声明的依赖名称列表。</param>
/// <param name="ValuesYaml">包内 values.yaml 文本;缺省时为空字符串。</param>
/// <param name="Warnings">解析过程产生的警告(如声明依赖但未打包 charts/),可为空列表。</param>
public sealed record ChartPackageInfo(
    string Name,
    string Version,
    string AppVersion,
    string Description,
    IReadOnlyList<string> Dependencies,
    string ValuesYaml,
    IReadOnlyList<string> Warnings);

/// <summary>
/// chart 包读取:以 <see cref="GZipStream"/> + <see cref="TarReader"/> 在内存中读取包根目录下的
/// Chart.yaml 与 values.yaml,不落盘解压(无路径穿越面);包不合法或缺少名称/版本时抛
/// <see cref="ValidationException"/>(中文提示)。
/// </summary>
public static class ChartPackageReader
{
    private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>解析 chart 包。</summary>
    /// <param name="content">.tgz 字节内容。</param>
    /// <returns>解析出的 chart 元数据、values 文本与警告。</returns>
    public static ChartPackageInfo Read(byte[] content)
    {
        if (content.Length == 0)
        {
            throw new ValidationException("chart 包为空");
        }

        string? chartMetadataText = null;
        var chartPrefix = "";
        var candidateValues = new Dictionary<string, string>(StringComparer.Ordinal);
        var prefixesWithChartsFolder = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var tar = new TarReader(gzip);
            while (tar.GetNextEntry() is { } entry)
            {
                if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile))
                {
                    continue;
                }

                var entryName = entry.Name.Replace('\\', '/');
                var chartsIndex = entryName.IndexOf("/charts/", StringComparison.Ordinal);
                if (chartsIndex >= 0)
                {
                    prefixesWithChartsFolder.Add(entryName[..(chartsIndex + 1)]);
                }

                if (entryName == "Chart.yaml" || entryName.EndsWith("/Chart.yaml", StringComparison.Ordinal))
                {
                    var prefix = entryName == "Chart.yaml" ? "" : entryName[..^"Chart.yaml".Length];
                    if (chartMetadataText is null || prefix.Length < chartPrefix.Length)
                    {
                        chartPrefix = prefix;
                        chartMetadataText = ReadText(entry);
                    }
                    continue;
                }
                if (entryName == "values.yaml" || entryName.EndsWith("/values.yaml", StringComparison.Ordinal))
                {
                    var prefix = entryName == "values.yaml" ? "" : entryName[..^"values.yaml".Length];
                    candidateValues[prefix] = ReadText(entry);
                }
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or FormatException)
        {
            throw new ValidationException($"chart 包无法读取:{ex.Message}");
        }

        if (chartMetadataText is null)
        {
            throw new ValidationException("chart 包不合法:缺少 Chart.yaml");
        }

        ChartMetadataDto? metadata;
        try
        {
            metadata = _yamlDeserializer.Deserialize<ChartMetadataDto>(chartMetadataText);
        }
        catch (Exception ex)
        {
            throw new ValidationException($"Chart.yaml 格式错误:{ex.Message}");
        }

        var name = metadata?.Name?.Trim() ?? "";
        var version = metadata?.Version?.Trim() ?? "";
        if (name.Length == 0 || version.Length == 0)
        {
            throw new ValidationException("Chart.yaml 缺少名称或版本");
        }

        var dependencies = metadata?.Dependencies?
            .Select(dependency => dependency.Name?.Trim() ?? "")
            .Where(dependencyName => dependencyName.Length > 0)
            .ToList() ?? [];
        var warnings = new List<string>();
        if (dependencies.Count > 0 && !prefixesWithChartsFolder.Contains(chartPrefix))
        {
            warnings.Add("包声明了 dependencies 但未包含 charts/ 目录,安装可能失败(请先执行 helm dependency build 后重新打包)");
        }

        return new ChartPackageInfo(
            name,
            version,
            metadata?.AppVersion?.Trim() ?? "",
            metadata?.Description?.Trim() ?? "",
            dependencies,
            candidateValues.GetValueOrDefault(chartPrefix, ""),
            warnings);
    }

    private static string ReadText(TarEntry entry)
    {
        if (entry.DataStream is null)
        {
            return "";
        }
        using var reader = new StreamReader(entry.DataStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed class ChartMetadataDto
    {
        public string? Name { get; set; }

        public string? Version { get; set; }

        public string? AppVersion { get; set; }

        public string? Description { get; set; }

        public List<ChartDependencyDto>? Dependencies { get; set; }
    }

    private sealed class ChartDependencyDto
    {
        public string? Name { get; set; }
    }
}
