using MultiClusterMgmtSys.Infrastructure.Helm;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Helm;

public sealed class HelmTempDirectoryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mcm-helm-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void CleanupStale_removes_only_expired_directories()
    {
        Directory.CreateDirectory(_root);
        var expiredDirectory = Directory.CreateDirectory(Path.Combine(_root, "expired")).FullName;
        var freshDirectory = Directory.CreateDirectory(Path.Combine(_root, "fresh")).FullName;
        var now = DateTime.UtcNow;
        Directory.SetLastWriteTimeUtc(expiredDirectory, now.AddHours(-25));
        Directory.SetLastWriteTimeUtc(freshDirectory, now.AddHours(-1));

        var removed = HelmTempDirectory.CleanupStale(_root, TimeSpan.FromHours(24), now);

        Assert.Equal(1, removed);
        Assert.False(Directory.Exists(expiredDirectory));
        Assert.True(Directory.Exists(freshDirectory));
    }

    [Fact]
    public void CleanupStale_returns_zero_when_root_missing()
    {
        Assert.Equal(0, HelmTempDirectory.CleanupStale(Path.Combine(_root, "missing"), TimeSpan.FromHours(24), DateTime.UtcNow));
    }

    [Fact]
    public void Create_makes_isolated_directories_under_root()
    {
        var first = HelmTempDirectory.Create();
        var second = HelmTempDirectory.Create();

        try
        {
            Assert.True(Directory.Exists(first));
            Assert.True(Directory.Exists(second));
            Assert.NotEqual(first, second);
            Assert.StartsWith(HelmTempDirectory.Root, first);
        }
        finally
        {
            HelmTempDirectory.TryDelete(first);
            HelmTempDirectory.TryDelete(second);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
