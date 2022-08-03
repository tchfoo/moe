using System.Text.RegularExpressions;

namespace TNTBot;

public static class DurationParser
{
  public static TimeSpan Parse(string s)
  {
    var days = ParsePostfixedNumber(s, "d");
    var hours = ParsePostfixedNumber(s, "h");
    var minutes = ParsePostfixedNumber(s, "m");
    var seconds = ParsePostfixedNumber(s, "s");
    return new TimeSpan(days, hours, minutes, seconds);
  }

  private static int ParsePostfixedNumber(string text, string postfix)
  {
    var match = Regex.Match(text, $@"(\d+)\s*{postfix}");
    if (!match.Success)
    {
      return 0;
    }

    return int.Parse(match.Groups[1].Value);
  }
}
