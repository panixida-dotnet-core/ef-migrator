using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

[Collection(nameof(PostgreSqlCollection))]
public sealed class AutoMigratorTests(PostgreSqlContainerFixture fixture)
{
    private const string ExistingMigrationId = "20260420120000_CreateExistingEntities";
    private const string PendingContextMigrationId = "20260420121000_CreateExistingEntitiesForPendingContext";

    [Fact(DisplayName = "Creates migration files in the configured directory without applying them")]
    public async Task RunMigrationsAsync_WhenGenerationEnabledAndApplyingDisabled_CreatesMigrationFilesOnly()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        using var project = new TempMigrationProject(Path.Combine("Data", "Migrations"));
        var builder = TestHostBuilder.Create<GeneratedMigrationDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: false,
            project.ProjectPath,
            project.MigrationsDirectory);

        using var host = builder.Build();

        await host.RunMigrationsAsync<GeneratedMigrationDbContext>();
        var generatedFiles = project.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty();
        generatedFiles.Should().OnlyContain(file =>
            string.Equals(Path.GetDirectoryName(file), project.MigrationsPath, StringComparison.OrdinalIgnoreCase));
        (await File.ReadAllTextAsync(
            GetGeneratedMigrationFile(project),
            TestContext.Current.CancellationToken)).Should().Contain("generated_entities");
        (await DatabaseAssert.TableExistsAsync(connectionString, "generated_entities")).Should().BeFalse();
        (await DatabaseAssert.HistoryRowsCountAsync(connectionString)).Should().Be(0);
    }

    [Fact(DisplayName = "Creates a migration and applies it to PostgreSQL")]
    public async Task RunMigrationsAsync_WhenGenerationAndApplyingEnabled_CreatesAndAppliesMigration()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        using var project = new TempMigrationProject();
        var builder = TestHostBuilder.Create<GeneratedMigrationDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: true,
            project.ProjectPath,
            project.MigrationsDirectory);

        using var host = builder.Build();

        await host.RunMigrationsAsync<GeneratedMigrationDbContext>();
        var migrationId = GetGeneratedMigrationId(project);
        project.GetGeneratedFiles().Should().NotBeEmpty();
        (await DatabaseAssert.TableExistsAsync(connectionString, "generated_entities")).Should().BeTrue();
        (await DatabaseAssert.HistoryRowExistsAsync(connectionString, migrationId)).Should().BeTrue();
    }

    [Fact(DisplayName = "Applies only existing migrations without generating new files")]
    public async Task RunMigrationsAsync_WhenOnlyApplyingEnabled_AppliesExistingMigrationsWithoutGeneratingNewFiles()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        using var project = new TempMigrationProject();
        var builder = TestHostBuilder.Create<PendingChangesDbContext>(
            connectionString,
            generateMigrations: false,
            applyMigrations: true,
            project.ProjectPath,
            project.MigrationsDirectory);

        using var host = builder.Build();

        await host.RunMigrationsAsync<PendingChangesDbContext>();
        project.GetGeneratedFiles().Should().BeEmpty();
        (await DatabaseAssert.TableExistsAsync(connectionString, "existing_entities")).Should().BeTrue();
        (await DatabaseAssert.TableExistsAsync(connectionString, "pending_entities")).Should().BeFalse();
        (await DatabaseAssert.HistoryRowExistsAsync(connectionString, PendingContextMigrationId)).Should().BeTrue();
    }

    [Fact(DisplayName = "Applies only the previous migration when generation is disabled and the model has a new entity")]
    public async Task RunMigrationsAsync_WhenPreviousMigrationExistsButGenerationDisabledAndApplyingEnabled_AppliesPreviousMigrationOnly()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        using var project = new TempMigrationProject();
        var builder = TestHostBuilder.Create<PendingChangesDbContext>(
            connectionString,
            generateMigrations: false,
            applyMigrations: true,
            project.ProjectPath,
            project.MigrationsDirectory);

        using var host = builder.Build();

        await host.RunMigrationsAsync<PendingChangesDbContext>();
        project.GetGeneratedFiles().Should().BeEmpty();
        (await DatabaseAssert.TableExistsAsync(connectionString, "existing_entities")).Should().BeTrue();
        (await DatabaseAssert.TableExistsAsync(connectionString, "pending_entities")).Should().BeFalse();
        (await DatabaseAssert.HistoryRowsCountAsync(connectionString)).Should().Be(1);
        (await DatabaseAssert.HistoryRowExistsAsync(connectionString, PendingContextMigrationId)).Should().BeTrue();
    }

    [Fact(DisplayName = "Does nothing with a pending previous migration when generation and applying are disabled")]
    public async Task RunMigrationsAsync_WhenPreviousMigrationExistsButGenerationAndApplyingDisabled_DoesNothing()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        using var project = new TempMigrationProject();
        var builder = TestHostBuilder.Create<PendingChangesDbContext>(
            connectionString,
            generateMigrations: false,
            applyMigrations: false,
            project.ProjectPath,
            project.MigrationsDirectory);

        using var host = builder.Build();

        await host.RunMigrationsAsync<PendingChangesDbContext>();
        project.GetGeneratedFiles().Should().BeEmpty();
        (await DatabaseAssert.TableExistsAsync(connectionString, "existing_entities")).Should().BeFalse();
        (await DatabaseAssert.TableExistsAsync(connectionString, "pending_entities")).Should().BeFalse();
        (await DatabaseAssert.HistoryRowsCountAsync(connectionString)).Should().Be(0);
    }

    [Fact(DisplayName = "Creates a migration only for the new entity when the previous migration is in the snapshot but not applied")]
    public async Task RunMigrationsAsync_WhenPreviousMigrationExistsButIsNotAppliedAndApplyingDisabled_CreatesOnlyNewEntityMigration()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        using var project = new TempMigrationProject();
        var builder = TestHostBuilder.Create<PendingChangesDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: false,
            project.ProjectPath,
            project.MigrationsDirectory);

        using var host = builder.Build();

        await host.RunMigrationsAsync<PendingChangesDbContext>();
        var migrationText = await File.ReadAllTextAsync(
            GetGeneratedMigrationFile(project),
            TestContext.Current.CancellationToken);
        migrationText.Should().Contain("pending_entities");
        migrationText.Should().NotContain("existing_entities");
        (await DatabaseAssert.TableExistsAsync(connectionString, "existing_entities")).Should().BeFalse();
        (await DatabaseAssert.TableExistsAsync(connectionString, "pending_entities")).Should().BeFalse();
        (await DatabaseAssert.HistoryRowsCountAsync(connectionString)).Should().Be(0);
    }

    [Fact(DisplayName = "Applies the previous snapshot migration before applying the generated new entity migration")]
    public async Task RunMigrationsAsync_WhenPreviousMigrationExistsButIsNotAppliedAndApplyingEnabled_AppliesPreviousAndNewMigrations()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        using var project = new TempMigrationProject();
        var builder = TestHostBuilder.Create<PendingChangesDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: true,
            project.ProjectPath,
            project.MigrationsDirectory);

        using var host = builder.Build();

        await host.RunMigrationsAsync<PendingChangesDbContext>();
        var migrationId = GetGeneratedMigrationId(project);
        var migrationText = await File.ReadAllTextAsync(
            GetGeneratedMigrationFile(project),
            TestContext.Current.CancellationToken);
        migrationText.Should().Contain("pending_entities");
        migrationText.Should().NotContain("existing_entities");
        (await DatabaseAssert.TableExistsAsync(connectionString, "existing_entities")).Should().BeTrue();
        (await DatabaseAssert.TableExistsAsync(connectionString, "pending_entities")).Should().BeTrue();
        (await DatabaseAssert.HistoryRowExistsAsync(connectionString, PendingContextMigrationId)).Should().BeTrue();
        (await DatabaseAssert.HistoryRowExistsAsync(connectionString, migrationId)).Should().BeTrue();
    }

    [Fact(DisplayName = "Does nothing when migration generation and applying are disabled")]
    public async Task RunMigrationsAsync_WhenGenerationAndApplyingDisabled_DoesNothing()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        using var project = new TempMigrationProject();
        var builder = TestHostBuilder.Create<GeneratedMigrationDbContext>(
            connectionString,
            generateMigrations: false,
            applyMigrations: false,
            project.ProjectPath,
            project.MigrationsDirectory);

        using var host = builder.Build();

        await host.RunMigrationsAsync<GeneratedMigrationDbContext>();
        project.GetGeneratedFiles().Should().BeEmpty();
        (await DatabaseAssert.TableExistsAsync(connectionString, "generated_entities")).Should().BeFalse();
        (await DatabaseAssert.HistoryRowsCountAsync(connectionString)).Should().Be(0);
    }

    [Fact(DisplayName = "Applies compiled migrations without creating new files when the model has no differences")]
    public async Task RunMigrationsAsync_WhenNoModelDifferencesExist_AppliesCompiledMigrations()
    {
        var connectionString = await fixture.CreateConnectionStringAsync();
        using var project = new TempMigrationProject();
        var builder = TestHostBuilder.Create<ExistingMigrationDbContext>(
            connectionString,
            generateMigrations: true,
            applyMigrations: true,
            project.ProjectPath,
            project.MigrationsDirectory);

        using var host = builder.Build();

        await host.RunMigrationsAsync<ExistingMigrationDbContext>();
        project.GetGeneratedFiles().Should().BeEmpty();
        (await DatabaseAssert.TableExistsAsync(connectionString, "existing_entities")).Should().BeTrue();
        (await DatabaseAssert.HistoryRowExistsAsync(connectionString, ExistingMigrationId)).Should().BeTrue();
    }

    private static string GetGeneratedMigrationId(TempMigrationProject project)
    {
        return Path.GetFileNameWithoutExtension(GetGeneratedMigrationFile(project));
    }

    private static string GetGeneratedMigrationFile(TempMigrationProject project)
    {
        return project.GetGeneratedFiles().Single(file =>
        {
            var fileName = Path.GetFileName(file);

            return !fileName.EndsWith(".Designer.cs", StringComparison.Ordinal)
                && !fileName.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal);
        });
    }
}
