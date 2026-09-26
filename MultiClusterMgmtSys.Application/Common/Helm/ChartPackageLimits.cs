using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Common.Helm;

/// <summary>上传 chart 包的大小上限校验(契约见 helm-release-management spec)。</summary>
public static class ChartPackageLimits
{
    /// <summary>校验包大小是否在上限内;超出抛 <see cref="ValidationException"/>(中文提示,含上限兆字节数)。</summary>
    /// <param name="sizeInBytes">包字节数。</param>
    /// <param name="maxBytes">上限字节数;非正数表示不限制。</param>
    public static void EnsureWithinLimit(long sizeInBytes, long maxBytes)
    {
        if (maxBytes > 0 && sizeInBytes > maxBytes)
        {
            throw new ValidationException($"chart 包超出大小上限({maxBytes / 1024 / 1024} MB)");
        }
    }
}
