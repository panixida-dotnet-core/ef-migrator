using Npgsql;

namespace PANiXiDA.Core.Ef.Migrator.IntegrationTests.TestInfrastructure;

internal static class DatabaseAssert
{
    public static async Task<bool> TableExistsAsync(string connectionString, string tableName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select exists(
                select 1
                from information_schema.tables
                where table_schema = 'public'
                  and table_name = @tableName
            );
            """;
        command.Parameters.AddWithValue("tableName", tableName);

        var result = await command.ExecuteScalarAsync();

        return result is bool exists && exists;
    }

    public static async Task<bool> HistoryRowExistsAsync(string connectionString, string migrationId)
    {
        if (!await TableExistsAsync(connectionString, "__EFMigrationsHistory"))
        {
            return false;
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select count(*)
            from "__EFMigrationsHistory"
            where "MigrationId" = @migrationId;
            """;
        command.Parameters.AddWithValue("migrationId", migrationId);

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt64(result) > 0;
    }

    public static async Task<long> HistoryRowsCountAsync(string connectionString)
    {
        if (!await TableExistsAsync(connectionString, "__EFMigrationsHistory"))
        {
            return 0;
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select count(*)
            from "__EFMigrationsHistory";
            """;

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt64(result);
    }
}
