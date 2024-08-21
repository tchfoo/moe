using Discord.WebSocket;

namespace Moe.Models;

public class EmbedFixerPattern
{
  public string Pattern { get; set; } = default!;
  public string Replacement { get; set; } = default!;
}
