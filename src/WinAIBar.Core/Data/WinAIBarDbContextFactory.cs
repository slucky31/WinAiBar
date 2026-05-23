using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WinAIBar.Core.Data;

public class WinAIBarDbContextFactory : IDesignTimeDbContextFactory<WinAIBarDbContext>
{
    public WinAIBarDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WinAIBarDbContext>()
            .UseSqlite("Data Source=designtime.db")
            .Options;
        return new WinAIBarDbContext(options);
    }
}
