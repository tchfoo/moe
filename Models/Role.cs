using Discord.WebSocket;

namespace TNTBot.Models
{
  public class Role
  {
    public string Name { get; set; }
    public SocketRole DiscordRole { get; set; }

    public Role(string name, SocketRole discordRole)
    {
      Name = name;
      DiscordRole = discordRole;
    }
  }
}
