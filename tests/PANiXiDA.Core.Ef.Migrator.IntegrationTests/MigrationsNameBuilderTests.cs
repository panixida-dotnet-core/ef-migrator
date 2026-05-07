using Microsoft.EntityFrameworkCore.Migrations.Operations;

using PANiXiDA.Core.Ef.Migrator.Extensions;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests;

public sealed class MigrationsNameBuilderTests
{
    [Fact(DisplayName = "Builds a readable name for each supported operation type")]
    public void BuildMigrationName_WhenOperationTypeIsKnown_UsesReadableOperationPart()
    {
        var cases = new (MigrationOperation Operation, string ExpectedPart)[]
        {
            (new AddColumnOperation { Name = "Name", Table = "users" }, "Add_Column_Name_To_users_Table"),
            (new DropColumnOperation { Name = "OldName", Table = "users" }, "Delete_Column_OldName_From_users_Table"),
            (new AlterColumnOperation { Name = "Age", Table = "users", ClrType = typeof(int), ColumnType = "integer" }, "Alter_Column_Age_In_users_Table"),
            (new RenameColumnOperation { Name = "OldName", NewName = "NewName", Table = "users" }, "Rename_Column_OldName_In_users_Table_To_NewName"),
            (new CreateTableOperation { Name = "orders" }, "Add_orders_Table"),
            (new DropTableOperation { Name = "old_orders" }, "Delete_old_orders_Table"),
            (new RenameTableOperation { Name = "old_users", NewName = "new_users" }, "Rename_Table_old_users_To_new_users"),
            (new CreateIndexOperation { Name = "IX_users_name", Table = "users" }, "Add_IX_users_name_Index_To_users_Table"),
            (new DropIndexOperation { Name = "IX_users_old_name", Table = "users" }, "Delete_IX_users_old_name_Index_From_users_Table"),
            (new RenameIndexOperation { Name = "IX_old", NewName = "IX_new", Table = "users" }, "Rename_Index_IX_old_On_users_Table_To_IX_new"),
            (new AddForeignKeyOperation { Name = "FK_orders_users", Table = "orders" }, "Add_FK_FK_orders_users_To_orders_Table"),
            (new DropForeignKeyOperation { Name = "FK_orders_users", Table = "orders" }, "Drop_FK_FK_orders_users_From_orders_Table"),
            (new AddPrimaryKeyOperation { Name = "PK_users", Table = "users" }, "Add_PK_PK_users_To_users_Table"),
            (new DropPrimaryKeyOperation { Name = "PK_users", Table = "users" }, "Drop_PK_PK_users_From_users_Table"),
            (new AddUniqueConstraintOperation { Name = "AK_users_email", Table = "users" }, "Add_UC_AK_users_email_To_users_Table"),
            (new DropUniqueConstraintOperation { Name = "AK_users_email", Table = "users" }, "Drop_UC_AK_users_email_From_users_Table"),
            (new SqlOperation { Sql = "select 1;" }, "Sql"),
        };

        foreach (var (operation, expectedPart) in cases)
        {
            var migrationName = MigrationsNameBuilder.BuildMigrationName([operation]);

            migrationName.Should().Contain(expectedPart);
        }
    }

    [Fact(DisplayName = "Removes invalid characters and duplicate name parts")]
    public void BuildMigrationName_WhenOperationNamesContainInvalidCharactersAndDuplicates_CleansName()
    {
        var duplicateOperation = new AddColumnOperation { Name = "Bad//Name", Table = "users" };

        var migrationName = MigrationsNameBuilder.BuildMigrationName([duplicateOperation, duplicateOperation]);

        migrationName.Should().Contain("Add_Column_Bad_Name_To_users_Table");
        migrationName.Should().NotContain("/");
        migrationName.IndexOf("Add_Column_Bad_Name_To_users_Table", StringComparison.Ordinal)
            .Should()
            .Be(migrationName.LastIndexOf("Add_Column_Bad_Name_To_users_Table", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Truncates an overlong name and appends a stable hash")]
    public void BuildMigrationName_WhenNameIsTooLong_TruncatesNameAndAddsHash()
    {
        var operation = new AddColumnOperation
        {
            Name = new string('A', 240),
            Table = "users",
        };

        var migrationName = MigrationsNameBuilder.BuildMigrationName([operation]);

        migrationName.Length.Should().BeLessThanOrEqualTo(134);
        migrationName.Should().MatchRegex("_[A-F0-9]{8}$");
    }

    [Fact(DisplayName = "Removes trailing dots and spaces from operation name parts")]
    public void BuildMigrationName_WhenOperationNamesHaveTrailingDotsAndSpaces_CleansName()
    {
        var operation = new RenameTableOperation { Name = "users", NewName = "renamed_users. " };

        var migrationName = MigrationsNameBuilder.BuildMigrationName([operation]);

        migrationName.Should().Contain("Rename_Table_users_To_renamed_users");
        migrationName.Should().EndWith("renamed_users");
    }
}
