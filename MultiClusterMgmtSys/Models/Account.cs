namespace MultiClusterMgmtSys.Models;

public class Account
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public AppRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
