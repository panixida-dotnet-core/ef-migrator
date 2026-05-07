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
/// when a generic host starts.
/// </summary>
public static class AutoMigrator
{
    /// <summary>
    /// Builds the host, checks the specified <typeparamref name="TContext"/> model,
    /// creates a new migration in the <c>Ef:ProjectPath</c>/<c>Ef:MigrationsDirectory</c> directory when needed,
    /// and applies migrations to the database.
    /// </summary>
    /// <typeparam name="TContext">The Entity Framework Core context type to migrate.</typeparam>
    /// <param name="builder">The configured application builder with the registered <typeparamref name="TContext"/> and configuration.</param>
    /// <returns>The built <see cref="IHost"/> after the selected migration actions are completed.</returns>
    /// <remarks>
    /// Behavior is controlled by the <c>GenerateMigrations</c> and <c>ApplyMigrations</c> settings.
    /// When generation is enabled and the model differs from the last snapshot, a migration is saved to the configured folder.
    /// When applying is enabled, existing or newly created migrations are applied through the connection configured for
    /// the registered <typeparamref name="TContext"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when migration generation requires the <c>Ef:ProjectPath</c> or <c>Ef:MigrationsDirectory</c>
    /// setting and it is missing.
    /// </exception>
    public static async Task<IHost> RunMigrationsAsync<TContext>(this IHostBuilder builder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var host = builder.Build();

        using var scope = host.Services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        var generateMigrations = configuration.GetValue("GenerateMigrations", true);
        var applyMigrations = configuration.GetValue("ApplyMigrations", true);

        if (!generateMigrations && !applyMigrations)
        {
            return host;
        }

        if (!generateMigrations)
        {
            if (applyMigrations)
            {
                await MigrationsApplier.ApplyPendingMigrationsAsync(db);
            }

            return host;
        }

        using var designServiceProvider = CreateDesignServiceProvider(db);
        var difference = MigrationsDifferenceProvider.GetDifferences(db, designServiceProvider);

        if (difference.Count == 0)
        {
            if (applyMigrations)
            {
                await db.Database.MigrateAsync();
            }

            return host;
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

        if (applyMigrations)
        {
            await MigrationsApplier.ApplyPendingMigrationsAsync(db);

            await MigrationsApplier.ApplyMigrationAsync(
                db: db,
                difference: difference.UpOperations,
                migrationId: scaffoldedMigration.MigrationId,
                targetModel: difference.TargetModel);
        }

        return host;
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
