using Discord;
using Discord.WebSocket;

namespace Moe.Services;

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

  private Embed PinMessageEmbed(IMessage msg)
  {
    var color = Colors.Grey;
    if (msg.Author is SocketGuildUser)
    {
      var user = (SocketGuildUser)msg.Author;
      color = Colors.GetMainRoleColor(user);
    }

    var attachmentInfo = msg.Attachments.Count >= 2 ?
      $"*{msg.Attachments.Count - 1} more image(s)*\n" :
      string.Empty;
    var jumpToMessage = $"[Jump to message]({msg.GetJumpUrl()})";

    var embed = new EmbedBuilder()
      .WithAuthor(msg.Author)
      .WithDescription($"{msg.Content}\n\n{attachmentInfo}{jumpToMessage}")
      .WithFooter($"{msg.Timestamp.DateTime.ToLocalTime(): yyyy-MM-dd • HH:mm}")
      .WithColor(color);

    if (msg.Attachments.Count > 0)
    {
      embed.WithImageUrl(msg.Attachments.First().Url);
    }

    return embed.Build();
  }

  private async Task<bool> EnsurePinnable(SocketCommandBase cmd, IMessage msg)
  {
    if (msg.Author.IsBot)
    {
      await cmd.FollowupAsync($"{Emotes.ErrorEmote} Bot messages cannot be pinned");
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

      await cmd.FollowupAsync($"{Emotes.ErrorEmote} No pin channel was set. Set it using `/settings pinchannel`");
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

    var onlyImageAttachments = msg.Attachments.All(x => x.ContentType.StartsWith("image"));
    if (onlyImageAttachments)
    {
      var pinEmbed = PinMessageEmbed(msg);
      await pinChannel!.SendMessageAsync(embed: pinEmbed);
    }
    else
    {
      var attachments = string.Join("\n", msg.Attachments.Select(x => x.Url));
      await pinChannel!.SendMessageAsync($"**{msg.Author.Username}**: {msg.Content}\n\n{attachments}");
    }

    await cmd.FollowupAsync($"{Emotes.SuccessEmote} Message successfully pinned");
    return true;
  }
}
