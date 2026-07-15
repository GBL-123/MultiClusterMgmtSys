using Microsoft.AspNetCore.Identity;

namespace MultiClusterMgmtSys.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
