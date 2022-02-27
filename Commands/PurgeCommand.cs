using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class PurgeCommand : SlashCommandBase
  {
    private readonly PurgeService service;
    public PurgeCommand(PurgeService service) : base("purge")
    {
      Description = "Purge messages from a channel";
      Options = new SlashCommandOptionBuilder()
        .AddOption("count", ApplicationCommandOptionType.Integer, "The number of messages to purge", isRequired: true);
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = (SocketGuildUser)cmd.User;
      var channel = (SocketTextChannel)cmd.Channel;
      var count = (int)cmd.GetOption<long>("count");

      if (!service.IsAuthorized(user, ModrankLevel.Moderator, out var error))
      {
        await cmd.RespondAsync(error);
        return;
      }

      if (count > service.MaxPurgeCount)
      {
        await cmd.RespondAsync($"Cannot purge more than {service.MaxPurgeCount} messages");
        return;
      }

      await cmd.DeferAsync();
      await service.Purge(cmd, channel, count);
      await cmd.FollowupAsync($"Purged {count} messages from {channel.Mention}");
    }
  }
}
