using System.Text.RegularExpressions;

namespace Moe.Services;

public class EmbedFixerService
{
  private readonly Dictionary<string, string> linkRegexes;

  public EmbedFixerService()
  {
    linkRegexes = new Dictionary<string, string>
    {
      { @"https?://(?:clips\.twitch\.tv|(?:www\.)?twitch\.tv/\w+\/clip)\/([A-Za-z0-9-_]+)", @"https://clips.fxtwitch.tv/$1"},
      { @"https?://(?:[\w-]+?\.)?reddit\.com", @"https://rxddit.com" },
      { @"https?://(?:www\.)?threads\.net", @"https://fixthreads.net" },
      { @"https?://(?:www\.)?(twitter|x)\.com", @"https://fxtwitter.com" },
      { @"https?://(?:www\.)?instagram.com", @"https://ddinstagram.com" },
      { @"https?://(?:to)?github\.com/([A-Za-z0-9-]+/[A-Za-z0-9._-]+)/(?:issues|pull)/([0-9]+)([^\s]*)?", @"[$1#$2$3]($&)" },
      { @"https?://(?:www\.)?tiktok.com", @"https://vxtiktok.com" },
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
