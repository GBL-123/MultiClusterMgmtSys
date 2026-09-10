using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Dialogs;

public class EditClusterDialogTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    [Fact]
    public async Task Edit_mode_prefills_cluster_fields()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var added = await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("edit-dialog-cluster", version: "1.29.0"));

        var provider = ctx.Render<MudDialogProvider>();
        var reference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Clusters.Shared.EditClusterDialog>(
                "编辑集群",
                new MudBlazor.DialogParameters { { "ClusterId", added.Id } });

        provider.WaitForState(() => provider.Markup.Contains("edit-dialog-cluster"));

        Assert.Contains("集群名称", provider.Markup);
        Assert.Contains("API Server", provider.Markup);
        Assert.Contains("保存", provider.Markup);
    }

    [Fact]
    public async Task Submit_updates_cluster_name_and_closes()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var added = await harness.ClusterRepo.AddAsync(TestData.NewCluster("rename-me"));

        var provider = ctx.Render<MudDialogProvider>();
        var reference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Clusters.Shared.EditClusterDialog>(
                "编辑集群",
                new MudBlazor.DialogParameters { { "ClusterId", added.Id } });

        provider.WaitForState(() => provider.Markup.Contains("rename-me"));

        var nameField = provider.FindComponents<MudTextField<string>>()
            .First(f => f.Instance.Label == "集群名称");
        await provider.InvokeAsync(async () => await nameField.Instance.ValueChanged!.InvokeAsync("renamed-cluster"));

        var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("保存"));
        submit.Find("button").Click();

        provider.WaitForState(
            () => harness.ClusterRepo.GetByIdAsync(added.Id)!.Result!.Name == "renamed-cluster" ||
                  harness.ClusterRepo.GetByIdAsync(added.Id).Result!.Name == "renamed-cluster",
            TimeSpan.FromSeconds(10));

        var reloaded = await harness.ClusterRepo.GetByIdAsync(added.Id);
        Assert.Equal("renamed-cluster", reloaded!.Name);
    }
}
