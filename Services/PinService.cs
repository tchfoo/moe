using Discord;
using Discord.WebSocket;

namespace TNTBot.Services
{
  public class PinService
  {
    private readonly SettingsService settingsService;

    public PinService(SettingsService settingsService)
    {
      this.settingsService = settingsService;
    }

    private async Task<SocketTextChannel?> GetPinChannel(SocketGuild guild)
    {
      return await settingsService.GetPinChannel(guild);
    }

    private Embed PinMessageEmbed(IMessage message)
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

    private async Task<bool> EnsurePinnable(SocketCommandBase cmd, IMessage msg)
    {
      if (msg.Author.IsBot)
      {
        await cmd.RespondAsync("I'm not allowed to respond to bots");
        return false;
      }

      return true;
    }

    private async Task<bool> EnsurePinChannelSet(SocketCommandBase cmd, SocketTextChannel? pinChannel)
    {
      if (pinChannel is null)
      {
        await cmd.RespondAsync("No pin channel was set. Set it using /settings pinchannel");
        return false;
      }

      return true;
    }

    public async Task<bool> TryPinningMessage(SocketCommandBase cmd, IMessage msg)
    {
      var guild = (cmd.Channel as SocketGuildChannel)!.Guild;

      if (!await EnsurePinnable(cmd, msg))
      {
        return false;
      }

      var pinChannel = await GetPinChannel(guild);
      if (!await EnsurePinChannelSet(cmd, pinChannel))
      {
        return false;
      }

      var pinEmbed = PinMessageEmbed(msg);
      await pinChannel!.SendMessageAsync(embed: pinEmbed);
      await cmd.RespondAsync("Message successfully pinned.");
      return true;
    }
  }
}
