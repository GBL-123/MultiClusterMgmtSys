using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Common.Helm;

/// <summary>Helm release 列表项(helm list -o json 的解析结果)。</summary>
/// <param name="Name">release 名称。</param>
/// <param name="Namespace">命名空间。</param>
/// <param name="Revision">当前 revision。</param>
/// <param name="Status">Helm 状态原值(如 deployed)。</param>
/// <param name="Chart">chart 引用(形如 name-version)。</param>
/// <param name="AppVersion">chart 的 appVersion。</param>
/// <param name="UpdatedAt">最后更新时间;格式无法识别时为 null。</param>
public sealed record HelmReleaseListEntry(
    string Name,
    string Namespace,
    int Revision,
    string Status,
    string Chart,
    string AppVersion,
    DateTime? UpdatedAt);

/// <summary>Helm release 状态详情(helm status -o json 的解析结果)。</summary>
/// <param name="Name">release 名称。</param>
/// <param name="Namespace">命名空间。</param>
/// <param name="Revision">当前 revision。</param>
/// <param name="Status">Helm 状态原值。</param>
/// <param name="Description">状态描述(如 Upgrade complete)。</param>
/// <param name="Notes">chart NOTES 文本。</param>
/// <param name="ChartName">chart 名称。</param>
/// <param name="ChartVersion">chart 版本。</param>
/// <param name="AppVersion">chart 的 appVersion。</param>
/// <param name="Manifest">渲染后的 manifest YAML。</param>
/// <param name="LastDeployedAt">最后部署时间。</param>
public sealed record HelmReleaseStatus(
    string Name,
    string Namespace,
    int Revision,
    string Status,
    string Description,
    string Notes,
    string ChartName,
    string ChartVersion,
    string AppVersion,
    string Manifest,
    DateTime? LastDeployedAt);

/// <summary>Helm release 历史项(helm history -o json 的解析结果)。</summary>
/// <param name="Revision">revision 序号。</param>
/// <param name="Status">该 revision 的 Helm 状态原值。</param>
/// <param name="Chart">chart 引用。</param>
/// <param name="AppVersion">chart 的 appVersion。</param>
/// <param name="Description">该 revision 的描述。</param>
/// <param name="UpdatedAt">该 revision 的时间。</param>
public sealed record HelmReleaseHistoryEntry(
    int Revision,
    string Status,
    string Chart,
    string AppVersion,
    string Description,
    DateTime? UpdatedAt);

