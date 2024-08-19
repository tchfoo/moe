using Discord.WebSocket;

namespace Moe.Models;

public class EmbedFixerModel
{
  public string Pattern { get; set; } = default!;
  public string Replacement { get; set; } = default!;
}
