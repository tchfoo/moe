using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
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
          .AddOption("role", ApplicationCommandOptionType.Role, "The role to add", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("remove")
          .WithDescription("Remove a role from the list of custom roles")
          .AddOption("name", ApplicationCommandOptionType.String, "The custom role", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("toggle")
          .WithDescription("Apply or remove an custom role from yourself")
          .AddOption("name", ApplicationCommandOptionType.String, "The custom role", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("list")
          .WithDescription("List all custom roles")
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
        "toggle" => ToggleRole(cmd, subcommand, user.Guild),
        "list" => ListRoles(cmd, user.Guild),
        _ => throw new InvalidOperationException($"{Emotes.ErrorEmote} Unknown subcommand {subcommand.Name}")
      };

      await handle;
    }

    private bool Authorize(SocketGuildUser user, string subcommand, out string? error)
    {
      error = null;
      if (subcommand == "list" || subcommand == "toggle")
      {
        return true;
      }

      return service.IsAuthorized(user, ModrankLevel.Administrator, out error);
    }

    private async Task AddRole(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var name = subcommand.GetOption<string>("name")!;
      var role = subcommand.GetOption<SocketRole>("role")!;

      if (await service.HasRole(guild, name))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} Role **{name}** already exists");
        return;
      }

      await service.AddRole(guild, name, role);
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

    private async Task ToggleRole(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var name = subcommand.GetOption<string>("name")!;
      var user = (SocketGuildUser)cmd.User;

      if (!await service.HasRole(guild, name))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} Role **{name}** does not exist");
        return;
      }

      if (await service.IsSubscribedToRole(user, name))
      {
        await service.UnsubscribeFromRole(user, name);
        await cmd.RespondAsync($"{Emotes.SuccessEmote} Unsubscribed from role **{name}**");
      }
      else
      {
        await service.SubscribeToRole(user, name);
        await cmd.RespondAsync($"{Emotes.SuccessEmote} Subscribed to role **{name}**");
      }
    }

    private async Task ListRoles(SocketSlashCommand cmd, SocketGuild guild)
    {
      if (!await service.HasRoles(guild))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} There are no custom roles");
        return;
      }

      var roles = await service.GetRoles(guild);

      var p = new PaginatableEmbedBuilder<CustomRole>
        (5, roles, items =>
          new EmbedBuilder()
            .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
            .WithTitle("Custom roles")
            .WithFields(items.Select(x => new EmbedFieldBuilder()
              .WithName(x.Name)
              .WithValue(x.DiscordRole.Mention)))
            .WithColor(Colors.Blurple)
        );

      await cmd.RespondAsync(embed: p.Embed, components: p.Components);
    }
  }
}
