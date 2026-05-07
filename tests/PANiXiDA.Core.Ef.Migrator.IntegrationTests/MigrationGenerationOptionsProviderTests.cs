using Microsoft.Extensions.Configuration;

using PANiXiDA.Core.Ef.Migrator.Extensions;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;
using PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

public sealed class MigrationGenerationOptionsProviderTests
{
    [Fact(DisplayName = "Reads migration generation settings and computes the folder namespace")]
    public void Get_WhenConfigurationIsValid_ReturnsOptions()
    {
        using var project = new TempMigrationProject(Path.Combine("Data", "Migrations"));
        var configuration = CreateConfiguration(
            ("Ef:ProjectPath", project.ProjectPath),
            ("Ef:MigrationsDirectory", project.MigrationsDirectory));

        var options = MigrationGenerationOptionsProvider.Get<GeneratedMigrationDbContext>(configuration);

        options.ProjectPathAbsolute.Should().Be(Path.GetFullPath(project.ProjectPath));
        options.MigrationsDirectory.Should().Be(project.MigrationsDirectory);
        options.RootNamespace.Should().Be(typeof(GeneratedMigrationDbContext).Assembly.GetName().Name);
        options.MigrationsSubNamespace.Should().Be("Data.Migrations");
    }

    [Fact(DisplayName = "Throws when configuration is null")]
    public void Get_WhenConfigurationIsNull_Throws()
    {
        var act = () => MigrationGenerationOptionsProvider.Get<GeneratedMigrationDbContext>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Throws when ProjectPath is missing")]
    public void Get_WhenProjectPathIsMissing_Throws()
    {
        var configuration = CreateConfiguration(("Ef:MigrationsDirectory", "Migrations"));

        var act = () => MigrationGenerationOptionsProvider.Get<GeneratedMigrationDbContext>(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Не задан Ef:ProjectPath.");
    }

    [Fact(DisplayName = "Throws when MigrationsDirectory is missing")]
    public void Get_WhenMigrationsDirectoryIsMissing_Throws()
    {
        using var project = new TempMigrationProject();
        var configuration = CreateConfiguration(("Ef:ProjectPath", project.ProjectPath));

        var act = () => MigrationGenerationOptionsProvider.Get<GeneratedMigrationDbContext>(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Не задан Ef:MigrationsDirectory.");
    }

    [Fact(DisplayName = "Throws when the migrations directory leaves the project directory")]
    public void Get_WhenMigrationsDirectoryLeavesProject_Throws()
    {
        using var project = new TempMigrationProject();
        var configuration = CreateConfiguration(
            ("Ef:ProjectPath", project.ProjectPath),
            ("Ef:MigrationsDirectory", Path.Combine("..", "Outside")));

        var act = () => MigrationGenerationOptionsProvider.Get<GeneratedMigrationDbContext>(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Ef:MigrationsDirectory должен указывать на папку внутри проекта:*");
    }

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();
    }
}
