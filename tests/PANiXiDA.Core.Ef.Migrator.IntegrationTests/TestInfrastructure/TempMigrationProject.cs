namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;

internal sealed class TempMigrationProject : IDisposable
{
    public TempMigrationProject(string migrationsDirectory = "GeneratedMigrations")
    {
        ProjectPath = Path.Combine(
            Path.GetTempPath(),
            "PANiXiDA.Core.Ef.Migrator.IntegrationTests",
            Guid.NewGuid().ToString("N"));
        MigrationsDirectory = migrationsDirectory;

        Directory.CreateDirectory(ProjectPath);
    }

    public string ProjectPath { get; }
    public string MigrationsDirectory { get; }

    public string MigrationsPath
    {
        get
        {
            return Path.Combine(ProjectPath, MigrationsDirectory);
        }
    }

    public IReadOnlyList<string> GetGeneratedFiles()
    {
        if (!Directory.Exists(MigrationsPath))
        {
            return [];
        }

        return [
            .. Directory
                .GetFiles(MigrationsPath, "*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(file => file, StringComparer.Ordinal)
        ];
    }

    public void Dispose()
    {
        if (Directory.Exists(ProjectPath))
        {
            Directory.Delete(ProjectPath, recursive: true);
        }
    }
}
