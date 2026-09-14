namespace MultiClusterMgmtSys.Common.Time;

/// <summary>
/// 相对时间展示格式化(如「3 分钟前」),用于事件等瞬态数据的列表主展示。
/// </summary>
public static class RelativeTimeFormatter
{
    /// <summary>将时间格式化为相对当前时刻的中文文本;null 返回占位符 —。</summary>
    /// <param name="value">待格式化的时间;UTC 时间先转换为本地时间再参与比较。</param>
    /// <param name="now">比较基准时刻(默认当前本地时间),供测试注入固定时刻。</param>
    /// <returns>相对时间文本(刚刚 / N 分钟前 / N 小时前 / N 天前)或占位符 —。</returns>
    public static string Format(DateTime? value, DateTime? now = null)
    {
        if (value is null)
        {
            return "—";
        }

        var local = value.Value.Kind == DateTimeKind.Utc ? value.Value.ToLocalTime() : value.Value;
        var delta = (now ?? DateTime.Now) - local;
        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }

        if (delta.TotalSeconds < 60)
        {
            return "刚刚";
        }

        if (delta.TotalMinutes < 60)
        {
            return $"{(int)delta.TotalMinutes} 分钟前";
        }

        if (delta.TotalHours < 24)
        {
            return $"{(int)delta.TotalHours} 小时前";
        }

        return $"{(int)delta.TotalDays} 天前";
    }
}
