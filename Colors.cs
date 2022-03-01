using Discord;
using Discord.WebSocket;

namespace TNTBot
{
  public static class Colors
  {
    public static readonly Color Red = new(0xCC6D64);
    public static readonly Color Blurple = new(0x7289DA);
    public static readonly Color Green = new(0x64CCA8);
    public static readonly Color Yellow = new(0xF5EEB9);
    public static readonly Color LightBlue = new(0x6BD1EA);

    public static Color GetMainRoleColor(SocketGuildUser user)
    {
      return user.Roles
        .Where(x => x.Color != default)
        .OrderByDescending(x => x.Position)
        .Select(x => x.Color)
        .FirstOrDefault(Blurple);
    }
  }
}
