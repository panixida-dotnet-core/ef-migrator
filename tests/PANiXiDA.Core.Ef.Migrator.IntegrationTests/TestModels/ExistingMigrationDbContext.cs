using Microsoft.EntityFrameworkCore;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

internal sealed class ExistingMigrationDbContext(DbContextOptions<ExistingMigrationDbContext> options) : DbContext(options)
{
    public DbSet<ExistingEntity> ExistingEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        TestModelBuilder.ConfigureExistingModel(modelBuilder, includeProductVersion: false);
    }
}
