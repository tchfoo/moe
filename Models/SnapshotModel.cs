using Discord;
using Discord.WebSocket;

namespace MoeBot.Models;

public class SnapshotModel
{
  public int Id { get; set; }
  public SocketGuild Guild { get; set; } = default!;
  public string Name { get; set; } = default!;
  public string GuildName { get; set; } = default!;
  public Image? GuildIcon { get; set; }
  public Dictionary<ulong, string> Channels { get; set; } = default!;
  public Dictionary<ulong, string>? Roles { get; set; }
}
