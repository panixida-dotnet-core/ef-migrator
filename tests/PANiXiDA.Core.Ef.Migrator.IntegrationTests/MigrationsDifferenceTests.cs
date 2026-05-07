using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using PANiXiDA.Core.Ef.Migrator.Extensions;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

public sealed class MigrationsDifferenceTests
{
    [Fact(DisplayName = "Stores migration operations and target model")]
    public void Constructor_WhenValuesAreValid_SetsProperties()
    {
        using var db = new GeneratedMigrationDbContext(CreateOptions());
        IReadOnlyList<MigrationOperation> upOperations = [new SqlOperation { Sql = "select 1;" }];
        IReadOnlyList<MigrationOperation> downOperations = [new SqlOperation { Sql = "select 2;" }];
        var targetModel = db.Model;

        var difference = new MigrationsDifference(upOperations, downOperations, targetModel);

        difference.UpOperations.Should().BeSameAs(upOperations);
        difference.DownOperations.Should().BeSameAs(downOperations);
        difference.TargetModel.Should().BeSameAs(targetModel);
        difference.Count.Should().Be(1);
    }

    [Fact(DisplayName = "Throws when up operations are null")]
    public void Constructor_WhenUpOperationsAreNull_Throws()
    {
        using var db = new GeneratedMigrationDbContext(CreateOptions());

        var act = () => new MigrationsDifference(null!, [], db.Model);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Throws when down operations are null")]
    public void Constructor_WhenDownOperationsAreNull_Throws()
    {
        using var db = new GeneratedMigrationDbContext(CreateOptions());

        var act = () => new MigrationsDifference([], null!, db.Model);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Throws when target model is null")]
    public void Constructor_WhenTargetModelIsNull_Throws()
    {
        var act = () => new MigrationsDifference([], [], null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static DbContextOptions<GeneratedMigrationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<GeneratedMigrationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
    }
}
