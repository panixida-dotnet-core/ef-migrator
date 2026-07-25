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
            configurationValues[$"Ef:Contexts:{typeof(TContext).Name}:ProjectPath"] = projectPath;
        }

        if (migrationsDirectory is not null)
        {
            configurationValues[$"Ef:Contexts:{typeof(TContext).Name}:MigrationsDirectory"] = migrationsDirectory;
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
                AddDbContext<TContext>(services, postgreSqlConnectionString);
            });
    }

    public static IHostBuilder Create<TFirstContext, TSecondContext>(
        string postgreSqlConnectionString,
        bool generateMigrations,
        bool applyMigrations,
        IReadOnlyDictionary<string, string?> additionalConfiguration)
        where TFirstContext : DbContext
        where TSecondContext : DbContext
    {
        var configurationValues = new Dictionary<string, string?>(additionalConfiguration)
        {
            ["PostgreSqlConnectionString"] = postgreSqlConnectionString,
            ["GenerateMigrations"] = generateMigrations.ToString(),
            ["ApplyMigrations"] = applyMigrations.ToString(),
        };

        return Host
            .CreateDefaultBuilder([])
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(configurationValues);
            })
            .ConfigureServices(services =>
            {
                AddDbContext<TFirstContext>(services, postgreSqlConnectionString);
                AddDbContext<TSecondContext>(services, postgreSqlConnectionString);
            });
    }

    private static void AddDbContext<TContext>(
        IServiceCollection services,
        string postgreSqlConnectionString)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(options =>
        {
            options.UseNpgsql(
                postgreSqlConnectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(TContext).Assembly.GetName().Name));
        });
    }
}
