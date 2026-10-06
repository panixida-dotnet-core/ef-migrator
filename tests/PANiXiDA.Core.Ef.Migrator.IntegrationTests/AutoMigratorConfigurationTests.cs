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

        using var host = builder.Build();

        async Task act()
        {
            await host.RunMigrationsAsync<ExistingMigrationDbContext>();
        }

        await FluentActions.Awaiting(act).Should().NotThrowAsync();
    }

    [Fact(DisplayName = "Throws when ProjectPath is missing for migration generation")]
    public async Task RunMigrationsAsync_WhenGenerationNeedsProjectPathAndItIsMissing_Throws()
    {
        const string connectionString = "Host=localhost;Database=unused;Username=unused;Password=unused";
        var builder = TestHostBuilder.Create<GeneratedMigrationDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: false);

        using var host = builder.Build();
        var act = async () => await host.RunMigrationsAsync<GeneratedMigrationDbContext>();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Не задан Ef:Contexts:GeneratedMigrationDbContext:ProjectPath.");
    }

    [Theory(DisplayName = "Reports missing generation settings before connecting to PostgreSQL")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunMigrationsAsync_WhenGenerationSettingsAreMissingAndApplyingEnabled_ValidatesBeforeConnecting(
        bool missingProjectPath)
    {
        const string connectionString = "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=1";
        using var project = new TempMigrationProject();
        using var host = TestHostBuilder.Create<GeneratedMigrationDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: true,
            projectPath: missingProjectPath ? null : project.ProjectPath,
            migrationsDirectory: missingProjectPath ? project.MigrationsDirectory : null)
            .Build();
        var missingSetting = missingProjectPath ? "ProjectPath" : "MigrationsDirectory";
        var act = async () => await host.RunMigrationsAsync<GeneratedMigrationDbContext>();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Не задан Ef:Contexts:GeneratedMigrationDbContext:{missingSetting}.");
    }

    [Fact(DisplayName = "Throws when the host is null")]
    public async Task RunMigrationsAsync_WhenHostIsNull_Throws()
    {
        IHost? host = null;

        var act = async () => await host!.RunMigrationsAsync<GeneratedMigrationDbContext>();

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Runs migration generation for multiple configured DbContexts")]
    public async Task RunMigrationsAsync_WithMultipleContexts_UsesContextGenerationConfiguration()
    {
        const string connectionString = "Host=localhost;Database=unused;Username=unused;Password=unused";
        using var project = new TempMigrationProject("GeneratedMigrations");
        var builder = TestHostBuilder.Create<ExistingMigrationDbContext, GeneratedMigrationDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: false,
            new Dictionary<string, string?>
            {
                ["Ef:Contexts:GeneratedMigrationDbContext:ProjectPath"] = project.ProjectPath,
                ["Ef:Contexts:GeneratedMigrationDbContext:MigrationsDirectory"] = project.MigrationsDirectory
            });

        using var host = builder.Build();

        await host.RunMigrationsAsync<ExistingMigrationDbContext>();
        await host.RunMigrationsAsync<GeneratedMigrationDbContext>();

        var generatedFiles = project.GetGeneratedFiles();

        generatedFiles.Should().HaveCount(3);
        generatedFiles.Should().ContainSingle(path =>
            path.EndsWith(".Designer.cs", StringComparison.Ordinal));
        generatedFiles.Should().ContainSingle(path =>
            path.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal));
        generatedFiles.Should().ContainSingle(path =>
            !path.EndsWith(".Designer.cs", StringComparison.Ordinal) &&
            !path.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal));
    }
}
