using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Daos;

public class AccountRepository(AppDbContext db)
{
    private readonly AppDbContext db = db;

    public async Task<Account?> GetByUsernameAsync(string username)
    {
        return await db.Accounts.FirstOrDefaultAsync(a => a.Username == username);
    }

    public async Task<int> CountAsync()
    {
        return await db.Accounts.CountAsync();
    }

    public async Task<Account> AddAsync(Account entity)
    {
        db.Accounts.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }
}
