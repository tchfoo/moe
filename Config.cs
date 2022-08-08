using DotNetEnv;
using MoeBot.Services;

namespace MoeBot;

public class Config
{
  public string Token { get; set; } = default!;
  public ulong? ServerID { get; set; } = default!;
  public List<ulong> Owners { get; set; } = default!;
  public ulong Yaha { get; set; } = default!;
  public TimeSpan BackupInterval { get; set; } = default!;
  public int BackupsToKeep { get; set; } = default!;

  public static async Task<Config> Load()
  {
    string envFile = ".env";
    if (ConfigService.IsDev())
    {
      envFile = "dev.env";
    }
    else if (ConfigService.IsProd())
    {
      envFile = "prod.env";
    }
    await LogService.LogToFileAndConsole($"Using {envFile} for environment variables");
    Env.Load(envFile);

    return new Config()
    {
      Token = GetEnv("TOKEN"),
      ServerID = ConfigService.IsProd() ? null : ulong.Parse(GetEnv("SERVERID")),
      Owners = GetEnv("OWNERS")
        .Split(',')
        .Select(x => ulong.Parse(x))
        .ToList(),
      Yaha = ulong.Parse(GetEnv("YAHA")),
      BackupInterval = TimeSpan.FromMinutes(int.Parse(GetEnv("BACKUP_INTERVAL_MINUTES"))),
      BackupsToKeep = int.Parse(GetEnv("BACKUPS_TO_KEEP")),
    };
  }

  private static string GetEnv(string name)
  {
    return Environment.GetEnvironmentVariable(name) ??
      throw new InvalidOperationException($"Environment variable {name} is not set");
  }
}