/// <summary>
/// Helm CLI JSON 输出解析器:解析列表/状态/历史三类结构化输出;values 以 `-o yaml` 文本直读,不经此层。
/// 输出无法解析时抛 <see cref="HelmOperationException"/>(中文提示,不携带原始输出)。
/// </summary>
public static class HelmOutputParser
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>解析 `helm list -o json` 输出。</summary>
    /// <param name="json">标准输出文本。</param>
    /// <returns>release 列表项;输出为空数组时返回空列表。</returns>
    public static List<HelmReleaseListEntry> ParseReleaseList(string json)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<ListItemDto>>(json, _options) ?? [];
            return [.. items.Select(item => new HelmReleaseListEntry(
                item.Name ?? "",
                item.Namespace ?? "",
                ParseRevision(item.Revision),
                item.Status ?? "",
                item.Chart ?? "",
                item.AppVersion ?? "",
                TryParseTime(item.Updated)))];
        }
        catch (JsonException)
        {
            throw new HelmOperationException("Helm 命令输出无法解析");
        }
    }

    /// <summary>解析 `helm status -o json` 输出。</summary>
    /// <param name="json">标准输出文本。</param>
    /// <returns>release 状态详情。</returns>
    public static HelmReleaseStatus ParseStatus(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<StatusDto>(json, _options)
                ?? throw new JsonException("empty status output");
            return new HelmReleaseStatus(
                dto.Name ?? "",
                dto.Namespace ?? "",
                dto.Version,
                dto.Info?.Status ?? "",
                dto.Info?.Description ?? "",
                dto.Info?.Notes ?? "",
                dto.Chart?.Metadata?.Name ?? "",
                dto.Chart?.Metadata?.Version ?? "",
                dto.Chart?.Metadata?.AppVersion ?? "",
                dto.Manifest ?? "",
                TryParseTime(dto.Info?.LastDeployed));
        }
        catch (JsonException)
        {
            throw new HelmOperationException("Helm 命令输出无法解析");
        }
    }

    /// <summary>解析 `helm history -o json` 输出。</summary>
    /// <param name="json">标准输出文本。</param>
    /// <returns>历史项列表。</returns>
    public static List<HelmReleaseHistoryEntry> ParseHistory(string json)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<HistoryItemDto>>(json, _options) ?? [];
            return [.. items.Select(item => new HelmReleaseHistoryEntry(
                item.Revision,
                item.Status ?? "",
                item.Chart ?? "",
                item.AppVersion ?? "",
                item.Description ?? "",
                TryParseTime(item.Updated)))];
        }
        catch (JsonException)
        {
            throw new HelmOperationException("Helm 命令输出无法解析");
        }
    }

    /// <summary>
    /// 解析 Helm 输出的时间文本:兼容 RFC3339 与 Go time.Time.String()(形如 `2026-09-20 10:12:33.123456789 +0000 UTC`);
    /// 无法识别返回 null。
    /// </summary>
    /// <param name="value">时间文本。</param>
    /// <returns>UTC 时间;不可解析时为 null。</returns>
    public static DateTime? TryParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        var utcSuffixIndex = text.IndexOf(" UTC", StringComparison.Ordinal);
        if (utcSuffixIndex > 0)
        {
            text = text[..utcSuffixIndex];
        }

        if (text.Length > 10 && text[10] == ' ')
        {
            text = text[..10] + "T" + text[11..];
        }

        var dotIndex = text.IndexOf('.');
        if (dotIndex > 0)
        {
            var digitsEnd = dotIndex + 1;
            while (digitsEnd < text.Length && char.IsAsciiDigit(text[digitsEnd]))
            {
                digitsEnd++;
            }
            if (digitsEnd - dotIndex - 1 > 7)
            {
                text = text[..(dotIndex + 8)] + text[digitsEnd..];
            }
        }

        if (text.Length >= 5)
        {
            var offset = text[^5..];
            if ((offset[0] == '+' || offset[0] == '-') && offset[1..].All(char.IsAsciiDigit))
            {
                text = text[..^5] + offset[..3] + ":" + offset[3..];
            }
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return parsed.UtcDateTime;
        }
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
        {
            return parsed.UtcDateTime;
        }
        return null;
    }

    private static int ParseRevision(string? revision)
        => int.TryParse(revision, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    private sealed class ListItemDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }

        [JsonPropertyName("namespace")] public string? Namespace { get; set; }

        [JsonPropertyName("revision")] public string? Revision { get; set; }

        [JsonPropertyName("updated")] public string? Updated { get; set; }

        [JsonPropertyName("status")] public string? Status { get; set; }

        [JsonPropertyName("chart")] public string? Chart { get; set; }

        [JsonPropertyName("app_version")] public string? AppVersion { get; set; }
    }

    private sealed class StatusDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }

        [JsonPropertyName("namespace")] public string? Namespace { get; set; }

        [JsonPropertyName("version")] public int Version { get; set; }

        [JsonPropertyName("manifest")] public string? Manifest { get; set; }

        [JsonPropertyName("info")] public StatusInfoDto? Info { get; set; }

        [JsonPropertyName("chart")] public StatusChartDto? Chart { get; set; }
    }

    private sealed class StatusInfoDto
    {
        [JsonPropertyName("last_deployed")] public string? LastDeployed { get; set; }

        [JsonPropertyName("description")] public string? Description { get; set; }

        [JsonPropertyName("status")] public string? Status { get; set; }

        [JsonPropertyName("notes")] public string? Notes { get; set; }
    }

    private sealed class StatusChartDto
    {
        [JsonPropertyName("metadata")] public StatusChartMetadataDto? Metadata { get; set; }
    }

    private sealed class StatusChartMetadataDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }

        [JsonPropertyName("version")] public string? Version { get; set; }

        [JsonPropertyName("appVersion")] public string? AppVersion { get; set; }
    }

    private sealed class HistoryItemDto
    {
        [JsonPropertyName("revision")] public int Revision { get; set; }

        [JsonPropertyName("updated")] public string? Updated { get; set; }

        [JsonPropertyName("status")] public string? Status { get; set; }

        [JsonPropertyName("chart")] public string? Chart { get; set; }

        [JsonPropertyName("app_version")] public string? AppVersion { get; set; }

        [JsonPropertyName("description")] public string? Description { get; set; }
    }
}
