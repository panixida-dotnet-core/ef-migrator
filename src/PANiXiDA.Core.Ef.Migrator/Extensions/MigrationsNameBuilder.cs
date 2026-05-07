using Microsoft.EntityFrameworkCore.Migrations.Operations;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PANiXiDA.Core.Ef.Migrator.Extensions;

internal static partial class MigrationsNameBuilder
{
    private const int MaxMigrationLength = 150;
    private const int TimePrefix = 16;
    private const int MaxNameLength = MaxMigrationLength - TimePrefix;
    private const int MultipleUnderscoresTimeoutMilliseconds = 1000;

    internal static string BuildMigrationName(IEnumerable<MigrationOperation> operations)
    {
        var parts = new List<string>();
        var seen = new HashSet<string>();

        foreach (var operation in operations)
        {
            var raw = operation switch
            {
                AddColumnOperation o => $"Add_Column_{o.Name}_To_{o.Table}_Table",
                DropColumnOperation o => $"Delete_Column_{o.Name}_From_{o.Table}_Table",
                AlterColumnOperation o => $"Alter_Column_{o.Name}_In_{o.Table}_Table",
                RenameColumnOperation o => $"Rename_Column_{o.Name}_In_{o.Table}_Table_To_{o.NewName}",
                CreateTableOperation o => $"Add_{o.Name}_Table",
                DropTableOperation o => $"Delete_{o.Name}_Table",
                RenameTableOperation o => $"Rename_Table_{o.Name}_To_{o.NewName}",
                CreateIndexOperation o => $"Add_{o.Name}_Index_To_{o.Table}_Table",
                DropIndexOperation o => $"Delete_{o.Name}_Index_From_{o.Table}_Table",
                RenameIndexOperation o => $"Rename_Index_{o.Name}_On_{o.Table}_Table_To_{o.NewName}",
                AddForeignKeyOperation o => $"Add_FK_{o.Name}_To_{o.Table}_Table",
                DropForeignKeyOperation o => $"Drop_FK_{o.Name}_From_{o.Table}_Table",
                AddPrimaryKeyOperation o => $"Add_PK_{o.Name}_To_{o.Table}_Table",
                DropPrimaryKeyOperation o => $"Drop_PK_{o.Name}_From_{o.Table}_Table",
                AddUniqueConstraintOperation o => $"Add_UC_{o.Name}_To_{o.Table}_Table",
                DropUniqueConstraintOperation o => $"Drop_UC_{o.Name}_From_{o.Table}_Table",
                _ => operation.GetType().Name.Replace("Operation", "")
            };

            var cleaned = Clean(raw);
            if (seen.Add(cleaned))
            {
                parts.Add(cleaned);
            }
        }

        var baseName = string.Join('_', parts);
        var time = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        var uniqueName = $"{time}_{baseName}";

        return Truncate(uniqueName);
    }

    private static string Clean(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(input.Length);

        foreach (var ch in input)
        {
            if (invalid.Contains(ch))
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(ch);
            }
        }

        var cleaned = MultipleUnderscoresRegex().Replace(sb.ToString(), "_").TrimEnd('.', ' ');

        return cleaned;
    }

    private static string Truncate(string input, int maxLength = MaxNameLength)
    {
        if (input.Length <= maxLength)
        {
            return input;
        }

        const int HashLen = 8;
        var bytes = Encoding.UTF8.GetBytes(input);
        var hex = Convert.ToHexString(SHA256.HashData(bytes));
        var hash = hex[..HashLen];
        var stubLen = maxLength - (1 + HashLen);

        return input[..stubLen] + "_" + hash;
    }

    [GeneratedRegex("_{2,}", RegexOptions.None, MultipleUnderscoresTimeoutMilliseconds)]
    private static partial Regex MultipleUnderscoresRegex();
}
