using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using PANiXiDA.Core.Ef.Migrator.Options;

namespace PANiXiDA.Core.Ef.Migrator.Extensions;

internal static class MigrationGenerationOptionsProvider
{
    public static MigrationGenerationOptions Get<TContext>(IConfiguration configuration)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var projectPath = configuration["Ef:ProjectPath"]
            ?? throw new InvalidOperationException("Не задан Ef:ProjectPath.");

        var migrationsDirectory = configuration["Ef:MigrationsDirectory"]
            ?? throw new InvalidOperationException("Не задан Ef:MigrationsDirectory.");

        var projectPathAbsolute = Path.GetFullPath(projectPath);
        var migrationsDirectoryAbsolute = GetMigrationsDirectoryAbsolutePath(
            projectPathAbsolute,
            migrationsDirectory);

        var rootNamespace = typeof(TContext).Assembly.GetName().Name!;

        var migrationsSubNamespace = GetMigrationsSubNamespace(
            projectPathAbsolute,
            migrationsDirectoryAbsolute);

        return new MigrationGenerationOptions(
            projectPathAbsolute,
            migrationsDirectory,
            rootNamespace,
            migrationsSubNamespace);
    }

    private static string GetMigrationsDirectoryAbsolutePath(
        string projectPathAbsolute,
        string migrationsDirectoryRelative)
    {
        var migrationsDirectoryAbsolute = Path.GetFullPath(
            Path.Combine(projectPathAbsolute, migrationsDirectoryRelative));

        if (migrationsDirectoryAbsolute.StartsWith(
                projectPathAbsolute + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                migrationsDirectoryAbsolute,
                projectPathAbsolute,
                StringComparison.OrdinalIgnoreCase))
        {
            return migrationsDirectoryAbsolute;
        }

        throw new InvalidOperationException(
            $"Ef:MigrationsDirectory должен указывать на папку внутри проекта: {migrationsDirectoryRelative}");
    }

    private static string GetMigrationsSubNamespace(
        string projectPathAbsolute,
        string migrationsDirectoryAbsolute)
    {
        var migrationsDirectoryRelative = Path.GetRelativePath(
            projectPathAbsolute,
            migrationsDirectoryAbsolute);

        var namespaceParts = migrationsDirectoryRelative
            .Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(part => part != ".");

        return string.Join(".", namespaceParts);
    }
}
