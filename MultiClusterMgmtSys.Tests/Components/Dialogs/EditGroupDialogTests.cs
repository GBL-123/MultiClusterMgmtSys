using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using System.Threading;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Dialogs;

public class EditGroupDialogTests
{
    [Fact]
    public async Task Create_group_renders_form()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var provider = ctx.Render<MudDialogProvider>();

        await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Clusters.Shared.EditGroupDialog>("新建分组");

        await provider.InvokeAsync(() => { });
        provider.WaitForState(() => provider.Markup.Contains("mud-dialog-content"), TimeSpan.FromSeconds(5));
        Assert.Contains("添加", provider.Markup);
        Assert.Contains("取消", provider.Markup);
    }

    [Fact]
    public async Task Rename_mode_prefills_and_shows_save()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var added = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("old-name"));
        await harness.Db.SaveChangesAsync(CancellationToken.None);
        var provider = ctx.Render<MudDialogProvider>();

        await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Clusters.Shared.EditGroupDialog>(
                "重命名分组",
                new DialogParameters { { "GroupId", added.Entity.Id }, { "InitialName", "old-name" } });

        provider.WaitForState(() => provider.Markup.Contains("old-name"));

        Assert.Contains("保存", provider.Markup);
    }

    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }
}


