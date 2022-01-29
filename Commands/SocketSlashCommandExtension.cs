using Discord.WebSocket;

namespace TNTBot
{
  public static class SocketSlashCommandExtension
  {
    public static T? GetOption<T>(this SocketSlashCommand cmd, string name, T? @default = default)
    {
      if (cmd.Data.Options.Any(x => x.Name == name))
      {
        return (T)cmd.Data.Options.First(x => x.Name == name).Value;
      }
      return @default;
    }

    public static bool HasOption(this SocketSlashCommand cmd, string name)
    {
      return cmd.Data.Options.Any(x => x.Name == name);
    }
  }
}
