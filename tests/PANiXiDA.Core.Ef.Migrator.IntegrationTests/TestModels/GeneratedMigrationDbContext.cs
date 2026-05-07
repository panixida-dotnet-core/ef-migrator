using Microsoft.EntityFrameworkCore;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

internal sealed class GeneratedMigrationDbContext(DbContextOptions<GeneratedMigrationDbContext> options) : DbContext(options)
{
    public DbSet<GeneratedEntity> GeneratedEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        TestModelBuilder.ConfigureGeneratedModel(modelBuilder, includeProductVersion: false);
    }
}
