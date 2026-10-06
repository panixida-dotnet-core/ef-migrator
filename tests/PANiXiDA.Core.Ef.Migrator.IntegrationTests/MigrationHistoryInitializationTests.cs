using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Npgsql;

using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

[Collection(nameof(PostgreSqlCollection))]
public sealed class MigrationHistoryInitializationTests(PostgreSqlContainerFixture fixture)
{
    [Theory(DisplayName = "Initializes migration history without error logs on first and repeated runs")]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RunMigrationsAsync_WhenHistoryIsMissing_InitializesItWithoutErrors(
        bool generateMigrations,
        bool customHistory)
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        var errors = new List<string>();
        using var host = CreateHost(connectionString, generateMigrations, true, customHistory, errors);

        await host.RunMigrationsAsync<ExistingMigrationDbContext>();
        await host.RunMigrationsAsync<ExistingMigrationDbContext>();

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExistingMigrationDbContext>();
        (await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Should().ContainSingle();
        (await DatabaseAssert.TableExistsAsync(connectionString, "existing_entities")).Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Leaves migration history absent when applying migrations is disabled")]
    public async Task RunMigrationsAsync_WhenApplyingDisabled_DoesNotCreateHistory()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        var errors = new List<string>();
        using var host = CreateHost(connectionString, true, false, true, errors);

        await host.RunMigrationsAsync<ExistingMigrationDbContext>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("SELECT to_regclass('admin.migration_history') IS NOT NULL", connection);
        (await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)).Should().Be(false);
        errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Preserves automatic database creation when compiled migrations are applied")]
    public async Task RunMigrationsAsync_WhenDatabaseIsMissing_CreatesDatabaseAndAppliesMigrations()
    {
        var existingConnection = await fixture.CreateConnectionStringAsync();
        var connectionString = new NpgsqlConnectionStringBuilder(existingConnection)
        {
            Database = "missing_" + Guid.NewGuid().ToString("N"),
        }.ConnectionString;
        var errors = new List<string>();
        using var host = CreateHost(connectionString, true, true, false, errors);

        await host.RunMigrationsAsync<ExistingMigrationDbContext>();

        (await DatabaseAssert.HistoryRowsCountAsync(connectionString)).Should().Be(1);
        (await DatabaseAssert.TableExistsAsync(connectionString, "existing_entities")).Should().BeTrue();
    }

    [Fact(DisplayName = "Preserves error logging and exceptions for a real migration failure")]
    public async Task RunMigrationsAsync_WhenMigrationFails_LogsErrorAndDoesNotRecordMigration()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = new NpgsqlCommand("CREATE TABLE existing_entities (id integer PRIMARY KEY)", connection);
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var errors = new List<string>();
        using var host = CreateHost(connectionString, true, true, false, errors);

        var act = async () => await host.RunMigrationsAsync<ExistingMigrationDbContext>();

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.DuplicateTable);
        errors.Should().Contain(message => message.Contains("Failed executing DbCommand", StringComparison.Ordinal));
        (await DatabaseAssert.HistoryRowsCountAsync(connectionString)).Should().Be(0);
    }

    private static IHost CreateHost(
        string connectionString,
        bool generateMigrations,
        bool applyMigrations,
        bool customHistory,
        List<string> errors)
    {
        return TestHostBuilder.Create<ExistingMigrationDbContext>(connectionString, generateMigrations, applyMigrations)
            .ConfigureServices(services => services.AddDbContext<ExistingMigrationDbContext>(options =>
            {
                options.UseNpgsql(connectionString, postgres =>
                {
                    if (customHistory)
                    {
                        postgres.MigrationsHistoryTable("migration_history", "admin");
                    }
                });
                options.LogTo(errors.Add, LogLevel.Error);
            }))
            .Build();
    }
}
