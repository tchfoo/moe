using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class ListCustomRolesCommand : SlashCommandBase
  {
    private readonly CustomRoleService service;

    public ListCustomRolesCommand(CustomRoleService service) : base("listroles")
    {
      Description = "List all roles";
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var guild = (cmd.Channel as SocketGuildChannel)!.Guild;

      if (!await service.HasRoles(guild))
      {
        await cmd.RespondAsync("There are no custom roles");
        return;
      }

      var response = "Roles:\n";
      var roles = await service.GetRoles(guild);
      foreach (var role in roles)
      {
        response += $" - **{role.Name}**: {role.DiscordRole.Mention}\n";
      }

      await cmd.RespondAsync(response);
    }
  }
}
