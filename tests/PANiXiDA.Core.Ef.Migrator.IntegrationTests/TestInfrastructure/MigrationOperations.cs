using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;

internal static class MigrationOperations
{
    public static CreateTableOperation CreateTable(string tableName)
    {
        var operation = new CreateTableOperation
        {
            Name = tableName,
        };

        operation.Columns.Add(new AddColumnOperation
        {
            Name = "Id",
            Table = tableName,
            ClrType = typeof(int),
            ColumnType = "integer",
            IsNullable = false,
        });

        operation.PrimaryKey = new AddPrimaryKeyOperation
        {
            Name = "PK_" + tableName,
            Table = tableName,
            Columns = ["Id"],
        };

        return operation;
    }
}
