using Discord;
using Discord.WebSocket;

namespace Moe.Services;

public class DiscordLogService
{
  public void Register()
  {
    DiscordService.Discord.MessageDeleted += OnMessageDeleted;
    DiscordService.Discord.MessageUpdated += OnMessageUpdated;
    DiscordService.Discord.UserJoined += OnUserJoined;
    DiscordService.Discord.UserLeft += OnUserLeft;
    DiscordService.Discord.UserVoiceStateUpdated += async (genericUser, oldState, newState) =>
    {
      var user = (SocketGuildUser)genericUser;
      var oldChannel = oldState.VoiceChannel;
      var newChannel = newState.VoiceChannel;
      if (oldChannel == newChannel)
      {
        return;
      }

      if (oldChannel is not null && newChannel is not null)
      {
        await OnUserChangeVoice(user, oldState.VoiceChannel!, newState.VoiceChannel!);
      }
      else if (newState.VoiceChannel is not null)
      {
        await OnUserJoinedVoice(user, newState.VoiceChannel);
      }
      else if (oldState.VoiceChannel is not null)
      {
        await OnUserLeftVoice(user, oldState.VoiceChannel);
      }
    };
    DiscordService.Discord.UserBanned += OnUserBanned;
    DiscordService.Discord.UserUnbanned += OnUserUnbanned;
  }

  private async Task OnMessageDeleted(Cacheable<IMessage, ulong> cachedMessage,
    Cacheable<IMessageChannel, ulong> cachedChannel)
  {
    if (!cachedMessage.HasValue)
    {
      return;
    }

    var channel = (SocketTextChannel)cachedChannel.Value;
    var message = cachedMessage.Value;
    if (string.IsNullOrEmpty(message.Content))
    {
      return;
    }

    var embed = new EmbedBuilder()
      .WithAuthor(message.Author)
      .WithTitle($"Message deleted in #{channel.Name}")
      .WithDescription(message.Content)
      .WithColor(Colors.Red)
      .WithFooter(GenerateFooter(message.Author));

    if (message.Attachments.Count > 0)
    {
      embed.AddField("Attachments", AttachmentsToString(message.Attachments));
    }

    await LogService.Instance.LogToDiscord(channel.Guild, embed: embed.Build());
  }

  private async Task OnMessageUpdated(Cacheable<IMessage, ulong> oldCachedMessage,
    SocketMessage newMessage, ISocketMessageChannel genericChannel)
  {
    if (!oldCachedMessage.HasValue)
    {
      return;
    }

    var channel = (SocketTextChannel)genericChannel;
    var oldMessage = oldCachedMessage.Value;
    if (oldMessage.Content == newMessage.Content ||
      string.IsNullOrEmpty(oldMessage.Content) || string.IsNullOrEmpty(newMessage.Content))
    {
      return;
    }

    var embed = new EmbedBuilder()
        .WithAuthor(newMessage.Author)
        .WithTitle($"Message edited in #{channel.Name}")
        .WithDescription(
          $"**Before:** {oldMessage.Content}\n" +
          $"**After:** {newMessage.Content}\n\n" +
          $"[Jump to message]({newMessage.GetJumpUrl()})")
        .WithColor(Colors.Blurple)
        .WithFooter(GenerateFooter(newMessage.Author));

    await LogService.Instance.LogToDiscord(channel.Guild, embed: embed.Build());
  }

  private async Task OnUserJoined(SocketGuildUser user)
  {
    var embed = new EmbedBuilder()
        .WithAuthor(user)
        .WithTitle("Member joined")
        .WithDescription(
          $"{user.Mention}\n" +
          $"Created at {DateTimeToString(user.CreatedAt)}")
        .WithColor(Colors.Green)
        .WithFooter(GenerateFooter(user));

    await LogService.Instance.LogToDiscord(user.Guild, embed: embed.Build());
  }

