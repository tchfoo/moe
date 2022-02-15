using Discord;
using Discord.WebSocket;

namespace TNTBot.Services
{
  public class PinService
  {
    private const long pinChannelId = 938860776591089674;

    public SocketTextChannel GetPinChannel()
    {
      //TODO: Change this after settings
      DiscordSocketClient discord = DiscordService.Discord;
      return (SocketTextChannel)discord.GetChannel(pinChannelId);
    }

    public Embed PinMessageEmbed(IMessage message)
    {
      var roles = ((SocketGuildUser)message.Author).Roles
        .Where(x => x.Color != Color.Default)
        .OrderBy(x => x.Position);

      var embed = new EmbedBuilder()
        .WithAuthor(message.Author)
        .WithDescription($"{message.Content}\n\n[Jump to message]({message.GetJumpUrl()})")
        .WithFooter($"{message.Timestamp.DateTime: yyyy-MM-dd • H:m}")
        .WithColor(roles.Last().Color);

      if (message.Attachments.Count > 0)
      {
        embed.WithImageUrl(message.Attachments.First().Url);
      }

      return embed.Build();
    }
  }
}
