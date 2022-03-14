using DotNetEnv;

namespace TNTBot
{
  public class Config
  {
    public string Token { get; set; } = default!;
    public ulong ServerID { get; set; } = default!;
    public List<ulong> Owners { get; set; } = default!;
    public ulong Yaha { get; set; } = default!;

    public static Config Load()
    {
      Env.Load();

      return new Config()
      {
        Token = GetEnv("TOKEN"),
        ServerID = ulong.Parse(GetEnv("SERVERID")),
        Owners = GetEnv("OWNERS")
          .Split(',')
          .Select(x => ulong.Parse(x))
          .ToList(),
        Yaha = ulong.Parse(GetEnv("YAHA"))
      };
    }

    private static string GetEnv(string name)
    {
      return Environment.GetEnvironmentVariable(name) ??
        throw new InvalidOperationException($"Environment variable {name} is not set");
    }
  }
}
