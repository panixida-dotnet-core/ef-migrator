using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Npgsql.EntityFrameworkCore.PostgreSQL.Design.Internal;

using PANiXiDA.Core.Ef.Migrator.Extensions;

namespace PANiXiDA.Core.Ef.Migrator;

/// <summary>
/// Provides extensions for automatically creating and applying Entity Framework Core migrations
/// for registered Entity Framework Core contexts.
/// </summary>
public static class AutoMigrator
{
    /// <summary>
    /// Checks the specified <typeparamref name="TContext"/> model,
    /// creates a new migration in the configured context directory when needed,
    /// and applies migrations to the database.
    /// </summary>
    /// <typeparam name="TContext">The Entity Framework Core context type to migrate.</typeparam>
    /// <param name="host">The built host with the registered <typeparamref name="TContext"/> and configuration.</param>
    /// <returns>A task that represents the asynchronous migration operation.</returns>
    /// <remarks>
    /// Behavior is controlled by the <c>GenerateMigrations</c> and <c>ApplyMigrations</c> settings.
    /// When generation is enabled and the model differs from the last snapshot, a migration is saved to the configured folder.
    /// When applying is enabled, existing or newly created migrations are applied through the connection configured for
    /// the registered <typeparamref name="TContext"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when migration generation requires the
    /// <c>Ef:Contexts:{DbContextName}:ProjectPath</c> or
    /// <c>Ef:Contexts:{DbContextName}:MigrationsDirectory</c> setting and it is missing.
    /// </exception>
    public static async Task RunMigrationsAsync<TContext>(this IHost host)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(host);

        using var scope = host.Services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var generateMigrations = configuration.GetValue("GenerateMigrations", true);
        var applyMigrations = configuration.GetValue("ApplyMigrations", true);

        if (!generateMigrations && !applyMigrations)
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        if (!generateMigrations)
        {
            await MigrationsApplier.ApplyPendingMigrationsAsync(db);
            return;
        }

        using var designServiceProvider = CreateDesignServiceProvider(db);
        var difference = MigrationsDifferenceProvider.GetDifferences(
            db,
            designServiceProvider);

        if (difference.Count == 0)
        {
            if (applyMigrations)
            {
                await db.Database.MigrateAsync();
            }

            return;
        }

        var generationOptions = MigrationGenerationOptionsProvider.Get<TContext>(configuration);

        var scaffoldedMigration = MigrationsCreator.CreateAndSaveMigration(
            designServiceProvider: designServiceProvider,
            difference: difference,
            contextType: typeof(TContext),
            rootNamespace: generationOptions.RootNamespace,
            subNamespace: generationOptions.MigrationsSubNamespace,
            projectPath: generationOptions.ProjectPathAbsolute,
            outputDir: generationOptions.MigrationsDirectory);

        if (!applyMigrations)
        {
            return;
        }

        await MigrationsApplier.ApplyPendingMigrationsAsync(db);

        await MigrationsApplier.ApplyMigrationAsync(
            db: db,
            difference: difference.UpOperations,
            migrationId: scaffoldedMigration.MigrationId,
            targetModel: difference.TargetModel);
    }

    private static ServiceProvider CreateDesignServiceProvider(DbContext db)
    {
        var services = new ServiceCollection()
            .AddEntityFrameworkDesignTimeServices()
            .AddDbContextDesignTimeServices(db);

#pragma warning disable EF1001
        new NpgsqlDesignTimeServices().ConfigureDesignTimeServices(services);
#pragma warning restore EF1001

        return services.BuildServiceProvider();
    }
}
