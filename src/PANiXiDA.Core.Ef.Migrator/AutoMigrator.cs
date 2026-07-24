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
    public static Task<IHost> RunMigrationsAsync<TContext>(this IHostBuilder builder)
        where TContext : DbContext
    {
        return RunMigrationsAsync(
            builder,
            [DbContextMigration.For<TContext>()],
            useLegacyGenerationConfiguration: true);
    }

    /// <summary>
    /// Builds the host and runs migration generation and application for every specified DbContext.
    /// </summary>
    /// <param name="builder">The configured application builder with all specified DbContexts registered.</param>
    /// <param name="contexts">The DbContexts included in the migration run.</param>
    /// <returns>The built <see cref="IHost"/> after all selected migration actions are completed.</returns>
    /// <remarks>
    /// Contexts are processed sequentially in the supplied order. Generation settings are read from
    /// <c>Ef:Contexts:{ConfigurationName}:ProjectPath</c> and
    /// <c>Ef:Contexts:{ConfigurationName}:MigrationsDirectory</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="contexts"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when no contexts are supplied or the same DbContext or configuration name is registered more than once.
    /// </exception>
    public static Task<IHost> RunMigrationsAsync(
        this IHostBuilder builder,
        params DbContextMigration[] contexts)
    {
        return RunMigrationsAsync(
            builder,
            contexts,
            useLegacyGenerationConfiguration: false);
    }

    private static async Task<IHost> RunMigrationsAsync(
        IHostBuilder builder,
        DbContextMigration[] contexts,
        bool useLegacyGenerationConfiguration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateContexts(contexts);

        var host = builder.Build();

        using var scope = host.Services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var generateMigrations = configuration.GetValue("GenerateMigrations", true);
        var applyMigrations = configuration.GetValue("ApplyMigrations", true);

        if (!generateMigrations && !applyMigrations)
        {
            return host;
        }

        foreach (var context in contexts)
        {
            var db = (DbContext)scope.ServiceProvider.GetRequiredService(context.DbContextType);

            await RunDbContextMigrationsAsync(
                db,
                context,
                configuration,
                generateMigrations,
                applyMigrations,
                useLegacyGenerationConfiguration);
        }

        return host;
    }

    private static async Task RunDbContextMigrationsAsync(
        DbContext db,
        DbContextMigration context,
        IConfiguration configuration,
        bool generateMigrations,
        bool applyMigrations,
        bool useLegacyGenerationConfiguration)
    {
        if (!generateMigrations)
        {
            await MigrationsApplier.ApplyPendingMigrationsAsync(db);
            return;
        }

        using var designServiceProvider = CreateDesignServiceProvider(db);
        var difference = MigrationsDifferenceProvider.GetDifferences(db, designServiceProvider);

        if (difference.Count == 0)
        {
            if (applyMigrations)
            {
                await db.Database.MigrateAsync();
            }

            return;
        }

        var generationOptions = MigrationGenerationOptionsProvider.Get(
            context.DbContextType,
            configuration,
            useLegacyGenerationConfiguration
                ? null
                : context.ConfigurationName);

        var scaffoldedMigration = MigrationsCreator.CreateAndSaveMigration(
            designServiceProvider: designServiceProvider,
            difference: difference,
            contextType: context.DbContextType,
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

    private static void ValidateContexts(DbContextMigration[] contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        if (contexts.Length == 0)
        {
            throw new ArgumentException(
                "At least one DbContext migration must be specified.",
                nameof(contexts));
        }

        if (contexts.Any(context => context is null))
        {
            throw new ArgumentException(
                "DbContext migrations must not contain null values.",
                nameof(contexts));
        }

        var duplicateContext = contexts
            .GroupBy(context => context.DbContextType)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateContext is not null)
        {
            throw new ArgumentException(
                $"DbContext '{duplicateContext.Key.FullName}' is registered more than once.",
                nameof(contexts));
        }

        var duplicateConfigurationName = contexts
            .GroupBy(
                context => context.ConfigurationName,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateConfigurationName is not null)
        {
            throw new ArgumentException(
                $"Migration configuration name '{duplicateConfigurationName.Key}' is registered more than once.",
                nameof(contexts));
        }
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
