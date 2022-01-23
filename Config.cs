using Newtonsoft.Json;

class Config
{
  public ulong ServerID { get; set; }
  public List<ulong> Owners { get; set; }
  public ulong Yaha { get; set; }

  public static Config Load()
  {
    var json = File.ReadAllText("config.json");
    return JsonConvert.DeserializeObject<Config>(json)!;
  }
}
