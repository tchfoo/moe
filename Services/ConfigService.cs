namespace TNTBot.Services;

public static class ConfigService
{
  public static Config Config { get; private set; } = default!;
  public static string Environment { get; set; } = default!;

  public static async Task Init()
  {
    Config = await Config.Load();
  }

  public static bool IsDev()
  {
    return Environment == "dev";
  }

  public static bool IsProd()
  {
    return Environment == "prod";
  }
}
