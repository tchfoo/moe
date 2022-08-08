using Discord;
using Discord.WebSocket;

namespace MoeBot.Services;

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
    var user = (SocketGuildUser)message.Author;

    var embed = new EmbedBuilder()
      .WithAuthor(message.Author)
      .WithDescription($"{message.Content}\n\n[Jump to message]({message.GetJumpUrl()})")
      .WithFooter($"{message.Timestamp.DateTime.ToLocalTime(): yyyy-MM-dd • HH:mm}")
      .WithColor(Colors.GetMainRoleColor(user));

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
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Bot messages cannot be pinned");
      return false;
    }

    return true;
  }

  private async Task<bool> EnsurePinChannelSet(SocketCommandBase cmd, SocketTextChannel? pinChannel)
  {
    if (pinChannel is null)
    {
      var guild = ((SocketGuildChannel)cmd.Channel).Guild;
      await LogService.LogToFileAndConsole(
        "No pin channel was set", guild, LogSeverity.Warning);

      await cmd.RespondAsync($"{Emotes.ErrorEmote} No pin channel was set. Set it using `/settings pinchannel`");
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

    await LogService.LogToFileAndConsole(
      $"Pinning message {msg.Id} from {cmd.Channel}", guild);

    var pinEmbed = PinMessageEmbed(msg);
    await pinChannel!.SendMessageAsync(embed: pinEmbed);
    await cmd.RespondAsync($"{Emotes.SuccessEmote} Message successfully pinned");
    return true;
  }
}
