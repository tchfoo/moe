using Discord.WebSocket;

namespace TNTBot.Models;

public class CustomRole
{
  public string Name { get; set; }
  public SocketRole DiscordRole { get; set; }

  public CustomRole(string name, SocketRole discordRole)
  {
    Name = name;
    DiscordRole = discordRole;
  }
}
