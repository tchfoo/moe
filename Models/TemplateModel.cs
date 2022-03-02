using Discord.WebSocket;

namespace TNTBot.Models
{
  public class TemplateModel
  {
    public int Id { get; set; }
    public SocketGuild Guild { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Hidden { get; set; }
    public SocketGuildUser Creator { get; set; } = default!;
    public SocketTextChannel Channel { get; set; } = default!;
    public SocketRole? MentionedRole { get; set; }
    public string? ThumbnailImageUrl { get; set; }
    public string? ImageUrl { get; set; } = default;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? Footer { get; set; }
  }
}
