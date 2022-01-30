using Discord;
using Discord.WebSocket;

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
        AlwaysDownloadUsers = true
      };
      Discord = new DiscordSocketClient(config);

      Discord.Log += (msg) =>
      {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
      };
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
