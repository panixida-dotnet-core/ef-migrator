using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;

internal static class TestHostBuilder
{
    public static IHostBuilder Create<TContext>(
        string postgreSqlConnectionString,
        bool generateMigrations,
        bool applyMigrations,
        string? projectPath = null,
        string? migrationsDirectory = null)
        where TContext : DbContext
    {
        var configurationValues = new Dictionary<string, string?>
        {
            ["PostgreSqlConnectionString"] = postgreSqlConnectionString,
            ["GenerateMigrations"] = generateMigrations.ToString(),
            ["ApplyMigrations"] = applyMigrations.ToString(),
        };

        if (projectPath is not null)
        {
            configurationValues["Ef:ProjectPath"] = projectPath;
        }

        if (migrationsDirectory is not null)
        {
            configurationValues["Ef:MigrationsDirectory"] = migrationsDirectory;
        }

        return Host
            .CreateDefaultBuilder([])
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(configurationValues);
            })
            .ConfigureServices(services =>
            {
                services.AddDbContext<TContext>(options =>
                {
                    options.UseNpgsql(
                        postgreSqlConnectionString,
                        npgsql => npgsql.MigrationsAssembly(typeof(TContext).Assembly.GetName().Name));
                });
            });
    }
}
