using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.Tests.Common;

public class PagedResultTests
{
    [Fact]
    public void Default_constructor_creates_empty_result()
    {
        var result = new PagedResult<string>();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void Parameterized_constructor_sets_items_and_total()
    {
        var result = new PagedResult<string>(["a", "b"], 42);

        Assert.Equal(["a", "b"], result.Items);
        Assert.Equal(42, result.Total);
    }
}

public class VersionFilterSentinelTests
{
    [Fact]
    public void All_is_empty_string()
    {
        Assert.Equal("", VersionFilterSentinel.All);
    }

    [Fact]
    public void OnlyNull_is_marker_value()
    {
        Assert.Equal("__null__", VersionFilterSentinel.OnlyNull);
    }
}
