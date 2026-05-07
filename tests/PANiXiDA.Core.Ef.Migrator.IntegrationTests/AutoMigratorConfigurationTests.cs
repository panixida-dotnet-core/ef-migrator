using Microsoft.Extensions.Hosting;

using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

public sealed class AutoMigratorConfigurationTests
{
    [Fact(DisplayName = "Does not require migration folder settings when the model is unchanged and applying is disabled")]
    public async Task RunMigrationsAsync_WhenNoModelDifferencesExistAndApplyingDisabled_DoesNotReadGenerationOptions()
    {
        const string connectionString = "Host=localhost;Database=unused;Username=unused;Password=unused";
        var builder = TestHostBuilder.Create<ExistingMigrationDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: false);

        using var host = await builder.RunMigrationsAsync<ExistingMigrationDbContext>();

        host.Should().NotBeNull();
    }

    [Fact(DisplayName = "Throws when ProjectPath is missing for migration generation")]
    public async Task RunMigrationsAsync_WhenGenerationNeedsProjectPathAndItIsMissing_Throws()
    {
        const string connectionString = "Host=localhost;Database=unused;Username=unused;Password=unused";
        var builder = TestHostBuilder.Create<GeneratedMigrationDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: false);

        var act = async () => await builder.RunMigrationsAsync<GeneratedMigrationDbContext>();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Не задан Ef:ProjectPath.");
    }

    [Fact(DisplayName = "Throws when the host builder is null")]
    public async Task RunMigrationsAsync_WhenBuilderIsNull_Throws()
    {
        IHostBuilder? builder = null;

        var act = async () => await builder!.RunMigrationsAsync<GeneratedMigrationDbContext>();

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
