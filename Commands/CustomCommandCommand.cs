using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class CustomCommandCommand : SlashCommandBase
  {
    private readonly CustomCommandService service;

    public CustomCommandCommand(CustomCommandService service) : base("command")
    {
      Options = new SlashCommandOptionBuilder()
        .AddOption(new SlashCommandOptionBuilder()
          .WithName("add")
          .WithDescription("Add a custom command")
          .AddOption("name", ApplicationCommandOptionType.String, "The name of the command to add", isRequired: true)
          .AddOption("response", ApplicationCommandOptionType.String,
            @"Respond with this. Use $1, $2 for parameters, \n for newline or \m for new message", isRequired: true)
          .AddOption("description", ApplicationCommandOptionType.String, "A note on what the command does and how to use it", isRequired: false)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("remove")
          .WithDescription("Remove a custom command")
          .AddOption("name", ApplicationCommandOptionType.String, "The name of the command to remove", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("list")
          .WithDescription("List all custom commands")
          .WithType(ApplicationCommandOptionType.SubCommand)
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
        "add" => AddCommand(cmd, subcommand, guild),
        "remove" => RemoveCommand(cmd, subcommand, guild),
        "list" => ListCommands(cmd, guild),
        _ => throw new InvalidOperationException($"Unknown subcommand {subcommand.Name}")
      };

      await handle;
    }

    private async Task AddCommand(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var name = subcommand.GetOption<string>("name")!;
      var response = subcommand.GetOption<string>("response")!;
      var description = subcommand.GetOption<string>("description");

      if (await service.HasCommand(guild, name))
      {
        await cmd.RespondAsync($"Command **{await service.PrefixCommandName(guild, name)}** already exists");
      }

      if (!ValidateResponse(response, out var error))
      {
        await cmd.RespondAsync(error);
        return;
      }

      await service.AddCommand(guild, name, response, description);
      await cmd.RespondAsync($"Added command **{await service.PrefixCommandName(guild, name)}**");
    }

    private bool Authorize(SocketGuildUser user, string subcommand, out string? error)
    {
      error = null;
      if (subcommand == "list")
      {
        return true;
      }

      return service.IsAuthorized(user, ModrankLevel.Moderator, out error);
    }

    private async Task RemoveCommand(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var name = subcommand.GetOption<string>("name")!;

      if (!await service.HasCommand(guild, name))
      {
        await cmd.RespondAsync($"Command **{await service.PrefixCommandName(guild, name)}** does not exist");
      }

      await service.RemoveCommand(guild, name);
      await cmd.RespondAsync($"Removed command **{await service.PrefixCommandName(guild, name)}**");
    }

    private async Task ListCommands(SocketSlashCommand cmd, SocketGuild guild)
    {
      if (!await service.HasCommands(guild))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} There are no custom commands");
      }

      var commands = await service.GetCommands(guild);

      var embed = new EmbedBuilder()
        .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
        .WithTitle("Custom commands")
        .WithColor(Colors.Blurple);

      foreach (var command in commands)
      {
        var field = new EmbedFieldBuilder()
          .WithName(await service.PrefixCommandName(guild, command.Name))
          .WithValue(command.Description);
        embed.AddField(field);
      }

      await cmd.RespondAsync(embed: embed.Build());
    }

    private bool ValidateResponse(string response, out string? error)
    {
      error = default;
      var infos = service.GetParameterErrorInfos(response);
      for (int i = 0; i < infos.Count; i++)
      {
        var info = infos[i];
        if (info.DollarIndex == i + 1)
        {
          continue;
        }

        var highlightedResponse = response
          .Insert(info.ResponseEndIndex, "~~**")
          .Insert(info.ResponseStartIndex, "**~~");
        error = "Parameter numbers must be starting from 1 and incremented by 1. Same parameters can be used multiple times\n" +
          $"Expected parameter **${i + 1}** but got **${info.DollarIndex}**\n" +
          $"Fix it: *{highlightedResponse}*";
        return false;
      }

      return true;
    }
  }
}
