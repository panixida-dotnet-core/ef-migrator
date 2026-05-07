using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

[DbContext(typeof(PendingChangesDbContext))]
[Migration("20260420121000_CreateExistingEntitiesForPendingContext")]
public sealed class CreateExistingEntitiesForPendingContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "existing_entities",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_existing_entities", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "existing_entities");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        TestModelBuilder.ConfigureExistingModel(modelBuilder, includeProductVersion: true);
    }
}
