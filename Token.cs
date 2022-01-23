using Newtonsoft.Json;

class Token
{
  public string Value { get; set; }

  public static Token Load()
  {
    var json = File.ReadAllText("token.json");
    return JsonConvert.DeserializeObject<Token>(json);
  }
}
