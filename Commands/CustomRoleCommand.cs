using Discord;
using Discord.WebSocket;
using MoeBot.Models;
using MoeBot.Services;

namespace MoeBot.Commands;

public class CustomRoleCommand : SlashCommandBase
{
  private readonly CustomRoleService service;

  public CustomRoleCommand(CustomRoleService service) : base("role")
  {
    Options = new SlashCommandOptionBuilder()
      .AddOption(new SlashCommandOptionBuilder()
        .WithName("add")
        .WithDescription("Add a role to the list of custom roles")
        .AddOption("name", ApplicationCommandOptionType.String, "The name of the custom role", isRequired: true)
        .AddOption("description", ApplicationCommandOptionType.String, "A description on what this role is used for", isRequired: false)
        .AddOption("role", ApplicationCommandOptionType.Role, "The role to add", isRequired: true)
        .WithType(ApplicationCommandOptionType.SubCommand)
      ).AddOption(new SlashCommandOptionBuilder()
        .WithName("remove")
        .WithDescription("Remove a role from the list of custom roles")
        .AddOption("name", ApplicationCommandOptionType.String, "The custom role", isRequired: true)
        .WithType(ApplicationCommandOptionType.SubCommand)
      ).AddOption(new SlashCommandOptionBuilder()
        .WithName("select")
        .WithDescription("Apply or remove custom roles from yourself")
        .WithType(ApplicationCommandOptionType.SubCommand)
      ).WithType(ApplicationCommandOptionType.SubCommandGroup);
    this.service = service;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    var user = (SocketGuildUser)cmd.User;
    var subcommand = cmd.GetSubcommand();

    if (!Authorize(user, subcommand.Name, out var error))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} " + error);
      return;
    }

    var handle = subcommand.Name switch
    {
      "add" => AddRole(cmd, subcommand, user.Guild),
      "remove" => RemoveRole(cmd, subcommand, user.Guild),
      "select" => SelectRole(cmd, subcommand, user.Guild),
      _ => throw new InvalidOperationException($"{Emotes.ErrorEmote} Unknown subcommand {subcommand.Name}")
    };

    await handle;
  }

  private bool Authorize(SocketGuildUser user, string subcommand, out string? error)
  {
    error = null;
    if (subcommand == "list" || subcommand == "select")
    {
      return true;
    }

    return service.IsAuthorized(user, ModrankLevel.Administrator, out error);
  }

  private async Task AddRole(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
  {
    var name = subcommand.GetOption<string>("name")!;
    var description = subcommand.GetOption<string>("description");
    var role = subcommand.GetOption<SocketRole>("role")!;

    if (await service.HasRole(guild, name))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Role **{name}** already exists");
      return;
    }

    await service.AddRole(guild, name, description, role);
    await cmd.RespondAsync($"{Emotes.SuccessEmote} Added role **{name}** to the list of custom roles");
  }

  private async Task RemoveRole(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
  {
    var name = subcommand.GetOption<string>("name")!;

    if (!await service.HasRole(guild, name))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Role **{name}** does not exist");
      return;
    }

    await service.RemoveRole(guild, name);
    await cmd.RespondAsync($"{Emotes.SuccessEmote} Removed role **{name}** from the list of custom roles");
  }

  private async Task SelectRole(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
  {
    var user = (SocketGuildUser)cmd.User;

    if (!await service.HasRoles(guild))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} There are no custom roles");
      return;
    }

    var roles = await service.GetRoles(guild);
    IEnumerable<CustomRole> oldSubscribedRoles = await service.GetSubscribedRoles(user);
    var options = roles.Select(x =>
        new SelectMenuOptionBuilder(x.Name, x.Name, x.Description, isDefault: oldSubscribedRoles.Contains(x))
      ).ToList();

    var menu = (SubmittableSelectMenuBuilder)new SubmittableSelectMenuBuilder()
      .WithPlaceholder("Choose roles to apply to yourself")
      .WithOptions(options)
      .WithMinValues(0)
      .WithMaxValues(options.Count());

    menu.OnSubmitted += async submitted =>
    {
      var submittedRoles = submitted.Data.Values;
      var newSubscribedRoles = (await service.GetRoles(guild))
        .Where(x => submittedRoles.Contains(x.Name));

      await service.SyncronizeRoleSubscriptions(user, oldSubscribedRoles, newSubscribedRoles);
      oldSubscribedRoles = newSubscribedRoles;

      await submitted.RespondAsync($"{Emotes.SuccessEmote} Your roles have been updated", ephemeral: true);
    };

    var components = new ComponentBuilder()
        .WithSelectMenu(menu);
    await cmd.RespondAsync(components: components.Build(), ephemeral: true);
  }
}
