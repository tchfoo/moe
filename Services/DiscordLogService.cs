using Discord;
using Discord.WebSocket;

namespace TNTBot.Services
{
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
      var channel = (SocketTextChannel)cachedChannel.Value;
      var message = cachedMessage.Value;
      string messageOut;
      if (cachedMessage.HasValue)
      {
        messageOut = "The message:\n" +
          $"{message.Author.Mention} {message.CreatedAt.ToLocalTime()}\n" +
          $"{message.Content}\n" +
          $"{AttachmentsToString(message.Attachments)}";
      }
      else
      {
        messageOut = "The message is unkown";
      }

      await LogService.Instance.LogToDiscord(channel.Guild, $"A message was deleted in {channel.Mention}. {messageOut}");
    }

    private async Task OnMessageUpdated(Cacheable<IMessage, ulong> oldCachedMessage,
      SocketMessage newMessage, ISocketMessageChannel genericChannel)
    {
      var channel = (SocketTextChannel)genericChannel;
      var oldMessage = oldCachedMessage.Value;
      var oldMessageOut = oldMessage?.Content ?? "The message is unkown";
      var newMessageOut = newMessage.Content;

      await LogService.Instance.LogToDiscord(channel.Guild, $"A message was edited in {channel.Mention}.\n" +
        $"{newMessage.Author.Mention} {newMessage.CreatedAt.ToLocalTime()}\n" +
        $"Jump to message: {newMessage.GetJumpUrl()}\n" +
        $"Before: {oldMessageOut}\n" +
        $"After: {newMessageOut}");
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
      await LogService.Instance.LogToDiscord(user.Guild, "Member joined\n" +
         $"{user.Mention}\n" +
         $"Created at {user.CreatedAt.ToLocalTime()}");
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
      var guildUser = (SocketGuildUser)user;
      await LogService.Instance.LogToDiscord(guild, "Member left\n" +
         $"{user.Mention} joined {guildUser.JoinedAt!.Value.ToLocalTime()}\n" +
         $"{RolesToString(guildUser.Roles)}");
    }

    private async Task OnUserJoinedVoice(SocketGuildUser user, SocketVoiceChannel channel)
    {
      await LogService.Instance.LogToDiscord(user.Guild, "Voice channel join\n" +
        $"{user.Mention} joined {channel.Mention}");
    }

    private async Task OnUserLeftVoice(SocketGuildUser user, SocketVoiceChannel channel)
    {
      await LogService.Instance.LogToDiscord(user.Guild, "Voice channel leave\n" +
        $"{user.Mention} left {channel.Mention}");
    }

    private async Task OnUserChangeVoice(SocketGuildUser user, SocketVoiceChannel oldChannel, SocketVoiceChannel newChannel)
    {
      await LogService.Instance.LogToDiscord(user.Guild, "Voice channel change\n" +
        $"{user.Mention} changed voice channel from {oldChannel.Mention} to {newChannel.Mention}");
    }

    private async Task OnUserBanned(SocketUser user, SocketGuild guild)
    {
      await LogService.Instance.LogToDiscord(guild, "Member banned\n" +
        $"{user.Mention}");
    }

    private async Task OnUserUnbanned(SocketUser user, SocketGuild guild)
    {
      await LogService.Instance.LogToDiscord(guild, "Member unbanned\n" +
        $"{user.Mention}");
    }

    private string AttachmentsToString(IReadOnlyCollection<IAttachment> attachments)
    {
      string attachmentsOut = "";
      if (attachments.Count > 0)
      {
        attachmentsOut += $"{attachments.Count} attachments:\n";
        foreach (var attachment in attachments)
        {
          attachmentsOut += $" - {attachment.Url}\n";
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
        rolesOut += $"{roles.Count} roles:";
        foreach (var role in roles)
        {
          rolesOut += $" {role.Mention}";
        }
      }
      return rolesOut;
    }
  }
}
