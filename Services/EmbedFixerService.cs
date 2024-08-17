using System.Text.RegularExpressions;

namespace Moe.Services;

public class EmbedFixerService
{
  private readonly Dictionary<string, string> linkRegexes;

  public EmbedFixerService()
  {
    linkRegexes = new Dictionary<string, string>
    {
      { "https://reddit.com", "https://rxddit.com" }
    };
  }

  public string ReplaceLinks(string input)
  {
    foreach (var (key, value) in linkRegexes)
    {
      Regex regex = new Regex(key);
      input = regex.Replace(input, value);
    }

    return input;
  }
}
