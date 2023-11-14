using Discord.WebSocket;

namespace Moe.Models;

public class TemplateModel
{
  public int Id { get; set; }
  public SocketGuild Guild { get; set; } = default!;
  public SocketGuildUser Creator { get; set; } = default!;
  public string Name { get; set; } = default!;
  public SocketTextChannel Channel { get; set; } = default!;
  public SocketRole? Mention { get; set; }
  public bool Hidden { get; set; }
  public string Title { get; set; } = default!;
  public string Description { get; set; } = default!;
  public string? Footer { get; set; }
  public string? ThumbnailImageUrl { get; set; }
  public string? LargeImageUrl { get; set; } = default;
}
