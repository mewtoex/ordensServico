namespace Os.DatabaseTools;

public interface IBackupService
{
    Task<string> CreateAsync();
    Task<RestoreReport> VerifyRestoreAsync(string serverBackupPath);
}

public record RestoreReport(string BackupPath, string TestDatabase, IReadOnlyDictionary<string, long> Rows);
