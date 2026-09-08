using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using MultiClusterMgmtSys.Components.Common;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public sealed class BunitHost : BunitContext
{
    public BunitHost()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.TryAddSingleton(TimeProvider.System);
        Services.AddSingleton(NullLoggerFactory.Instance);
        Services.AddScoped<ExceptionPresenter>();
        Services.AddSingleton<ISnackbar>(sp => new Mock<ISnackbar>().Object);
    }
}
