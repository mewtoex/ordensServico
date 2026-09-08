using System.Text.Json;
using Os.DatabaseTools;

try
{
    var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
        ?? throw new InvalidOperationException("Configure ConnectionStrings__Database.");
    IBackupService service = new SqlBackupService(connection);
    if (args is ["backup"])
    {
        var path = await service.CreateAsync();
        Console.WriteLine(JsonSerializer.Serialize(new { BackupPath = path }));
    }
    else if (args is ["verify", var path])
        Console.WriteLine(JsonSerializer.Serialize(await service.VerifyRestoreAsync(path)));
    else
        throw new ArgumentException("Uso: backup | verify <caminho-no-servidor>");
    return 0;
}
catch (Exception exception)
{
    // SQL errors can contain data or credentials; keep diagnostics bounded.
    Console.Error.WriteLine($"Operação de backup falhou ({exception.GetType().Name}). Verifique conexão, permissões e arquivo.");
    return 1;
}
