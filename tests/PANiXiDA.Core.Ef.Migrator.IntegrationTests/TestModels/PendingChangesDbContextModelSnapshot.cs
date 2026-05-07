using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

[DbContext(typeof(PendingChangesDbContext))]
internal sealed class PendingChangesDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        TestModelBuilder.ConfigureExistingModel(modelBuilder, includeProductVersion: true);
    }
}
