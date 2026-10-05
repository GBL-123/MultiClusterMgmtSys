using System;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>
/// 可编程推进的 TimeProvider 桩:供限频/超时类测试控制当前时间。
/// </summary>
public sealed class StubTimeProvider : TimeProvider
{
    private DateTime _utcNow;

    /// <summary>以指定初始 UTC 时间构造桩。</summary>
    /// <param name="initialUtcNow">初始的 UTC 当前时间。</param>
    public StubTimeProvider(DateTime initialUtcNow)
    {
        _utcNow = DateTime.SpecifyKind(initialUtcNow, DateTimeKind.Utc);
    }

    /// <summary>推进桩时间。</summary>
    /// <param name="delta">前进的时间量。</param>
    public void Advance(TimeSpan delta) => _utcNow += delta;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
}
