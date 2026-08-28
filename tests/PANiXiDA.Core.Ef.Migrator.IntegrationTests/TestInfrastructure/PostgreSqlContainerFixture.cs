using Npgsql;

using Testcontainers.PostgreSql;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container;

    public PostgreSqlContainerFixture()
    {
        container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("migrator_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }

    public async Task<string> CreateConnectionStringAsync()
    {
        var databaseName = "db_" + Guid.NewGuid().ToString("N");

        await container.ExecScriptAsync($"CREATE DATABASE {databaseName};");

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = databaseName,
        };

        return connectionStringBuilder.ConnectionString;
    }
}
