using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class MuteCommand : SlashCommandBase
  {
    private readonly MuteService service;

    public MuteCommand(MuteService service) : base("mute")
    {
      Description = "Mute a user";
      Options = new SlashCommandOptionBuilder()
        .AddOption("user", ApplicationCommandOptionType.User, "The user to mute", isRequired: true)
        .AddOption("time", ApplicationCommandOptionType.String, "Duration of the mute (eg. 1h 30m)", isRequired: false)
        .AddOption("reason", ApplicationCommandOptionType.String, "Reason for the mute", isRequired: false);
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = cmd.GetOption<SocketGuildUser>("user")!;
      var guild = (cmd.Channel as SocketGuildChannel)!.Guild;
      TimeSpan duration = await service.GetDefaultMuteLength(guild);
      if (cmd.HasOption("time"))
      {
        var time = cmd.GetOption<string>("time")!;
        duration = DurationParser.Parse(time);
      }
      var expireAt = DateTime.Now + duration;
      var reason = cmd.GetOption("reason", "unspecified");

      if (await service.IsMuted(user))
      {
        await cmd.RespondAsync($"**{user}** is already muted");
        return;
      }

      await service.MuteUser(user, expireAt);
      await cmd.RespondAsync($"Muted **{user}** for {duration}. Reason: {reason}");
    }
  }
}
