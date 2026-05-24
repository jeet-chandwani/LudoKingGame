using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LudoKing.Data.Context;

public class LudoKingDbContextFactory : IDesignTimeDbContextFactory<LudoKingDbContext>
{
    public LudoKingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LudoKingDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=LudoKingDb;User Id=sa;Password=TestPassword123!;TrustServerCertificate=True;",
            b => b.MigrationsAssembly(typeof(LudoKingDbContext).Assembly.FullName));
        return new LudoKingDbContext(optionsBuilder.Options);
    }
}
