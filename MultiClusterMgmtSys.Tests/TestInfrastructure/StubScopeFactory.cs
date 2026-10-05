using Microsoft.Extensions.DependencyInjection;
using MultiClusterMgmtSys.Application.Abstractions;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>
/// 固定单服务的作用域工厂测试桩:模拟宿主 DI 供 singleton 服务按作用域解析仓储,
/// 服务集合内预注册传入的仓储实例(可 mock 或真实仓储)。
/// </summary>
public sealed class StubScopeFactory : IServiceScopeFactory
{
    private readonly ServiceProvider _provider;

    private StubScopeFactory(ServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>按单个服务实例构造作用域工厂;实例在所有作用域中共享。</summary>
    /// <param name="instance">注入服务集合的唯一实例(如健康快照仓储 mock)。</param>
    /// <returns>可交给被测 singleton 服务的作用域工厂。</returns>
    public static StubScopeFactory Of<T>(T instance)
        where T : class
    {
        var services = new ServiceCollection();
        services.AddSingleton(instance);
        return new StubScopeFactory(services.BuildServiceProvider());
    }

    /// <inheritdoc />
    public IServiceScope CreateScope() => _provider.CreateScope();
}
