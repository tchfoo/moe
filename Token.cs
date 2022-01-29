using Newtonsoft.Json;

namespace TNTBot
{
  public class Token
  {
    public string Value { get; set; } = default!;

    public static Token Load()
    {
      var json = File.ReadAllText("token.json");
      return JsonConvert.DeserializeObject<Token>(json)!;
    }
  }
}
