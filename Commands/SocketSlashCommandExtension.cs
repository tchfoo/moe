using Discord.WebSocket;

namespace MoeBot;

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

  public static T? GetOption<T>(this SocketSlashCommandDataOption data, string name, T? @default = default)
  {
    if (data.Options.Any(x => x.Name == name))
    {
      return (T)data.Options.First(x => x.Name == name).Value;
    }
    return @default;
  }

  public static bool HasOption(this SocketSlashCommand cmd, string name)
  {
    return cmd.Data.Options.Any(x => x.Name == name);
  }

  public static bool HasOption(this SocketSlashCommandDataOption data, string name)
  {
    return data.Options.Any(x => x.Name == name);
  }

  public static SocketSlashCommandDataOption GetSubcommand(this SocketSlashCommand cmd)
  {
    return cmd.Data.Options.First();
  }

  public static SocketSlashCommandDataOption GetSubcommand(this SocketSlashCommandDataOption data)
  {
    return data.Options.First();
  }
}
