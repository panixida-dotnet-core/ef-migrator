using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

using Npgsql.EntityFrameworkCore.PostgreSQL.Design.Internal;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;

internal static class EfDesignServices
{
    public static ServiceProvider Create(DbContext db)
    {
        var services = new ServiceCollection()
            .AddEntityFrameworkDesignTimeServices()
            .AddDbContextDesignTimeServices(db);

#pragma warning disable EF1001
        new NpgsqlDesignTimeServices().ConfigureDesignTimeServices(services);
#pragma warning restore EF1001

        return services.BuildServiceProvider();
    }
}
