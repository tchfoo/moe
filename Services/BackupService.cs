using System.Diagnostics;
namespace MoeBot.Services;

public class BackupService
{
  private const string BackupsDir = "backups";

  public void Init()
  {
    Task.Run(async () =>
    {
      var currentMs = (DateTime.Now - DateTime.MinValue).TotalMilliseconds;
      var intervalMs = ConfigService.Environment.BackupInterval.TotalMilliseconds;
      var lastBackupSince = TimeSpan.FromMilliseconds(currentMs % intervalMs);
      var nextBackup = ConfigService.Environment.BackupInterval - lastBackupSince;
      await Task.Delay(nextBackup);

      while (true)
      {
        await StartMakingDatabaseBackup();
        await DeleteOldBackups();
        await Task.Delay(ConfigService.Environment.BackupInterval);
      }
    });
  }

  private async Task StartMakingDatabaseBackup()
  {
    Directory.CreateDirectory(BackupsDir);

    var todayDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
    var backupPath = Path.Combine(BackupsDir, $"{todayDate}.db");
    var backupCommand = $"storage.db \".timeout 10000\" \".backup {backupPath}\"";

    await LogService.LogToFileAndConsole($"Backup started at {backupPath}");
    var currentDirectory = Directory.GetCurrentDirectory();
    var process = new Process()
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = "sqlite3",
        Arguments = backupCommand,
        WorkingDirectory = currentDirectory,
      }
    };
    process.Start();
    await process.WaitForExitAsync();
    await LogService.LogToFileAndConsole($"Backup done at {backupPath}");
  }

  private async Task DeleteOldBackups()
  {
    var filesToDelete = new DirectoryInfo(BackupsDir).GetFiles()
      .OrderBy(x => x.CreationTime)
      .Skip(ConfigService.Environment.BackupsToKeep);

    foreach (var file in filesToDelete)
    {
      await LogService.LogToFileAndConsole($"Deleting old backup: {file.FullName}");
      file.Delete();
    }
  }
}
