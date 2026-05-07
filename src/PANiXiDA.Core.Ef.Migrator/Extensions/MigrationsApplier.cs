using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using System.Reflection;

namespace PANiXiDA.Core.Ef.Migrator.Extensions;

internal static class MigrationsApplier
{
    internal static async Task ApplyPendingMigrationsAsync(DbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        var activeProvider = db.Database.ProviderName!;
        var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync())
            .ToHashSet(StringComparer.Ordinal);
        var migrationsAssembly = db.GetService<IMigrationsAssembly>();

        foreach (var (migrationId, migrationType) in migrationsAssembly.Migrations.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (appliedMigrations.Contains(migrationId))
            {
                continue;
            }

            var migration = migrationsAssembly.CreateMigration(migrationType, activeProvider);

            await ApplyMigrationAsync(
                db: db,
                difference: migration.UpOperations,
                migrationId: migrationId,
                targetModel: migration.TargetModel);
        }
    }

    internal static async Task ApplyMigrationAsync(
        DbContext db,
        IReadOnlyList<MigrationOperation> difference,
        string migrationId,
        IModel? targetModel = null)
    {
        ArgumentNullException.ThrowIfNull(difference);
        if (string.IsNullOrWhiteSpace(migrationId)) throw new ArgumentException("migrationId не может быть пустым", nameof(migrationId));

        using var transaction = await db.Database.BeginTransactionAsync();

        await ApplyEntityChangesAsync(db, difference, targetModel);
        await ApplyHistoryRowAsync(db, migrationId);

        await transaction.CommitAsync();
    }

    private static async Task ApplyEntityChangesAsync(
        DbContext db,
        IReadOnlyList<MigrationOperation> difference,
        IModel? targetModel)
    {
        var designTimeModel = targetModel ?? db.GetService<IDesignTimeModel>().Model;

        var sqlGenerator = db.GetService<IMigrationsSqlGenerator>();
        var commands = sqlGenerator.Generate(difference, designTimeModel);

        foreach (var command in commands.Where(c => !string.IsNullOrWhiteSpace(c.CommandText)))
        {
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        }
    }

    private static async Task ApplyHistoryRowAsync(
        DbContext db,
        string migrationId)
    {
        var historyRepository = db.GetService<IHistoryRepository>();

        var createHistorySql = historyRepository.GetCreateIfNotExistsScript();
        await db.Database.ExecuteSqlRawAsync(createHistorySql);

        var efVersion = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;

        var historyRow = new HistoryRow(migrationId, efVersion);
        var insertHistorySql = historyRepository.GetInsertScript(historyRow);
        await db.Database.ExecuteSqlRawAsync(insertHistorySql);
    }
}
