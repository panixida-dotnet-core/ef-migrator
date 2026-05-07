using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace PANiXiDA.Core.Ef.Migrator.Extensions;

internal static class MigrationsDifferenceProvider
{
    internal static MigrationsDifference GetDifferences<TContext>(
        TContext db,
        IServiceProvider designServiceProvider) where TContext : DbContext
    {
        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var modelDiffer = db.GetService<IMigrationsModelDiffer>();

        IRelationalModel? sourceRelational = null;
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;
        if (snapshotModel is not null)
        {
            var runtimeInitializer = designServiceProvider.GetRequiredService<IModelRuntimeInitializer>();
            var sourceConceptual = runtimeInitializer.Initialize(snapshotModel);
            sourceRelational = sourceConceptual.GetRelationalModel();
        }

        var designTimeModel = db.GetService<IDesignTimeModel>().Model;
        var targetRelational = designTimeModel.GetRelationalModel();

        var upOperations = modelDiffer.GetDifferences(sourceRelational, targetRelational);
        var downOperations = modelDiffer.GetDifferences(targetRelational, sourceRelational);

        return new MigrationsDifference(upOperations, downOperations, designTimeModel);
    }
}
