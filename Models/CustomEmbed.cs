using Discord.WebSocket;

namespace Moe.Models;

public class CustomEmbed
{
  public SocketTextChannel Channel { get; set; } = default!;
  public string Title { get; set; } = default!;
  public string Description { get; set; } = default!;
  public string? Footer { get; set; }
  public string? ThumbnailImageUrl { get; set; }
  public string? LargeImageUrl { get; set; } = default;
}
