using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Os.Api.Infra;

public class DesignFactory : IDesignTimeDbContextFactory<OsDb>
{
    public OsDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<OsDb>().UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__Database") ?? "Server=localhost;Database=Os;Integrated Security=true;TrustServerCertificate=true").Options);
}
