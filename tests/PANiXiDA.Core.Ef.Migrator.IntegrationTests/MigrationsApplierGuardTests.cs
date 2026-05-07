using PANiXiDA.Core.Ef.Migrator.Extensions;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

public sealed class MigrationsApplierGuardTests
{
    private const string MigrationId = "20260420123000_ApplyMigration";

    [Fact(DisplayName = "Throws when the DbContext is null while applying existing migrations")]
    public async Task ApplyPendingMigrationsAsync_WhenDbContextIsNull_Throws()
    {
        var act = async () => await MigrationsApplier.ApplyPendingMigrationsAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Throws when the migration operations list is null")]
    public async Task ApplyMigrationAsync_WhenDifferenceIsNull_Throws()
    {
        var act = async () => await MigrationsApplier.ApplyMigrationAsync(null!, null!, MigrationId);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Throws when the migration identifier is empty")]
    public async Task ApplyMigrationAsync_WhenMigrationIdIsEmpty_Throws()
    {
        var act = async () => await MigrationsApplier.ApplyMigrationAsync(null!, [], "");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("migrationId не может быть пустым*");
    }
}
