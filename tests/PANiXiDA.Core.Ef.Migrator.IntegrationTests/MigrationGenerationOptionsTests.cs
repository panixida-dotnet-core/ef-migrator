using PANiXiDA.Core.Ef.Migrator.Options;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

public sealed class MigrationGenerationOptionsTests
{
    [Fact(DisplayName = "Stores the provided migration generation settings")]
    public void Constructor_WhenValuesAreValid_SetsProperties()
    {
        var options = new MigrationGenerationOptions(
            "C:\\Project",
            "Migrations",
            "RootNamespace",
            "Data.Migrations");

        options.ProjectPathAbsolute.Should().Be("C:\\Project");
        options.MigrationsDirectory.Should().Be("Migrations");
        options.RootNamespace.Should().Be("RootNamespace");
        options.MigrationsSubNamespace.Should().Be("Data.Migrations");
    }

    [Theory(DisplayName = "Throws when a required value is empty")]
    [InlineData("", "Migrations", "RootNamespace")]
    [InlineData("C:\\Project", "", "RootNamespace")]
    [InlineData("C:\\Project", "Migrations", "")]
    public void Constructor_WhenRequiredValueIsEmpty_Throws(
        string projectPathAbsolute,
        string migrationsDirectory,
        string rootNamespace)
    {
        var act = () => new MigrationGenerationOptions(
            projectPathAbsolute,
            migrationsDirectory,
            rootNamespace,
            "Data.Migrations");

        act.Should().Throw<ArgumentException>();
    }
}
