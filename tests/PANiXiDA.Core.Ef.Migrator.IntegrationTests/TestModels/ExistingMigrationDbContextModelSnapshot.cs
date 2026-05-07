using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

[DbContext(typeof(ExistingMigrationDbContext))]
internal sealed class ExistingMigrationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        TestModelBuilder.ConfigureExistingModel(modelBuilder, includeProductVersion: true);
    }
}
