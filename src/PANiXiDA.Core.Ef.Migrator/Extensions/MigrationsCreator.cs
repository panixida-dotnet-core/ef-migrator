using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;

namespace PANiXiDA.Core.Ef.Migrator.Extensions;

internal static class MigrationsCreator
{
    internal static ScaffoldedMigration CreateAndSaveMigration(
        IServiceProvider designServiceProvider,
        MigrationsDifference difference,
        Type contextType,
        string rootNamespace,
        string subNamespace,
        string projectPath,
        string outputDir)
    {
        ArgumentNullException.ThrowIfNull(difference);

        var scaffolder = designServiceProvider.GetRequiredService<IMigrationsScaffolder>();
        var migrationsAssembly = designServiceProvider.GetRequiredService<IMigrationsAssembly>();
        var migrationsIdGenerator = designServiceProvider.GetRequiredService<IMigrationsIdGenerator>();
        var codeGeneratorSelector = designServiceProvider.GetRequiredService<IMigrationsCodeGeneratorSelector>();
        var codeGenerator = codeGeneratorSelector.Select("C#");

        var migrationName = MigrationsNameBuilder.BuildMigrationName(difference.UpOperations);
        var migrationId = migrationsIdGenerator.GenerateId(migrationName);
        var migrationNamespace = GetNamespace(rootNamespace, subNamespace);
        var snapshotName = contextType.Name + "ModelSnapshot";
        var previousMigrationId = migrationsAssembly.Migrations.LastOrDefault().Key;

        var scaffolded = new ScaffoldedMigration(
            codeGenerator.FileExtension,
            previousMigrationId,
            codeGenerator.GenerateMigration(
                migrationNamespace,
                migrationName,
                difference.UpOperations,
                difference.DownOperations),
            migrationId,
            codeGenerator.GenerateMetadata(
                migrationNamespace,
                contextType,
                migrationName,
                migrationId,
                difference.TargetModel),
            subNamespace,
            codeGenerator.GenerateSnapshot(
                migrationNamespace,
                contextType,
                snapshotName,
                difference.TargetModel),
            snapshotName,
            subNamespace);

        var outputDirectoryAbsolute = Path.Combine(projectPath, outputDir);
        scaffolder.Save(projectPath, scaffolded, outputDirectoryAbsolute);

        return scaffolded;
    }

    private static string GetNamespace(string rootNamespace, string subNamespace)
    {
        if (string.IsNullOrWhiteSpace(subNamespace))
        {
            return rootNamespace;
        }

        return rootNamespace + "." + subNamespace;
    }
}
