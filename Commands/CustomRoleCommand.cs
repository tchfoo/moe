using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class CustomRoleCommand : SlashCommandBase
  {
    private readonly CustomRoleService service;

    public CustomRoleCommand(CustomRoleService service) : base("role")
    {
      Description = "Toggle roles on yourself";
      Options = new SlashCommandOptionBuilder()
        .AddOption("name", ApplicationCommandOptionType.String, "The name of the role to toggle", isRequired: true);

      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = (SocketGuildUser)cmd.User;
      var guild = user.Guild;
      var name = cmd.GetOption<string>("name")!;

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
  }
}
