using System.Data;
using Dapper;
using HrmSystem.Application.Common.Interfaces.Data;
using Microsoft.Extensions.Options;

namespace HrmSystem.Infrastructure.Services.Backup;

/*
    //?     Executes the native SQL Server full backup and reads the produced size from msdb.
    //!     BACKUP DATABASE runs SERVER-SIDE — the .bak lands on the SQL container's
    //!     /var/backups volume, not on the API host. COMPRESSION shrinks the file;
    //!     CHECKSUM makes silent page corruption fail the backup instead of the restore.
*/
internal sealed class SqlServerBackupService(
    ISqlConnectionFactory connectionFactory,
    IOptions<BackupOptions> options
)
{
    private readonly BackupOptions _options = options.Value;

    public string BuildBackupFileName(DateTime utcNow) =>
        $"{_options.DatabaseName}_{utcNow:yyyyMMdd_HHmmss}.bak";

    public async Task<long> ExecuteBackupAsync(string fileName, CancellationToken ct)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        //! Path.Combine would produce a Windows separator on a Windows API host — the path
        //! belongs to the LINUX SQL container, so it is joined with '/' explicitly.
        string fullPath = $"{_options.Directory.TrimEnd('/')}/{fileName}";

        /*
            //!     The database name comes from trusted configuration, but it is still quoted
            //!     with QUOTENAME semantics ([]) — BACKUP DATABASE cannot be parameterized.
        */
        string backupSql =
            $"BACKUP DATABASE [{_options.DatabaseName}] TO DISK = @Path WITH COMPRESSION, CHECKSUM, INIT";

        var backupCommand = new CommandDefinition(
            commandText: backupSql,
            parameters: new { Path = fullPath },
            commandTimeout: 1800, //! Full backups can take a while — 30-minute ceiling.
            cancellationToken: ct
        );

        await connection.ExecuteAsync(backupCommand);

        //? msdb.dbo.backupset records every backup — the authoritative size source.
        const string sizeSql = """
            SELECT TOP 1 CAST(compressed_backup_size AS bigint)
            FROM msdb.dbo.backupset
            WHERE database_name = @DatabaseName
            ORDER BY backup_finish_date DESC
            """;

        var sizeCommand = new CommandDefinition(
            commandText: sizeSql,
            parameters: new { _options.DatabaseName },
            cancellationToken: ct
        );

        return await connection.ExecuteScalarAsync<long>(sizeCommand);
    }
}
