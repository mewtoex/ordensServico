using Microsoft.Data.SqlClient;

namespace Os.DatabaseTools;

public sealed class SqlBackupService(string connectionString) : IBackupService
{
    private static string Identifier(string value) => "[" + value.Replace("]", "]]") + "]";
    private static string Literal(string value) => "N'" + value.Replace("'", "''") + "'";

    private SqlConnection MasterConnection()
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master", ConnectTimeout = 10 };
        return new SqlConnection(builder.ConnectionString);
    }

    public async Task<string> CreateAsync()
    {
        var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (string.IsNullOrWhiteSpace(database) || new[] { "master", "model", "msdb", "tempdb" }.Contains(database, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Informe um banco de aplicação para backup.");
        var path = $"/var/opt/mssql/data/osbackup-{Guid.NewGuid():N}.bak";
        await using var connection = MasterConnection();
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"BACKUP DATABASE {Identifier(database)} TO DISK = {Literal(path)} WITH COPY_ONLY, CHECKSUM;");
        return path;
    }

    public async Task<RestoreReport> VerifyRestoreAsync(string serverBackupPath)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(serverBackupPath, @"\A/var/opt/mssql/data/osbackup-[0-9a-f]{32}\.bak\z"))
            throw new ArgumentException("Use o caminho de backup gerado por esta ferramenta.");
        var target = $"OsRestore_{Guid.NewGuid():N}";
        var source = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (target.Equals(source, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Banco de teste inválido.");
        await using var connection = MasterConnection();
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"RESTORE VERIFYONLY FROM DISK = {Literal(serverBackupPath)} WITH CHECKSUM;");
        var moves = new List<string>();
        await using (var command = new SqlCommand($"RESTORE FILELISTONLY FROM DISK = {Literal(serverBackupPath)};", connection) { CommandTimeout = 300 })
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var logical = reader.GetString(reader.GetOrdinal("LogicalName"));
                var type = reader.GetString(reader.GetOrdinal("Type"));
                if (type is not ("D" or "L"))
                    throw new InvalidOperationException("Somente arquivos de dados e log são suportados.");
                var destination = $"/var/opt/mssql/data/{target}_{moves.Count}.{(type == "L" ? "ldf" : "mdf")}";
                moves.Add($"MOVE {Literal(logical)} TO {Literal(destination)}");
            }
        }
        if (moves.Count == 0)
            throw new InvalidOperationException("Backup sem arquivos.");
        await ExecuteAsync(connection, $"IF DB_ID({Literal(target)}) IS NOT NULL THROW 51000, 'Destino já existe.', 1;");
        try
        {
            // Never use REPLACE: only a generated database and generated physical files are targets.
            await ExecuteAsync(connection, $"RESTORE DATABASE {Identifier(target)} FROM DISK = {Literal(serverBackupPath)} WITH CHECKSUM, RECOVERY, {string.Join(", ", moves)};");
            await ExecuteAsync(connection, $"DBCC CHECKDB ({Identifier(target)}) WITH NO_INFOMSGS, ALL_ERRORMSGS;");
            var rows = new Dictionary<string, long>();
            foreach (var table in new[] { "Users", "Customers", "Catalog", "Orders", "OrderItem", "AuditEntry", "RefreshSessions", "__EFMigrationsHistory" })
            {
                await using var count = new SqlCommand($"SELECT COUNT_BIG(*) FROM {Identifier(target)}.dbo.{Identifier(table)};", connection) { CommandTimeout = 300 };
                rows[table] = (long)(await count.ExecuteScalarAsync())!;
            }
            return new RestoreReport(serverBackupPath, target, rows);
        }
        finally
        {
            await ExecuteAsync(connection, $"IF DB_ID({Literal(target)}) IS NOT NULL BEGIN ALTER DATABASE {Identifier(target)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {Identifier(target)}; END;");
        }
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 300 };
        await command.ExecuteNonQueryAsync();
    }
}
