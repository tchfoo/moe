using System.Diagnostics;
namespace TNTBot.Services
{
  public class BackupService
  {
    private const string BackupsDir = "backups";

    public void Init()
    {
      Task.Run(async () =>
      {
        while (true)
        {
          await Task.Delay(ConfigService.Config.BackupInterval);
          await StartMakingDatabaseBackup();
          await DeleteOldBackups();
        }
      });
    }

    private async Task StartMakingDatabaseBackup()
    {
      Directory.CreateDirectory(BackupsDir);

      var todayDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
      var backupPath = Path.Combine(BackupsDir, $"{todayDate}.db");
      var backupCommand = $"storage.db \".timeout 10000\" \".backup {backupPath}\"";

      await LogService.LogToFileAndConsole($"Making backup with sqlite3: {backupCommand}");
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
    }

    private async Task DeleteOldBackups()
    {
      var filesToDelete = new DirectoryInfo(BackupsDir).GetFiles()
        .OrderBy(x => x.CreationTime)
        .Skip(ConfigService.Config.BackupsToKeep);

      foreach (var file in filesToDelete)
      {
        await LogService.LogToFileAndConsole($"Deleting old backup: {file.FullName}");
        file.Delete();
      }
    }
  }
}
