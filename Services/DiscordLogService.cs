using Discord;
using Discord.WebSocket;
using System.Drawing;

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

      System.Drawing.Color red = ColorTranslator.FromHtml("#cc6d64");
      var embed = new EmbedBuilder();

      if (cachedMessage.HasValue)
      {
        embed
          .WithAuthor(message.Author)
          .WithTitle($"Message deleted in #{channel.Name}")
          .WithDescription(message.Content)
          .WithColor((Discord.Color)red)
          .WithFooter($"ID: {message.Author.Id} • {message.CreatedAt.ToLocalTime():yyyy-MM-dd H:m}");

        if (message.Attachments.Count > 0)
        {
          embed
            .AddField("Attachments", AttachmentsToString(message.Attachments));
        }
      }
      else
      {
        embed
          .WithTitle($"Unknown message deleted in #{channel.Name}")
          .WithDescription("Unknown message.")
          .WithColor((Discord.Color)red)
          .WithFooter($"{message.CreatedAt.ToLocalTime():yyyy-MM-dd H:m}");
      }

      await LogService.Instance.LogToDiscord(channel.Guild, embed: embed.Build());
    }

    private async Task OnMessageUpdated(Cacheable<IMessage, ulong> oldCachedMessage,
      SocketMessage newMessage, ISocketMessageChannel genericChannel)
    {
      var channel = (SocketTextChannel)genericChannel;
      var oldMessage = oldCachedMessage.Value;
      var oldMessageOut = oldMessage?.Content ?? "The message is unkown";
      var newMessageOut = newMessage.Content;

      System.Drawing.Color blurple = ColorTranslator.FromHtml("#7289DA");

      var embed = new EmbedBuilder()
          .WithAuthor(newMessage.Author)
          .WithTitle($"Message edited in #{channel.Name}")
          .WithDescription($"**Before:** {oldMessageOut}\n" +
          $"**After:** {newMessageOut}\n\n" +
          $"[Jump to message]({newMessage.GetJumpUrl()})")
          .WithColor((Discord.Color)blurple)
          .WithFooter($"ID: {newMessage.Author.Id} • {newMessage.CreatedAt.ToLocalTime():yyyy-MM-dd H:m}");

      await LogService.Instance.LogToDiscord(channel.Guild, embed: embed.Build());
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
      System.Drawing.Color green = ColorTranslator.FromHtml("#64cca8");

      var embed = new EmbedBuilder()
          .WithAuthor(user)
          .WithTitle("Member joined")
          .WithDescription($"{user.Mention}\n" +
          $"Created at {user.CreatedAt.ToLocalTime():yyyy-MM-dd H:m}")
          .WithColor((Discord.Color)green)
          .WithFooter($"ID: {user.Id} • {DateTime.Now.ToLocalTime():yyyy-MM-dd H:m}");

      await LogService.Instance.LogToDiscord(user.Guild, embed: embed.Build());
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
      var guildUser = (SocketGuildUser)user;

      System.Drawing.Color yellow = ColorTranslator.FromHtml("#f5eeb9");

      var embedDescription = $"{user.Mention} joined {guildUser.JoinedAt!.Value.ToLocalTime():yyyy-MM-dd H:m}";

      if (guildUser.Roles.Count > 0)
      {
        embedDescription += $"\n**Roles**: {RolesToString(guildUser.Roles)}";
      }

      var embed = new EmbedBuilder()
          .WithAuthor(user)
          .WithTitle("Member left")
          .WithDescription(embedDescription)
          .WithColor((Discord.Color)yellow)
          .WithFooter($"ID: {user.Id} • {DateTime.Now.ToLocalTime():yyyy-MM-dd H:m}");

      await LogService.Instance.LogToDiscord(guild, embed: embed.Build());
    }

    private async Task OnUserJoinedVoice(SocketGuildUser user, SocketVoiceChannel channel)
    {
      System.Drawing.Color green = ColorTranslator.FromHtml("#64cca8");

      var embed = new EmbedBuilder()
        .WithAuthor(user)
        .WithTitle("Member joined voice channel")
        .WithDescription($"{user.Mention} joined {channel.Mention}")
        .WithColor((Discord.Color)green)
        .WithFooter($"ID: {user.Id} • {DateTime.Now.ToLocalTime():yyyy-MM-dd H:m}");

      await LogService.Instance.LogToDiscord(user.Guild, embed: embed.Build());
    }

    private async Task OnUserLeftVoice(SocketGuildUser user, SocketVoiceChannel channel)
    {
      System.Drawing.Color red = ColorTranslator.FromHtml("#cc6d64");

      var embed = new EmbedBuilder()
        .WithAuthor(user)
        .WithTitle("Member left voice channel")
        .WithDescription($"{user.Mention} left {channel.Mention}")
        .WithColor((Discord.Color)red)
        .WithFooter($"ID: {user.Id} • {DateTime.Now.ToLocalTime():yyyy-MM-dd H:m}");

      await LogService.Instance.LogToDiscord(user.Guild, embed: embed.Build());
    }

    private async Task OnUserChangeVoice(SocketGuildUser user, SocketVoiceChannel oldChannel, SocketVoiceChannel newChannel)
    {
      System.Drawing.Color blurple = ColorTranslator.FromHtml("#7289DA");

      var embed = new EmbedBuilder()
        .WithAuthor(user)
        .WithTitle("Member changed voice channel")
        .WithDescription($"**Before:** {oldChannel.Mention}\n" +
        $"**After:** {newChannel.Mention}")
        .WithColor((Discord.Color)blurple)
        .WithFooter($"ID: {user.Id} • {DateTime.Now.ToLocalTime():yyyy-MM-dd H:m}");

      await LogService.Instance.LogToDiscord(user.Guild, embed: embed.Build());
    }

    private async Task OnUserBanned(SocketUser user, SocketGuild guild)
    {
      System.Drawing.Color red = ColorTranslator.FromHtml("#cc6d64");

      var embed = new EmbedBuilder()
        .WithAuthor(user)
        .WithTitle("Member banned")
        .WithDescription($"{user.Mention}")
        .WithColor((Discord.Color)red)
        .WithFooter($"ID: {user.Id} • {DateTime.Now.ToLocalTime():yyyy-MM-dd H:m}");

      await LogService.Instance.LogToDiscord(guild, embed: embed.Build());
    }

    private async Task OnUserUnbanned(SocketUser user, SocketGuild guild)
    {
      System.Drawing.Color lightBlue = ColorTranslator.FromHtml("#6bd1ea");

      var embed = new EmbedBuilder()
        .WithAuthor(user)
        .WithTitle("Member unbanned")
        .WithDescription($"{user.Mention}")
        .WithColor((Discord.Color)lightBlue)
        .WithFooter($"ID: {user.Id} • {DateTime.Now.ToLocalTime():yyyy-MM-dd H:m}");

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
  }
}
