namespace TNTBot.Services
{
  public static class ConfigService
  {
    public static Config Config { get; }

    static ConfigService()
    {
      Config = Config.Load();
    }
  }
}
