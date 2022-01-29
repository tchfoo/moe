using Discord;
using Discord.WebSocket;

namespace TNTBot.Services
{
  public static class DiscordService
  {
    public static DiscordSocketClient Discord { get; }
    static DiscordService()
    {
      var config = new DiscordSocketConfig()
      {
        GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.AllUnprivileged,
        AlwaysDownloadUsers = true
      };
      Discord = new DiscordSocketClient(config);
    }
  }
}
