using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Dialogs;

public class WorkloadScaleDialogTests
{
    [Fact]
    public async Task Initial_replicas_equal_current()
    {
        await using var ctx = new BunitHost();
        var provider = ctx.Render<MudDialogProvider>();

        var dialogReference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadScaleDialog>(
                "扩缩容",
                new DialogParameters
                {
                    { "Name", "web" },
                    { "CurrentReplicas", 4 }
                });

        provider.WaitForState(() => provider.Markup.Contains("确认"));

        Assert.Contains("4", provider.Markup);
    }

    [Fact]
    public async Task Submit_returns_replicas()
    {
        await using var ctx = new BunitHost();
        var provider = ctx.Render<MudDialogProvider>();

        var dialogReference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadScaleDialog>(
                "扩缩容",
                new DialogParameters
                {
                    { "Name", "web" },
                    { "CurrentReplicas", 2 }
                });

        provider.WaitForState(() => provider.Markup.Contains("确认"));

        var plusButton = provider.FindComponents<MudIconButton>().Last();
        await provider.InvokeAsync(async () => await plusButton.Instance.OnClick.InvokeAsync());

        var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("确认"));
        await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

        var result = await dialogReference.Result;
        Assert.False(result.Canceled);
        Assert.Equal(3, (int)result.Data!);
    }

    [Fact]
    public async Task Negative_adjust_clamps_to_zero()
    {
        await using var ctx = new BunitHost();
        var provider = ctx.Render<MudDialogProvider>();

        var dialogReference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadScaleDialog>(
                "扩缩容",
                new DialogParameters
                {
                    { "Name", "web" },
                    { "CurrentReplicas", 0 }
                });

        provider.WaitForState(() => provider.Markup.Contains("确认"));

        var minusButton = provider.FindComponents<MudIconButton>().First();
        await provider.InvokeAsync(async () => await minusButton.Instance.OnClick.InvokeAsync());

        var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("确认"));
        await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

        var result = await dialogReference.Result;
        Assert.Equal(0, (int)result.Data!);
    }

    [Fact]
    public async Task Cancel_marks_canceled()
    {
        await using var ctx = new BunitHost();
        var provider = ctx.Render<MudDialogProvider>();

        var dialogReference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadScaleDialog>(
                "扩缩容",
                new DialogParameters { { "Name", "web" }, { "CurrentReplicas", 1 } });

        provider.WaitForState(() => provider.Markup.Contains("取消"));

        var cancel = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("取消"));
        await provider.InvokeAsync(async () => await cancel.Instance.OnClick.InvokeAsync());

        var result = await dialogReference.Result;
        Assert.True(result.Canceled);
    }
}
