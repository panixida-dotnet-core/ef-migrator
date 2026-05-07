namespace PANiXiDA.Core.Ef.Migrator.Options;

internal sealed class MigrationGenerationOptions
{
    public MigrationGenerationOptions(
        string projectPathAbsolute,
        string migrationsDirectory,
        string rootNamespace,
        string migrationsSubNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPathAbsolute);
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationsDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootNamespace);

        ProjectPathAbsolute = projectPathAbsolute;
        MigrationsDirectory = migrationsDirectory;
        RootNamespace = rootNamespace;
        MigrationsSubNamespace = migrationsSubNamespace;
    }

    public string ProjectPathAbsolute { get; }
    public string MigrationsDirectory { get; }
    public string RootNamespace { get; }
    public string MigrationsSubNamespace { get; }
}
