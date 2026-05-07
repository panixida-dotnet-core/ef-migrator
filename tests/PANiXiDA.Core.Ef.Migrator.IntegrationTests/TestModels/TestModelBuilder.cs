using Microsoft.EntityFrameworkCore;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestModels;

internal static class TestModelBuilder
{
    public static void ConfigureGeneratedModel(ModelBuilder modelBuilder, bool includeProductVersion)
    {
        ConfigureProviderAnnotations(modelBuilder, includeProductVersion);

        modelBuilder.Entity<GeneratedEntity>(entity =>
        {
            entity.ToTable("generated_entities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnType("integer");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(128).HasColumnType("character varying(128)");
        });
    }

    public static void ConfigureExistingModel(ModelBuilder modelBuilder, bool includeProductVersion)
    {
        ConfigureProviderAnnotations(modelBuilder, includeProductVersion);
        ConfigureExistingEntity(modelBuilder);
    }

    public static void ConfigurePendingChangesModel(ModelBuilder modelBuilder, bool includeProductVersion)
    {
        ConfigureProviderAnnotations(modelBuilder, includeProductVersion);
        ConfigureExistingEntity(modelBuilder);

        modelBuilder.Entity<PendingEntity>(entity =>
        {
            entity.ToTable("pending_entities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnType("integer");
            entity.Property(x => x.Title).IsRequired().HasMaxLength(128).HasColumnType("character varying(128)");
        });
    }

    private static void ConfigureExistingEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExistingEntity>(entity =>
        {
            entity.ToTable("existing_entities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnType("integer");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(128).HasColumnType("character varying(128)");
        });
    }

    private static void ConfigureProviderAnnotations(ModelBuilder modelBuilder, bool includeProductVersion)
    {
#pragma warning disable 612, 618
        if (includeProductVersion)
        {
            modelBuilder.HasAnnotation("ProductVersion", "10.0.5");
        }

        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 63);
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
#pragma warning restore 612, 618
    }
}
