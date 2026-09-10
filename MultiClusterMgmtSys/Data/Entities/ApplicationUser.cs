using Microsoft.AspNetCore.Identity;

namespace MultiClusterMgmtSys.Data.Entities;

/// <summary>
/// 应用用户:继承 ASP.NET Identity 用户(int 主键),角色为 Admin/Member;
/// 追加创建/更新/登录时间用于账号管理页展示。
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    /// <summary>创建时间;数据库默认值 CURRENT_TIMESTAMP,插入时未显式赋值则由数据库填充。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>最近更新时间,可空(从未修改过则为空)。</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>最近登录时间,可空(从未登录过则为空)。</summary>
    public DateTime? LastLoginAt { get; set; }
}
