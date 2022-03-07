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
          .WithDescription("Add a role to the list of assignable roles")
          .AddOption("name", ApplicationCommandOptionType.String, "The name of the assignable role to add", isRequired: true)
          .AddOption("role", ApplicationCommandOptionType.Role, "The role to add", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("remove")
          .WithDescription("Remove a role from the list of assignable roles")
          .AddOption("name", ApplicationCommandOptionType.String, "The assignable role to remove", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("toggle")
          .WithDescription("Apply or remove an assignable role from yourself")
          .AddOption("name", ApplicationCommandOptionType.String, "The assignable role to toggle", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("list")
          .WithDescription("List all assignable roles")
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
        await cmd.RespondAsync(error);
        return;
      }

      var handle = subcommand.Name switch
      {
        "add" => AddRole(cmd, subcommand, user.Guild),
        "remove" => RemoveRole(cmd, subcommand, user.Guild),
        "toggle" => ToggleRole(cmd, subcommand, user.Guild),
        "list" => ListRoles(cmd, user.Guild),
        _ => throw new InvalidOperationException($"Unknown subcommand {subcommand.Name}")
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

    private async Task ToggleRole(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var name = subcommand.GetOption<string>("name")!;
      var user = (SocketGuildUser)cmd.User;

      if (!await service.HasRole(guild, name))
      {
        await cmd.RespondAsync($"Role **{name}** does not exist");
        return;
      }

      if (await service.IsSubscribedToRole(user, name))
      {
        await service.UnsubscribeFromRole(user, name);
        await cmd.RespondAsync($"Unsubscribed from role **{name}**");
      }
      else
      {
        await service.SubscribeToRole(user, name);
        await cmd.RespondAsync($"Subscribed to role **{name}**");
      }
    }

    private async Task ListRoles(SocketSlashCommand cmd, SocketGuild guild)
    {
      if (!await service.HasRoles(guild))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} There are no custom roles");
        return;
      }

      var embed = new EmbedBuilder()
        .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
        .WithTitle("Applicable roles")
        .WithColor(Colors.Blurple);

      var roles = await service.GetRoles(guild);
      foreach (var role in roles)
      {
        var field = new EmbedFieldBuilder()
          .WithName(role.Name)
          .WithValue(role.DiscordRole.Mention);
        embed.AddField(field);
      }

      await cmd.RespondAsync(embed: embed.Build());
    }
  }
}
