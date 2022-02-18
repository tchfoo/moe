using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class SettingsCommand : SlashCommandBase
  {
    private readonly SettingsService service;

    public SettingsCommand(SettingsService service) : base("settings")
    {
      Description = "View or change settings";
      Options = new SlashCommandOptionBuilder()
        .AddOption(new SlashCommandOptionBuilder()
          .WithName("list")
          .WithDescription("List all settings")
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("pinchannel")
          .WithDescription("Set a channel where the bot will pin messages")
          .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("logchannel")
          .WithDescription("Set a channel where the bot will log messages")
          .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("mutelength")
          .WithDescription("Set the default mute duration")
          .AddOption("time", ApplicationCommandOptionType.String, "Duration of the mute (eg. 1h 30m)", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).WithType(ApplicationCommandOptionType.SubCommandGroup);
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var guild = (cmd.Channel as SocketGuildChannel)!.Guild;
      var subcommand = cmd.GetSubcommand();

      var handle = subcommand.Name switch
      {
        "list" => ListSettings(cmd, guild),
        "mutelength" => SetMuteLength(cmd, subcommand, guild),
        "pinchannel" => SetPinChannel(cmd, subcommand, guild),
        "logchannel" => SetLogChannel(cmd, subcommand, guild),
        _ => throw new InvalidOperationException($"Unknown subcommand {subcommand.Name}")
      };

      await handle;
    }

    private async Task ListSettings(SocketSlashCommand cmd, SocketGuild guild)
    {
      var muteLength = await service.GetMuteLength(guild);
      var pinChannel = await service.GetPinChannel(guild);
      var logChannel = await service.GetLogChannel(guild);

      await cmd.RespondAsync(
        $"Mute length: {muteLength}\n" +
        $"Pin channel: {pinChannel?.Mention ?? "None"}\n" +
        $"Log channel: {logChannel?.Mention ?? "None"}"
      );
    }

    private async Task SetMuteLength(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var time = subcommand.GetOption<string>("time")!;
      var muteLength = DurationParser.Parse(time);
      await service.SetMuteLength(guild, muteLength);
      await cmd.RespondAsync("Mute length set to " + muteLength);
    }

    private async Task SetPinChannel(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var channel = subcommand.GetOption<SocketTextChannel>("channel")!;
      await service.SetPinChannel(guild, channel);
      await cmd.RespondAsync("Pin channel set to " + channel.Mention);
    }

    private async Task SetLogChannel(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var channel = subcommand.GetOption<SocketTextChannel>("channel")!;
      await service.SetLogChannel(guild, channel);
      await cmd.RespondAsync("Log channel set to " + channel.Mention);
    }
  }
}
