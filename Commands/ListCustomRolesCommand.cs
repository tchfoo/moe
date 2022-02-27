using Discord;
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
