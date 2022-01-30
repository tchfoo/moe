namespace TNTBot.Services
{
  public static class ConfigService
  {
    public static Config Config { get; }
    public static Token Token { get; }

    static ConfigService()
    {
      Config = Config.Load();
      Token = Token.Load();
    }
  }
}
