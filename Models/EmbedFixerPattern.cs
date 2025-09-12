namespace Moe.Models;

public class EmbedFixerPattern
{
  public string Pattern { get; set; } = default!;
  public string Replacement { get; set; } = default!;

  public EmbedFixerPattern(string pattern, string replacement)
  {
    Pattern = pattern;
    Replacement = replacement;
  }
}
