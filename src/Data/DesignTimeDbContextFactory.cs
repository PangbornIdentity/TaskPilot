using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskPilot.Data;

/// <summary>
/// Used by `dotnet ef migrations add` at design time.
/// Always uses SQL Server so generated migrations are Azure SQL-compatible.
/// Connection string is never used at design time (only for schema introspection).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=.;Database=TaskPilot_Design;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new ApplicationDbContext(options);
    }
}
