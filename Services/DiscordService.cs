using Discord;
using Discord.WebSocket;
using TNTBot.Models;

namespace TNTBot.Services
{
  public static class DiscordService
  {
    public static DiscordSocketClient Discord { get; private set; } = default!;

    public static void Init()
    {
      var config = new DiscordSocketConfig()
      {
        GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.AllUnprivileged,
        AlwaysDownloadUsers = true,
        MessageCacheSize = 10_000,
      };
      Discord = new DiscordSocketClient(config);

      Discord.Log += async (msg) =>
        await LogService.LogToFileAndConsole(
          $"[{msg.Severity}] {msg.Message} {msg.Exception}", level: LogLevel.Discord);
    }

    public static async Task Start()
    {
      var token = ConfigService.Token.Value;
      await Discord.LoginAsync(TokenType.Bot, token);
      await Discord.StartAsync();

      // Don't close the app
      await Task.Delay(-1);
    }
  }
}
