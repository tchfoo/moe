using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class UnmuteCommand : SlashCommandBase
  {
    private readonly MuteService muteService;

    public UnmuteCommand(MuteService service) : base("unmute", "Unmute a user.")
    {
      Options = new SlashCommandOptionBuilder()
        .AddOption("user", ApplicationCommandOptionType.User, "The user to unmute.", isRequired: true);
      muteService = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = cmd.GetOption<SocketGuildUser>("user")!;

      if (!await muteService.IsMuted(user))
      {
        await cmd.RespondAsync($"**{user}** was not muted.");
        return;
      }

      await muteService.UnmuteUser(user);
      await cmd.RespondAsync($"Unmuted **{user}**.");
    }
  }
}
