using Discord.WebSocket;

namespace Moe.Models;

public class CustomRole
{
  public string Name { get; set; }
  public SocketRole DiscordRole { get; set; }
  public string? Description { get; set; }

  public CustomRole(string name, string? description, SocketRole discordRole)
  {
    Name = name;
    Description = description;
    DiscordRole = discordRole;
  }

  public override int GetHashCode()
  {
    return Name.GetHashCode();
  }

  public override bool Equals(object? obj)
  {
    return obj is CustomRole role && Name == role.Name;
  }
}
