using Microsoft.EntityFrameworkCore;

using PANiXiDA.Core.Ef.Migrator.Extensions;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

public sealed class MigrationsCreatorTests
{
    [Fact(DisplayName = "Saves the scaffolded migration in the configured directory")]
    public void CreateAndSaveMigration_WhenDifferenceExists_WritesMigrationFiles()
    {
        using var db = new GeneratedMigrationDbContext(CreateOptions<GeneratedMigrationDbContext>());
        using var designServiceProvider = EfDesignServices.Create(db);
        using var project = new TempMigrationProject(Path.Combine("Nested", "Migrations"));
        var differences = MigrationsDifferenceProvider.GetDifferences(db, designServiceProvider);

        var migration = MigrationsCreator.CreateAndSaveMigration(
            designServiceProvider,
            differences,
            typeof(GeneratedMigrationDbContext),
            rootNamespace: "PANiXiDA.Core.Ef.Migrator.IntegrationTests",
            subNamespace: "Nested.Migrations",
            project.ProjectPath,
            project.MigrationsDirectory);

        var generatedFiles = project.GetGeneratedFiles();
        migration.MigrationId.Should().NotBeNullOrWhiteSpace();
        generatedFiles.Should().Contain(file => Path.GetFileName(file) == migration.MigrationId + ".cs");
        generatedFiles.Should().Contain(file => Path.GetFileName(file) == migration.MigrationId + ".Designer.cs");
        generatedFiles.Should().Contain(file => Path.GetFileName(file).EndsWith("ModelSnapshot.cs", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Saves the migration in the root namespace when the subNamespace is empty")]
    public void CreateAndSaveMigration_WhenSubNamespaceIsEmpty_UsesRootNamespace()
    {
        using var db = new GeneratedMigrationDbContext(CreateOptions<GeneratedMigrationDbContext>());
        using var designServiceProvider = EfDesignServices.Create(db);
        using var project = new TempMigrationProject();
        var differences = MigrationsDifferenceProvider.GetDifferences(db, designServiceProvider);

        var migration = MigrationsCreator.CreateAndSaveMigration(
            designServiceProvider,
            differences,
            typeof(GeneratedMigrationDbContext),
            rootNamespace: "PANiXiDA.Core.Ef.Migrator.IntegrationTests",
            subNamespace: string.Empty,
            project.ProjectPath,
            project.MigrationsDirectory);

        var migrationText = File.ReadAllText(Path.Combine(project.MigrationsPath, migration.MigrationId + ".cs"));
        migrationText.Should().Contain("namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests");
        migrationText.Should().NotContain("namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.");
    }

    [Fact(DisplayName = "Throws when the migration difference is null")]
    public void CreateAndSaveMigration_WhenDifferenceIsNull_Throws()
    {
        using var db = new GeneratedMigrationDbContext(CreateOptions<GeneratedMigrationDbContext>());
        using var designServiceProvider = EfDesignServices.Create(db);
        using var project = new TempMigrationProject();

        var act = () => MigrationsCreator.CreateAndSaveMigration(
            designServiceProvider,
            null!,
            typeof(GeneratedMigrationDbContext),
            rootNamespace: "PANiXiDA.Core.Ef.Migrator.IntegrationTests",
            subNamespace: string.Empty,
            project.ProjectPath,
            project.MigrationsDirectory);

        act.Should().Throw<ArgumentNullException>();
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
