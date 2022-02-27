using Discord;
using Discord.WebSocket;
using TNTBot.Models;
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
          .AddOption(new SlashCommandOptionBuilder()
            .WithName("level")
            .WithDescription("Level of the modrank")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.Integer)
            .AddChoice(nameof(ModrankLevel.None), (int)ModrankLevel.None)
            .AddChoice(nameof(ModrankLevel.Moderator), (int)ModrankLevel.Moderator)
            .AddChoice(nameof(ModrankLevel.Administrator), (int)ModrankLevel.Administrator)
          ).WithType(ApplicationCommandOptionType.SubCommand)
        ).WithType(ApplicationCommandOptionType.SubCommandGroup);
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = (SocketGuildUser)cmd.User;
      var guild = user.Guild;
      var subcommand = cmd.GetSubcommand();

      if (!Authorize(user, subcommand.Name, out var error))
      {
        await cmd.RespondAsync(error);
        return;
      }

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

    private bool Authorize(SocketGuildUser user, string subcommand, out string? error)
    {
      if (subcommand == "list")
      {
        if (!service.IsAuthorized(user, ModrankLevel.Moderator, out error))
        {
          return false;
        }
      }
      else
      {
        if (!service.IsAuthorized(user, ModrankLevel.Administrator, out error))
        {
          return false;
        }
      }

      return true;
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
        modranksString += $"\n - {modrank.Level} - {modrank.Role.Mention}";
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
      var level = (ModrankLevel)subcommand.GetOption<long>("level")!;
      await service.SetModrank(role, level);
      await cmd.RespondAsync($"Modrank is now {level} for role {role.Mention}");
    }
  }
}
