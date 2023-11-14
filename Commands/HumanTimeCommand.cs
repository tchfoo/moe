using Discord;
using Discord.WebSocket;
using Moe.Models;
using Moe.Services;

namespace Moe.Commands;

public class HumanTimeCommand : SlashCommandBase
{
  private readonly HumanTimeService service;

  public HumanTimeCommand(HumanTimeService service) : base("humantime")
  {
    Description = "Converts time between time zones to human time";
    Options = new SlashCommandOptionBuilder()
      .AddOption("from", ApplicationCommandOptionType.String, "The time and time zone to convert from", isRequired: false)
      .AddOption("to", ApplicationCommandOptionType.String, "The time zone to convert to", isRequired: false);
    this.service = service;
  }

  public async override Task Handle(SocketSlashCommand cmd)
  {
    var guild = (cmd.Channel as SocketGuildChannel)!.Guild;

    var from = cmd.GetOption<string>("from") ?? string.Empty;
    var to = cmd.GetOption<string>("to") ?? string.Empty;

    if (string.IsNullOrEmpty(from) && string.IsNullOrEmpty(to))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} You didn't specify any time zone. Perhaps you believe in the Stacked Earth theory?");
      return;
    }

    TimeZoneTime fromTime;
    try
    {
      fromTime = await service.ParseTimeZoneTime(guild, from);
    }
    catch (FormatException ex)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Invalid from: {ex.Message}");
      return;
    }
    TimeZoneTime toTime;
    try
    {
      toTime = await service.ParseTimeZoneTime(guild, to);
    }
    catch (FormatException ex)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Invalid to: {ex.Message}");
      return;
    }

    toTime.Time = fromTime.Time + toTime.UtcOffset - fromTime.UtcOffset;

    var embed = new EmbedBuilder()
      .WithTitle("Time zone converter")
      .AddField("From", $"{fromTime.Time:HH:mm} {fromTime.TimeZoneString}")
      .AddField("To", $"{toTime.Time:HH:mm} {toTime.TimeZoneString}")
      .WithColor(Colors.Blurple);
    await cmd.RespondAsync(embed: embed.Build());
  }
}
