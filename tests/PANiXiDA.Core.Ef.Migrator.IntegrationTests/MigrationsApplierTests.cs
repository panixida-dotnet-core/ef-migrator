using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using PANiXiDA.Core.Ef.Migrator.Extensions;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

[Collection(nameof(PostgreSqlCollection))]
public sealed class MigrationsApplierTests(PostgreSqlContainerFixture fixture)
{
    private const string MigrationId = "20260420123000_ApplyMigration";

    [Fact(DisplayName = "Skips an existing migration when it is already in the history table")]
    public async Task ApplyPendingMigrationsAsync_WhenMigrationAlreadyApplied_SkipsIt()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        await using var db = new ExistingMigrationDbContext(CreateOptions<ExistingMigrationDbContext>(connectionString));

        await MigrationsApplier.ApplyPendingMigrationsAsync(db);
        await MigrationsApplier.ApplyPendingMigrationsAsync(db);

        (await DatabaseAssert.TableExistsAsync(connectionString, "existing_entities")).Should().BeTrue();
        (await DatabaseAssert.HistoryRowsCountAsync(connectionString)).Should().Be(1);
    }

    [Fact(DisplayName = "Applies SQL operations and writes a migration history row")]
    public async Task ApplyMigrationAsync_WhenDifferenceExists_AppliesOperationsAndWritesHistory()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        await using var db = new GeneratedMigrationDbContext(CreateOptions<GeneratedMigrationDbContext>(connectionString));
        var operations = new MigrationOperation[]
        {
            MigrationOperations.CreateTable("applier_entities"),
        };

        await MigrationsApplier.ApplyMigrationAsync(db, operations, MigrationId);

        (await DatabaseAssert.TableExistsAsync(connectionString, "applier_entities")).Should().BeTrue();
        (await DatabaseAssert.HistoryRowExistsAsync(connectionString, MigrationId)).Should().BeTrue();
    }

    private static DbContextOptions<TContext> CreateOptions<TContext>(string connectionString)
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(TContext).Assembly.GetName().Name))
            .Options;
    }
}
