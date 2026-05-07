using Microsoft.EntityFrameworkCore;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

internal sealed class PendingChangesDbContext(DbContextOptions<PendingChangesDbContext> options) : DbContext(options)
{
    public DbSet<ExistingEntity> ExistingEntities { get; set; } = null!;
    public DbSet<PendingEntity> PendingEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        TestModelBuilder.ConfigurePendingChangesModel(modelBuilder, includeProductVersion: false);
    }
}
