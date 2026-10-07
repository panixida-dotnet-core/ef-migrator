using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Npgsql;

using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

[Collection(nameof(PostgreSqlCollection))]
public sealed class MigrationHistoryPermissionsTests(PostgreSqlContainerFixture fixture)
{
    [Theory(DisplayName = "Completes an apply-only no-op with read-only migration history access")]
    [InlineData(null, null, null)]
    [InlineData("migration_history", "admin", null)]
    [InlineData("Migration History", "History Schema", null)]
    [InlineData(null, null, "history_path")]
    public async Task RunMigrationsAsync_WhenHistoryIsCurrentAndSchemaCreateIsDenied_DoesNotExecuteDdl(
        string? historyTable,
        string? historySchema,
        string? searchPath)
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        if (searchPath is not null)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = new NpgsqlCommand($"CREATE SCHEMA {searchPath}", connection);
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            connectionString = new NpgsqlConnectionStringBuilder(connectionString)
            {
                SearchPath = searchPath,
            }.ConnectionString;
        }

        using (var host = CreateHost(connectionString, historyTable, historySchema, []))
        {
            await host.RunMigrationsAsync<ExistingMigrationDbContext>();
        }

        var readOnlyConnection = await CreateHistoryReaderAsync(
            connectionString, historyTable, historySchema ?? searchPath ?? "public", grantHistorySelect: true);
        var errors = new List<string>();
        using var readOnlyHost = CreateHost(readOnlyConnection, historyTable, historySchema, errors);

        await readOnlyHost.RunMigrationsAsync<ExistingMigrationDbContext>();

        using var scope = readOnlyHost.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExistingMigrationDbContext>();
        (await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Should().ContainSingle();
        errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Preserves schema permission errors when migration history is absent")]
    public async Task RunMigrationsAsync_WhenHistoryIsMissingAndSchemaCreateIsDenied_Throws()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        var readOnlyConnection = await CreateHistoryReaderAsync(
            connectionString, historyTable: null, historySchema: "public", grantHistorySelect: false);
        var errors = new List<string>();
        using var host = CreateHost(readOnlyConnection, null, null, errors);
        var act = async () => await host.RunMigrationsAsync<ExistingMigrationDbContext>();

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        errors.Should().Contain(message => message.Contains("Failed executing DbCommand", StringComparison.Ordinal));
        (await DatabaseAssert.TableExistsAsync(connectionString, "__EFMigrationsHistory")).Should().BeFalse();
    }

    private static IHost CreateHost(
        string connectionString, string? historyTable, string? historySchema, List<string> errors)
    {
        return TestHostBuilder.Create<ExistingMigrationDbContext>(connectionString, false, true)
            .ConfigureServices(services => services.AddDbContext<ExistingMigrationDbContext>(options =>
            {
                options.UseNpgsql(connectionString, postgres =>
                {
                    if (historyTable is not null)
                    {
                        postgres.MigrationsHistoryTable(historyTable, historySchema);
                    }
                });
                options.LogTo(errors.Add, LogLevel.Error);
            }))
            .Build();
    }

    private static async Task<string> CreateHistoryReaderAsync(
        string connectionString, string? historyTable, string historySchema, bool grantHistorySelect)
    {
        var role = "history_reader_" + Guid.NewGuid().ToString("N");
        const string password = "synthetic-history-reader-password";
        using var commandBuilder = new NpgsqlCommandBuilder();
        var schemaIdentifier = commandBuilder.QuoteIdentifier(historySchema);
        var tableIdentifier = commandBuilder.QuoteIdentifier(historyTable ?? "__EFMigrationsHistory");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand($"""
            CREATE ROLE {role} LOGIN PASSWORD '{password}';
            REVOKE CREATE ON SCHEMA {schemaIdentifier} FROM PUBLIC;
            GRANT USAGE ON SCHEMA {schemaIdentifier} TO {role};
            """, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        if (grantHistorySelect)
        {
            command.CommandText = $"GRANT SELECT ON TABLE {schemaIdentifier}.{tableIdentifier} TO {role}";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        return new NpgsqlConnectionStringBuilder(connectionString)
        {
            Username = role,
            Password = password,
        }.ConnectionString;
    }
}
