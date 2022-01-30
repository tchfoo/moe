using Newtonsoft.Json;

namespace TNTBot
{
  public class Config
  {
    public ulong ServerID { get; set; } = default!;
    public List<ulong> Owners { get; set; } = default!;
    public ulong Yaha { get; set; } = default!;
    public string CommandPrefix { get; set; } = default!;

    public static Config Load()
    {
      var json = File.ReadAllText("config.json");
      return JsonConvert.DeserializeObject<Config>(json)!;
    }
  }
}
