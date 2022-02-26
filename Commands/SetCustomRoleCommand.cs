using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class SetCustomRoleCommand : SlashCommandBase
  {
    private readonly CustomRoleService service;

    public SetCustomRoleCommand(CustomRoleService service) : base("setrole")
    {
      Options = new SlashCommandOptionBuilder()
        .AddOption(new SlashCommandOptionBuilder()
          .WithName("add")
          .WithDescription("Add a role to the list of assignable roles")
          .AddOption("name", ApplicationCommandOptionType.String, "The name of the role to add", isRequired: true)
          .AddOption("role", ApplicationCommandOptionType.Role, "The role to add", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("remove")
          .WithDescription("Remove a role from the list of assignable roles")
          .AddOption("name", ApplicationCommandOptionType.String, "The name of the role to remove", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).WithType(ApplicationCommandOptionType.SubCommandGroup);

      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = (SocketGuildUser)cmd.User;
      var guild = user.Guild;
      var subcommand = cmd.GetSubcommand();

      if (!service.IsAuthorized(user, ModrankLevel.Administrator, out var error))
      {
        await cmd.RespondAsync(error);
        return;
      }

      var handle = subcommand.Name switch
      {
        "add" => AddRole(cmd, subcommand, guild),
        "remove" => RemoveRole(cmd, subcommand, guild),
        _ => throw new InvalidOperationException($"Unknown subcommand {subcommand.Name}")
      };

      await handle;
    }

    private async Task AddRole(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var name = subcommand.GetOption<string>("name")!;
      var role = subcommand.GetOption<SocketRole>("role")!;

      if (await service.HasRole(guild, name))
      {
        await cmd.RespondAsync($"Role **{name}** already exists");
        return;
      }

      await service.AddRole(guild, name, role);
      await cmd.RespondAsync($"Added role **{name}** to the list of assignable roles");
    }

    private async Task RemoveRole(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var name = subcommand.GetOption<string>("name")!;

      if (!await service.HasRole(guild, name))
      {
        await cmd.RespondAsync($"Role **{name}** does not exist");
        return;
      }

      await service.RemoveRole(guild, name);
      await cmd.RespondAsync($"Removed role **{name}** from the list of assignable roles");
    }
  }
}
