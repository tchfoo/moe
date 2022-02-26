using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class UnmuteCommand : SlashCommandBase
  {
    private readonly MuteService service;

    public UnmuteCommand(MuteService service) : base("unmute")
    {
      Description = "Unmute a user";
      Options = new SlashCommandOptionBuilder()
        .AddOption("user", ApplicationCommandOptionType.User, "The user to unmute", isRequired: true);
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = cmd.GetOption<SocketGuildUser>("user")!;

      var commandUser = (SocketGuildUser)cmd.User;
      if (!service.IsAuthorized(commandUser, ModrankLevel.Moderator, out var error))
      {
        await cmd.RespondAsync(error);
        return;
      }

      if (!await service.IsMuted(user))
      {
        await cmd.RespondAsync($"**{user}** was not muted");
        return;
      }

      await service.UnmuteUser(user);
      await cmd.RespondAsync($"Unmuted **{user}**");
    }
  }
}
