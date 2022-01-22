using Newtonsoft.Json;

class Config
{
  public string Token { get; set; }
  public ulong ServerID { get; set; }

  public static Config Load()
  {
    var json = File.ReadAllText("config.json");
    return JsonConvert.DeserializeObject<Config>(json);
  }
}