  private async Task OnUserLeft(SocketGuild guild, SocketUser user)
  {
    var guildUser = (SocketGuildUser)user;

    var embedDescription = $"{user.Mention} joined {DateTimeToString(guildUser.JoinedAt!.Value)}";

    if (guildUser.Roles.Count > 0)
    {
      embedDescription += $"\n**Roles**: {RolesToString(guildUser.Roles)}";
    }

    var embed = new EmbedBuilder()
        .WithAuthor(user)
        .WithTitle("Member left")
        .WithDescription(embedDescription)
        .WithColor(Colors.Yellow)
        .WithFooter(GenerateFooter(user));

    await LogService.Instance.LogToDiscord(guild, embed: embed.Build());
  }

  private async Task OnUserJoinedVoice(SocketGuildUser user, SocketVoiceChannel channel)
  {
    var embed = new EmbedBuilder()
      .WithAuthor(user)
      .WithTitle("Member joined voice channel")
      .WithDescription($"{user.Mention} joined {channel.Mention}")
      .WithColor(Colors.Green)
      .WithFooter(GenerateFooter(user));

    await LogService.Instance.LogToDiscord(user.Guild, embed: embed.Build());
  }

  private async Task OnUserLeftVoice(SocketGuildUser user, SocketVoiceChannel channel)
  {
    var embed = new EmbedBuilder()
      .WithAuthor(user)
      .WithTitle("Member left voice channel")
      .WithDescription($"{user.Mention} left {channel.Mention}")
      .WithColor(Colors.Red)
      .WithFooter(GenerateFooter(user));

    await LogService.Instance.LogToDiscord(user.Guild, embed: embed.Build());
  }

  private async Task OnUserChangeVoice(SocketGuildUser user, SocketVoiceChannel oldChannel, SocketVoiceChannel newChannel)
  {
    var embed = new EmbedBuilder()
      .WithAuthor(user)
      .WithTitle("Member changed voice channel")
      .WithDescription(
        $"**Before:** {oldChannel.Mention}\n" +
        $"**After:** {newChannel.Mention}")
      .WithColor(Colors.Blurple)
      .WithFooter(GenerateFooter(user));

    await LogService.Instance.LogToDiscord(user.Guild, embed: embed.Build());
  }

  private async Task OnUserBanned(SocketUser user, SocketGuild guild)
  {
    var embed = new EmbedBuilder()
      .WithAuthor(user)
      .WithTitle("Member banned")
      .WithDescription($"{user.Mention}")
      .WithColor(Colors.Red)
      .WithFooter(GenerateFooter(user));

    await LogService.Instance.LogToDiscord(guild, embed: embed.Build());
  }

  private async Task OnUserUnbanned(SocketUser user, SocketGuild guild)
  {
    var embed = new EmbedBuilder()
      .WithAuthor(user)
      .WithTitle("Member unbanned")
      .WithDescription($"{user.Mention}")
      .WithColor(Colors.LightBlue)
      .WithFooter(GenerateFooter(user));

    await LogService.Instance.LogToDiscord(guild, embed: embed.Build());
  }

  private string AttachmentsToString(IReadOnlyCollection<IAttachment> attachments)
  {
    string attachmentsOut = "";
    if (attachments.Count > 0)
    {
      foreach (var attachment in attachments)
      {
        attachmentsOut += $"{attachment.Filename}\n";
      }
    }
    return attachmentsOut;
  }

  private string RolesToString(IReadOnlyCollection<SocketRole> allRoles)
  {
    var roles = allRoles.Where(x => !x.IsEveryone).ToList();
    string rolesOut = "";
    if (roles.Count > 0)
    {
      foreach (var role in roles)
      {
        rolesOut += $" {role.Mention}";
      }
    }
    return rolesOut;
  }

  private readonly string DateTimeFormatString = "yyyy-MM-dd HH:mm";

  private string DateTimeToString(DateTimeOffset dateTimeOffset)
  {
    return dateTimeOffset.ToLocalTime().ToString(DateTimeFormatString);
  }

  private string DateTimeToString(DateTime dateTime)
  {
    return dateTime.ToLocalTime().ToString(DateTimeFormatString);
  }

  private string NowToString()
  {
    return DateTimeToString(DateTime.Now);
  }

  private string GenerateFooter(IUser user)
  {
    return $"ID: {user.Id} • {NowToString()}";
  }
}
