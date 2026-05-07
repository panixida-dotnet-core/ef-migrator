using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace PANiXiDA.Core.Ef.Migrator.Extensions;

internal sealed class MigrationsDifference
{
    public MigrationsDifference(
        IReadOnlyList<MigrationOperation> upOperations,
        IReadOnlyList<MigrationOperation> downOperations,
        IModel targetModel)
    {
        ArgumentNullException.ThrowIfNull(upOperations);
        ArgumentNullException.ThrowIfNull(downOperations);
        ArgumentNullException.ThrowIfNull(targetModel);

        UpOperations = upOperations;
        DownOperations = downOperations;
        TargetModel = targetModel;
    }

    public IReadOnlyList<MigrationOperation> UpOperations { get; }
    public IReadOnlyList<MigrationOperation> DownOperations { get; }
    public IModel TargetModel { get; }
    public int Count => UpOperations.Count;
}
