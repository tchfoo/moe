using Discord;
using Discord.WebSocket;

namespace MoeBot.Services;

public static class DiscordService
{
  public static DiscordSocketClient Discord { get; private set; } = default!;

  public static void Init()
  {
    var config = new DiscordSocketConfig()
    {
      GatewayIntents =
        GatewayIntents.Guilds |
        GatewayIntents.GuildMembers |
        GatewayIntents.GuildBans |
        GatewayIntents.GuildVoiceStates |
        GatewayIntents.GuildMessages |
        GatewayIntents.MessageContent |
        GatewayIntents.DirectMessages,
      AlwaysDownloadUsers = true,
      MessageCacheSize = 10_000,
    };
    Discord = new DiscordSocketClient(config);

    Discord.Log += async (msg) =>
      await LogService.LogToFileAndConsole(
        $"[DNet] {msg.Message} {msg.Exception}", severity: msg.Severity);
  }

  public static async Task Start()
  {
    var token = ConfigService.Environment.Token;
    await Discord.LoginAsync(TokenType.Bot, token);
    await Discord.StartAsync();

    // Don't close the app
    await Task.Delay(-1);
  }
}
