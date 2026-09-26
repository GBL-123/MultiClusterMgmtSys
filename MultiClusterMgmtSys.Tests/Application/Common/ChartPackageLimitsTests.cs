using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class ChartPackageLimitsTests
{
    [Fact]
    public void EnsureWithinLimit_allows_size_at_limit()
    {
        ChartPackageLimits.EnsureWithinLimit(52_428_800, 52_428_800);
    }

    [Fact]
    public void EnsureWithinLimit_throws_when_over_limit()
    {
        var exception = Assert.Throws<ValidationException>(() => ChartPackageLimits.EnsureWithinLimit(52_428_801, 52_428_800));

        Assert.Contains("50 MB", exception.UserMessage);
    }

    [Fact]
    public void EnsureWithinLimit_treats_non_positive_max_as_unlimited()
    {
        ChartPackageLimits.EnsureWithinLimit(long.MaxValue, 0);
        ChartPackageLimits.EnsureWithinLimit(long.MaxValue, -1);
    }
}
