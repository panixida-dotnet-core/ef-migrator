using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using PANiXiDA.Core.Ef.Migrator.Extensions;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

public sealed class MigrationsDifferenceProviderTests
{
    [Fact(DisplayName = "Returns table creation operations when the snapshot is missing")]
    public void GetDifferences_WhenSnapshotIsMissing_ReturnsDifferencesFromEmptyModel()
    {
        using var db = new GeneratedMigrationDbContext(CreateOptions<GeneratedMigrationDbContext>());
        using var designServiceProvider = EfDesignServices.Create(db);

        var differences = MigrationsDifferenceProvider.GetDifferences(db, designServiceProvider);

        differences.UpOperations.OfType<CreateTableOperation>().Should().Contain(operation => operation.Name == "generated_entities");
    }

    [Fact(DisplayName = "Returns an empty list when the snapshot matches the model")]
    public void GetDifferences_WhenSnapshotMatchesModel_ReturnsEmptyList()
    {
        using var db = new ExistingMigrationDbContext(CreateOptions<ExistingMigrationDbContext>());
        using var designServiceProvider = EfDesignServices.Create(db);

        var differences = MigrationsDifferenceProvider.GetDifferences(db, designServiceProvider);

        differences.Count.Should().Be(0);
        differences.DownOperations.Should().BeEmpty();
    }

    [Fact(DisplayName = "Returns operations only for new changes when the snapshot already contains the previous entity")]
    public void GetDifferences_WhenSnapshotContainsPreviousEntity_ReturnsOnlyNewEntity()
    {
        using var db = new PendingChangesDbContext(CreateOptions<PendingChangesDbContext>());
        using var designServiceProvider = EfDesignServices.Create(db);

        var differences = MigrationsDifferenceProvider.GetDifferences(db, designServiceProvider);

        differences.UpOperations.OfType<CreateTableOperation>().Should().ContainSingle(operation => operation.Name == "pending_entities");
        differences.UpOperations.OfType<CreateTableOperation>().Should().NotContain(operation => operation.Name == "existing_entities");
    }

    private static DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(
                "Host=localhost;Database=unused;Username=unused;Password=unused",
                npgsql => npgsql.MigrationsAssembly(typeof(TContext).Assembly.GetName().Name))
            .Options;
    }
}
