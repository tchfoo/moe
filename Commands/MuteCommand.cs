using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class MuteCommand : SlashCommandBase
  {
    private static readonly TimeSpan DefaultMuteDuration = TimeSpan.FromMinutes(30);
    private readonly MuteService muteService;

    public MuteCommand(MuteService service) : base("mute", "Mute a user.")
    {
      Options = new SlashCommandOptionBuilder()
        .AddOption("user", ApplicationCommandOptionType.User, "The user to mute.", isRequired: true)
        .AddOption("time", ApplicationCommandOptionType.String, "Duration of the mute (eg. 1h 30m)", isRequired: false)
        .AddOption("reason", ApplicationCommandOptionType.String, "Reason for the mute.", isRequired: false);
      muteService = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = cmd.GetOption<SocketGuildUser>("user")!;
      TimeSpan duration = DefaultMuteDuration;
      if (cmd.HasOption("time"))
      {
        duration = ParseDuration(cmd.GetOption<string>("time")!);
      }
      var expireAt = DateTime.Now + duration;
      var reason = cmd.GetOption("reason", "unspecified");

      if (await muteService.IsMuted(user))
      {
        await cmd.RespondAsync($"**{user}** is already muted.");
        return;
      }

      await muteService.MuteUser(user, expireAt);
      await cmd.RespondAsync($"Muted **{user}** for {duration}. Reason: {reason}.");
    }

    private TimeSpan ParseDuration(string s)
    {
      var days = ParsePostfixedNumber(s, "d");
      var hours = ParsePostfixedNumber(s, "h");
      var minutes = ParsePostfixedNumber(s, "m");
      var seconds = ParsePostfixedNumber(s, "s");
      return new TimeSpan(days, hours, minutes, seconds);
    }

    private int ParsePostfixedNumber(string text, string postfix)
    {
      var match = Regex.Match(text, $@"\d+\s*{postfix}");
      if (!match.Success)
      {
        return 0;
      }

      var withoutPostfix = match.Value.Replace(postfix, string.Empty);
      return int.Parse(withoutPostfix);
    }
  }
}
