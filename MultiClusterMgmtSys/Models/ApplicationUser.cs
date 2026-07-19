using Microsoft.AspNetCore.Identity;

namespace MultiClusterMgmtSys.Models;

public class ApplicationUser : IdentityUser<int>
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
