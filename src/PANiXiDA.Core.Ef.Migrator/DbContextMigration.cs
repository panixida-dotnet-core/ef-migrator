using Microsoft.EntityFrameworkCore;

namespace PANiXiDA.Core.Ef.Migrator;

/// <summary>
/// Describes a DbContext included in a multi-context migration run.
/// </summary>
public sealed class DbContextMigration
{
    private DbContextMigration(
        Type dbContextType,
        string configurationName)
    {
        DbContextType = dbContextType;
        ConfigurationName = configurationName;
    }

    /// <summary>
    /// Gets the DbContext type to migrate.
    /// </summary>
    public Type DbContextType { get; }

    /// <summary>
    /// Gets the context name used under the <c>Ef:Contexts</c> configuration section.
    /// </summary>
    public string ConfigurationName { get; }

    /// <summary>
    /// Creates a migration descriptor for the specified DbContext.
    /// </summary>
    /// <typeparam name="TContext">The Entity Framework Core context type to migrate.</typeparam>
    /// <param name="configurationName">
    /// The context name under <c>Ef:Contexts</c>. Defaults to the DbContext type name.
    /// </param>
    /// <returns>A migration descriptor for the specified DbContext.</returns>
    public static DbContextMigration For<TContext>(
        string? configurationName = null)
        where TContext : DbContext
    {
        var resolvedConfigurationName = configurationName ?? typeof(TContext).Name;

        if (string.IsNullOrWhiteSpace(resolvedConfigurationName))
        {
            throw new ArgumentException(
                "The migration configuration name must not be empty.",
                nameof(configurationName));
        }

        if (resolvedConfigurationName.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The migration configuration name must not contain ':'.",
                nameof(configurationName));
        }

        return new DbContextMigration(
            typeof(TContext),
            resolvedConfigurationName);
    }
}
