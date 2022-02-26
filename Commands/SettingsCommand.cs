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
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("modrank")
          .WithDescription("Change which discord roles are considered mods and admins by this bot. Server admins are bot admins")
          .AddOption("role", ApplicationCommandOptionType.Role, "The role", isRequired: true)
          .AddOption("level", ApplicationCommandOptionType.Integer, "Level of the modrank (0 = None, 1 = Moderator, 2 = Administrator)", isRequired: true)
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
        "modrank" => SetModrank(cmd, subcommand),
        _ => throw new InvalidOperationException($"Unknown subcommand {subcommand.Name}")
      };

      await handle;
    }

    private async Task ListSettings(SocketSlashCommand cmd, SocketGuild guild)
    {
      var muteLength = await service.GetMuteLength(guild);
      var pinChannel = await service.GetPinChannel(guild);
      var logChannel = await service.GetLogChannel(guild);
      var modranks = await service.GetModranks(guild);

      var embed = new EmbedBuilder()
        .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
        .WithTitle("Bot Settings")
        .AddField("Mute length", muteLength, inline: true)
        .AddField("Pin chanel", pinChannel?.Mention ?? "None", inline: true)
        .AddField("Log channel", logChannel?.Mention ?? "None", inline: true)
        .WithColor(Colors.Blurple);

      string modranksString = "Modranks:";
      foreach (var modrank in modranks)
      {
        var level = service.ConvertModrankLevelToString(modrank.Level);
        modranksString += $"\n - {level} - {modrank.Role.Mention}";
      }

      await cmd.RespondAsync(text: modranksString, embed: embed.Build());
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

    private async Task SetModrank(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand)
    {
      var role = subcommand.GetOption<SocketRole>("role")!;
      var level = (int)subcommand.GetOption<long>("level")!;
      if (level < 0 || level > 2)
      {
        await cmd.RespondAsync("Level must be 0, 1 or 2");
        return;
      }

      await service.SetModrank(role, level);
      var levelOut = service.ConvertModrankLevelToString(level);
      await cmd.RespondAsync($"Modrank is now {levelOut} for role {role.Mention}");
    }
  }
}
