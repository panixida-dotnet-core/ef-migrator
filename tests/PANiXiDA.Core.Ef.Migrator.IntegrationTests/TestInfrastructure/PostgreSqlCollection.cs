namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;

[CollectionDefinition(nameof(PostgreSqlCollection))]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
}
