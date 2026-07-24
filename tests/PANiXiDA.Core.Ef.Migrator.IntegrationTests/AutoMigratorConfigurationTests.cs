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
                ["Ef:Contexts:Generated:ProjectPath"] = project.ProjectPath,
                ["Ef:Contexts:Generated:MigrationsDirectory"] = project.MigrationsDirectory
            });

        using var host = await builder.RunMigrationsAsync(
            DbContextMigration.For<ExistingMigrationDbContext>("Existing"),
            DbContextMigration.For<GeneratedMigrationDbContext>("Generated"));

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

    [Fact(DisplayName = "Rejects duplicate DbContexts in a multi-context migration run")]
    public async Task RunMigrationsAsync_WithDuplicateContexts_Throws()
    {
        const string connectionString = "Host=localhost;Database=unused;Username=unused;Password=unused";
        var builder = TestHostBuilder.Create<GeneratedMigrationDbContext>(
            connectionString,
            generateMigrations: false,
            applyMigrations: false);

        var act = async () => await builder.RunMigrationsAsync(
            DbContextMigration.For<GeneratedMigrationDbContext>("First"),
            DbContextMigration.For<GeneratedMigrationDbContext>("Second"));

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("DbContext '*' is registered more than once. (Parameter 'contexts')");
    }
}
