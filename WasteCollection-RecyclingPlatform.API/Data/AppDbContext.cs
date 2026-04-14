using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using WasteCollection_RecyclingPlatform.Repositories.Data;

namespace WasteCollection_RecyclingPlatform.API.Data;

public class AppDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>();
        var conn = config.GetConnectionString("Default");
        optionsBuilder.UseMySql(conn, new MySqlServerVersion(new Version(8, 0, 34)),
            x => x.MigrationsAssembly("WasteCollection-RecyclingPlatform.API"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
